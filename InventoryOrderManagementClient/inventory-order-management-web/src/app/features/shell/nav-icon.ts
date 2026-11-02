import { Component, input } from '@angular/core';
import { NavIconName } from './navigation';

@Component({
  selector: 'app-nav-icon',
  template: `
    <svg
      viewBox="0 0 24 24"
      class="size-5 shrink-0"
      fill="none"
      stroke="currentColor"
      stroke-width="1.75"
      stroke-linecap="round"
      stroke-linejoin="round"
      aria-hidden="true"
    >
      @switch (name()) {
        @case ('box') {
          <path d="M21 8l-9-5-9 5 9 5 9-5z" />
          <path d="M3 8v8l9 5 9-5V8" />
          <path d="M12 13v8" />
        }
        @case ('tag') {
          <path d="M20 13l-7 7-9-9V4h7l9 9z" />
          <path d="M7.5 7.5h.01" />
        }
        @case ('truck') {
          <path d="M3 7h11v10H3z" />
          <path d="M14 10h4l3 3v4h-7" />
          <circle cx="7" cy="18" r="1.5" />
          <circle cx="18" cy="18" r="1.5" />
        }
        @case ('users') {
          <path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2" />
          <circle cx="9" cy="7" r="3" />
          <path d="M22 21v-2a4 4 0 0 0-3-3.87" />
          <path d="M16 3.13a3 3 0 0 1 0 5.75" />
        }
        @case ('archive') {
          <path d="M3 7h18" />
          <path d="M5 7l1 13h12l1-13" />
          <path d="M9 11h6" />
        }
        @case ('bag') {
          <path d="M6 7h12l-1 13H7L6 7z" />
          <path d="M9 7a3 3 0 0 1 6 0" />
        }
        @case ('clipboard') {
          <path d="M9 4h6l1 2H8l1-2z" />
          <path d="M8 6H6v14h12V6h-2" />
          <path d="M9 12h6" />
          <path d="M9 16h6" />
        }
        @case ('chart') {
          <path d="M4 19V5" />
          <path d="M4 19h16" />
          <path d="M8 15l3-4 3 2 4-6" />
        }
        @case ('user') {
          <circle cx="12" cy="8" r="3" />
          <path d="M5 20a7 7 0 0 1 14 0" />
        }
        @case ('shield') {
          <path d="M12 3l8 3v6c0 5-3.5 7.5-8 9-4.5-1.5-8-4-8-9V6l8-3z" />
        }
        @case ('key') {
          <circle cx="8" cy="15" r="3" />
          <path d="M10.5 13.5L20 4" />
          <path d="M16 8l2 2" />
          <path d="M18 6l2 2" />
        }
        @case ('scroll') {
          <path d="M8 4h9a2 2 0 0 1 2 2v14l-2-1-2 1-2-1-2 1-2-1-2 1V6a2 2 0 0 1 2-2z" />
          <path d="M10 9h6" />
          <path d="M10 13h6" />
        }
      }
    </svg>
  `,
})
export class NavIcon {
  readonly name = input.required<NavIconName>();
}
