import { DisplayCurrencyPipe, DisplayNumberPipe } from '../../pipes/display-format.pipe';
import { RouterLink } from '@angular/router';
import { Component, input } from '@angular/core';
import { PropertyVisual } from '../property-visual/property-visual';
import { CatalogProperty } from '../../models/catalog';

@Component({
  selector: 'app-property-card',
  imports: [RouterLink, DisplayCurrencyPipe, DisplayNumberPipe, PropertyVisual],
  templateUrl: './property-card.html',
  styleUrl: './property-card.css',
})
export class PropertyCard {
  readonly property = input.required<CatalogProperty>();
  readonly demo = input(true);
}
