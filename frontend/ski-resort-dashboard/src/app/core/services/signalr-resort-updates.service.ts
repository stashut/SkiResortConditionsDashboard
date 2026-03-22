import { inject, Injectable } from '@angular/core';
import {
  HttpTransportType,
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
  NullLogger
} from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { APP_API_BASE_URL } from '../tokens/app-config.token';
import { SignalrOmitCredentialsHttpClient } from './signalr-omit-credentials-http-client';

export interface ResortConditionsUpdatedEvent {
  resortId: string;
  payload: unknown;
}

@Injectable({
  providedIn: 'root'
})
export class SignalrResortUpdatesService {
  private readonly hubUrl = `${inject(APP_API_BASE_URL)}/hubs/resort-conditions`;
  private hub?: HubConnection;
  private readonly updatesSubject = new Subject<ResortConditionsUpdatedEvent>();
  private readonly desiredResortSubscriptions = new Set<string>();
  private startPromise?: Promise<void>;

  readonly updates$ = this.updatesSubject.asObservable();

  private isConnected(): boolean {
    return this.hub?.state === HubConnectionState.Connected;
  }

  start(): void {
    if (this.hub && this.hub.state !== HubConnectionState.Disconnected) {
      return;
    }

    if (!this.hub) {
      this.hub = new HubConnectionBuilder()
        // WebSockets can fail through some proxies; LongPolling uses plain HTTP and often works once /hubs/* is routed.
        // Cross-origin (CloudFront → execute-api): use fetch with credentials "omit" via a custom HttpClient so
        // negotiate is never credentialed (matches API Gateway CORS with Allow credentials off).
        .withUrl(this.hubUrl, {
          // API Gateway HTTP API does not proxy WebSockets to Kestrel; LongPolling (plain HTTP) is the
          // only transport that works end-to-end through execute-api without a dedicated WebSocket API.
          transport: HttpTransportType.LongPolling,
          withCredentials: false,
          httpClient: new SignalrOmitCredentialsHttpClient(NullLogger.instance)
        })
        .withAutomaticReconnect()
        .configureLogging(LogLevel.Information)
        .build();

      this.hub.on('ResortConditionsUpdated', (payload: unknown) => {
        const anyPayload = payload as any;
        const resortId =
          anyPayload?.resortId ?? anyPayload?.ResortId ?? anyPayload?.ResortID;

        if (typeof resortId === 'string' && resortId.length > 0) {
          this.updatesSubject.next({ resortId, payload });
        }
      });

      this.hub.onreconnected(() => this.syncAllSubscriptions());
    }

    this.startPromise = this.hub.start().then(() => this.syncAllSubscriptions());
    void this.startPromise.catch((err) => {
      console.error('SignalR connection failed', { hubUrl: this.hubUrl, err });
    });
  }

  private async ensureConnected(): Promise<boolean> {
    if (!this.hub) {
      this.start();
    }

    if (!this.hub) {
      return false;
    }

    if (this.isConnected()) {
      return true;
    }

    if (this.hub.state === HubConnectionState.Connecting && this.startPromise) {
      try {
        await this.startPromise;
      } catch {
        return false;
      }
      return this.isConnected();
    }

    return false;
  }

  private async syncAllSubscriptions(): Promise<void> {
    if (!(await this.ensureConnected()) || !this.hub) {
      return;
    }

    for (const resortId of this.desiredResortSubscriptions) {
      void this.hub.invoke('SubscribeToResort', resortId).catch((err) => {
        console.error('Failed to sync resort subscription', err);
      });
    }
  }

  subscribeToResort(resortId: string): void {
    this.desiredResortSubscriptions.add(resortId);
    this.start();

    void this.ensureConnected().then((connected) => {
      if (!connected || !this.hub) {
        return;
      }

      void this.hub.invoke('SubscribeToResort', resortId).catch((err) => {
        console.error('Failed to subscribe to resort updates', err);
      });
    });
  }

  unsubscribeFromResort(resortId: string): void {
    this.desiredResortSubscriptions.delete(resortId);

    void this.ensureConnected().then((connected) => {
      if (!connected || !this.hub) {
        return;
      }

      void this.hub
        .invoke('UnsubscribeFromResort', resortId)
        .catch((err) => {
          console.error('Failed to unsubscribe from resort updates', err);
        });
    });
  }

  stop(): void {
    if (!this.hub) {
      return;
    }

    void this.hub.stop().catch((err) => {
      console.error('Failed to stop SignalR connection', err);
    });
  }
}

