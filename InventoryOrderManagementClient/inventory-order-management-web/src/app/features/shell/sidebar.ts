import { Component, computed, inject, input, output } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../core/auth/services/auth.service';
import { NavIcon } from './nav-icon';
import { NAVIGATION_GROUPS } from './navigation';

@Component({
  selector: 'app-sidebar',
  imports: [RouterLink, RouterLinkActive, NavIcon],
  templateUrl: './sidebar.html',
})
export class Sidebar {
  private readonly auth = inject(AuthService);

  readonly collapsed = input(false);
  readonly navigated = output<void>();

  readonly groups = computed(() =>
    NAVIGATION_GROUPS.map((group) => ({
      ...group,
      items: group.items.filter((item) => this.auth.hasPermission(item.permission)),
    })).filter((group) => group.items.length > 0),
  );
}
