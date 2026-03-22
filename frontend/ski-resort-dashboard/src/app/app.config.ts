import { ApplicationConfig } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimations } from '@angular/platform-browser/animations';

import { routes } from './app.routes';
import { APP_API_BASE_URL } from './core/tokens/app-config.token';
import { userIdInterceptor } from './core/interceptors/user-id.interceptor';
import { environment } from '../environments/environment';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(withInterceptors([userIdInterceptor])),
    provideAnimations(),
    { provide: APP_API_BASE_URL, useValue: environment.apiBaseUrl }
  ]
};
