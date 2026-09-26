import { TestBed } from '@angular/core/testing';
import { PageHeading } from './page-heading/page-heading';
import { UiState } from './ui-state/ui-state';

describe('shared UI primitives', () => {
  it('renders a focusable route heading with its context', () => {
    const fixture = TestBed.createComponent(PageHeading);
    fixture.componentRef.setInput('eyebrow', 'Public route');
    fixture.componentRef.setInput('title', 'Property catalog');
    fixture.componentRef.setInput('description', 'Browse the published catalog.');
    fixture.detectChanges();

    const heading = fixture.nativeElement.querySelector('#route-heading') as HTMLHeadingElement;
    expect(heading.textContent).toContain('Property catalog');
    expect(heading.getAttribute('tabindex')).toBe('-1');
    expect(fixture.nativeElement.textContent).toContain('Public route');
  });

  it('keeps a passive state message out of live announcements by default', () => {
    const fixture = TestBed.createComponent(UiState);
    fixture.componentRef.setInput('title', 'No results');
    fixture.componentRef.setInput('message', 'Try changing the filters.');
    fixture.componentRef.setInput('tone', 'empty');
    fixture.detectChanges();

    const state = fixture.nativeElement.querySelector('section') as HTMLElement;
    expect(state.classList.contains('ui-state--empty')).toBe(true);
    expect(state.hasAttribute('role')).toBe(false);
    expect(state.hasAttribute('aria-live')).toBe(false);
  });

  it('uses an alert for explicitly assertive errors', () => {
    const fixture = TestBed.createComponent(UiState);
    fixture.componentRef.setInput('title', 'Could not load');
    fixture.componentRef.setInput('message', 'Try again.');
    fixture.componentRef.setInput('tone', 'error');
    fixture.componentRef.setInput('announcement', 'assertive');
    fixture.detectChanges();

    const state = fixture.nativeElement.querySelector('section') as HTMLElement;
    expect(state.getAttribute('role')).toBe('alert');
    expect(state.getAttribute('aria-live')).toBe('assertive');
    expect(state.textContent).toContain('error');
  });
});
