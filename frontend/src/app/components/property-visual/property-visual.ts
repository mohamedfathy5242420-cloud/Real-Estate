import { Component, computed, input } from '@angular/core';

/** Local editorial artwork, never a photo or evidence of unit amenities. */
@Component({
  selector: 'app-property-visual',
  template: `<div class="visual" [class]="'visual visual--' + scene()" aria-hidden="true">
    <span class="visual-label">Architectural illustration</span>
  </div>`,
  styleUrl: './property-visual.css',
})
export class PropertyVisual {
  readonly unitId = input('');
  protected readonly scene = computed(() => {
    const id = this.unitId();
    if (id === 'demo-coast-01') return 'coast';
    if (id === 'demo-city-01' || id === 'demo-cedar-02') return 'city';
    return 'garden';
  });
}
