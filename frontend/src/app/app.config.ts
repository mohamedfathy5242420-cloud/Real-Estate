import { ApplicationConfig, LOCALE_ID, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { provideHttpClient } from '@angular/common/http';
import { CatalogReader, CATALOG_DATA_MODE } from './api/catalog-reader';
import { HttpCatalogReader } from './api/http-catalog-reader';
import { provideClientHydration, withEventReplay } from '@angular/platform-browser';

import { APP_LOCALE } from './app-locale';

export const appConfig: ApplicationConfig = {
  providers: [
    { provide: LOCALE_ID, useValue: APP_LOCALE },
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideClientHydration(withEventReplay()),
    provideHttpClient(),
    { provide: CATALOG_DATA_MODE, useValue: 'live' },
    { provide: CatalogReader, useClass: HttpCatalogReader },
  ],
};
