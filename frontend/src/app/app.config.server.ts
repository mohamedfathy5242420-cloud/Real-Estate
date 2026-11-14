import { mergeApplicationConfig, ApplicationConfig } from '@angular/core';
import { provideServerRendering, withRoutes } from '@angular/ssr';
import { appConfig } from './app.config';
import { serverRoutes } from './app.routes.server';
import { API_BASE } from './api/api-base';

const serverConfig: ApplicationConfig = {
  providers: [
    provideServerRendering(withRoutes(serverRoutes)),
    {
      provide: API_BASE,
      useFactory: () => `${process.env['API_INTERNAL_ORIGIN'] ?? 'http://localhost:5006'}/api`,
    },
  ],
};

export const config = mergeApplicationConfig(appConfig, serverConfig);
