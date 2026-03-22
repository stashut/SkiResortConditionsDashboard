import {
  AbortError,
  HttpClient,
  HttpError,
  HttpRequest,
  HttpResponse,
  ILogger,
  LogLevel,
  TimeoutError
} from '@microsoft/signalr';

function isArrayBuffer(val: unknown): val is ArrayBuffer {
  return val instanceof ArrayBuffer;
}

function deserializeContent(
  response: Response,
  responseType?: XMLHttpRequestResponseType
): Promise<string | ArrayBuffer> {
  switch (responseType) {
    case 'arraybuffer':
      return response.arrayBuffer();
    case 'text':
      return response.text();
    case 'blob':
    case 'document':
    case 'json':
      throw new Error(`${responseType} is not supported.`);
    default:
      return response.text();
  }
}

/**
 * SignalR {@link HttpClient} that always uses {@link fetch} with
 * `credentials: "omit"`. That avoids credentialed CORS (no
 * `Access-Control-Allow-Credentials: true` needed), which matches
 * API Gateway HTTP APIs that keep **Allow credentials** off.
 *
 * The stock {@link @microsoft/signalr!FetchHttpClient} uses
 * `same-origin` when `withCredentials` is false; some environments
 * still surface negotiate failures against API Gateway. `omit` is
 * the strictest guarantee for cross-origin hubs.
 */
export class SignalrOmitCredentialsHttpClient extends HttpClient {
  private readonly _logger: ILogger;

  constructor(logger: ILogger) {
    super();
    this._logger = logger;
  }

  /** @inheritDoc */
  public async send(request: HttpRequest): Promise<HttpResponse> {
    if (request.abortSignal?.aborted) {
      throw new AbortError();
    }
    if (!request.method) {
      throw new Error('No method defined.');
    }
    if (!request.url) {
      throw new Error('No url defined.');
    }

    const abortController = new AbortController();
    let abortError: AbortError | TimeoutError | undefined;

    if (request.abortSignal) {
      request.abortSignal.onabort = () => {
        abortController.abort();
        abortError = new AbortError();
      };
    }

    let timeoutId: ReturnType<typeof setTimeout> | undefined;
    if (request.timeout) {
      timeoutId = setTimeout(() => {
        abortController.abort();
        this._logger.log(LogLevel.Warning, 'Timeout from HTTP request.');
        abortError = new TimeoutError();
      }, request.timeout);
    }

    const content = request.content === '' ? undefined : request.content;
    const headers: Record<string, string> = {
      'X-Requested-With': 'XMLHttpRequest',
      ...(request.headers as Record<string, string> | undefined)
    };

    if (content) {
      if (isArrayBuffer(content)) {
        headers['Content-Type'] = 'application/octet-stream';
      } else {
        headers['Content-Type'] = 'text/plain;charset=UTF-8';
      }
    }

    let response: Response;
    try {
      response = await globalThis.fetch(request.url, {
        body: content,
        cache: 'no-cache',
        credentials: 'omit',
        headers,
        method: request.method,
        mode: 'cors',
        redirect: 'follow',
        signal: abortController.signal
      });
    } catch (e) {
      if (abortError) {
        throw abortError;
      }
      this._logger.log(LogLevel.Warning, `Error from HTTP request. ${e}.`);
      throw e;
    } finally {
      if (timeoutId) {
        clearTimeout(timeoutId);
      }
      if (request.abortSignal) {
        request.abortSignal.onabort = null;
      }
    }

    if (!response.ok) {
      const errorMessage = (await deserializeContent(
        response,
        'text'
      )) as string;
      throw new HttpError(errorMessage || response.statusText, response.status);
    }

    const payload = await deserializeContent(response, request.responseType);
    return new HttpResponse(response.status, response.statusText, payload);
  }
}
