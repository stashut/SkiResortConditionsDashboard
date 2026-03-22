import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { APP_API_BASE_URL } from '../tokens/app-config.token';

const STORAGE_KEY = 'ski-anon-user-id';

let memoryFallbackId: string | undefined;

function getOrCreateAnonymousUserId(): string {
  try {
    let id = localStorage.getItem(STORAGE_KEY);
    if (!id?.trim()) {
      id = crypto.randomUUID();
      localStorage.setItem(STORAGE_KEY, id);
    }
    return id;
  } catch {
    // Private mode / blocked storage — per tab session, avoids sharing one global id
    memoryFallbackId ??= crypto.randomUUID();
    return memoryFallbackId;
  }
}

/**
 * Sends X-User-Id so favorites/settings are scoped per browser (see API GetUserId()).
 * Required for production DynamoDB; avoids every anonymous user sharing "demo-user".
 */
export const userIdInterceptor: HttpInterceptorFn = (req, next) => {
  const apiBase = inject(APP_API_BASE_URL);
  if (!req.url.startsWith(apiBase)) {
    return next(req);
  }

  const id = getOrCreateAnonymousUserId();
  return next(
    req.clone({
      setHeaders: { 'X-User-Id': id }
    })
  );
};
