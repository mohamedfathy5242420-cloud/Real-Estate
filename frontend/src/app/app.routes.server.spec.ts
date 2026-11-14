import { RenderMode } from '@angular/ssr';
import { serverRoutes } from './app.routes.server';

describe('server route rendering policy', () => {
  it('server-renders the public catalog routes', () => {
    expect(serverRoutes).toContainEqual({ path: 'catalog', renderMode: RenderMode.Server });
    expect(serverRoutes).toContainEqual({ path: 'catalog/:unitId', renderMode: RenderMode.Server });
  });

  it('client-renders private and admin route families', () => {
    expect(serverRoutes).toContainEqual({ path: 'my/**', renderMode: RenderMode.Client });
    expect(serverRoutes).toContainEqual({ path: 'employee/**', renderMode: RenderMode.Client });
    expect(serverRoutes).toContainEqual({ path: 'admin/**', renderMode: RenderMode.Client });
  });
});
