import { Component, computed, inject, input, output } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { filter, map, startWith } from 'rxjs';
import { AuthService } from '../../core/auth/services/auth.service';
import { resolveActiveNavRoute } from './nav-active';
import { NavIcon } from './nav-icon';
import { NAVIGATION_GROUPS } from './navigation';

@Component({
  selector: 'app-sidebar',
  imports: [RouterLink, NavIcon, TranslatePipe],
  templateUrl: './sidebar.html',
})
export class Sidebar {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly collapsed = input(false);
  readonly navigated = output<void>();

  readonly groups = computed(() =>
    NAVIGATION_GROUPS.map((group) => ({
      ...group,
      items: group.items.filter((item) => this.auth.hasPermission(item.permission)),
    })).filter((group) => group.items.length > 0),
  );

  private readonly currentUrl = toSignal(
    this.router.events.pipe(
      filter((event) => event instanceof NavigationEnd),
      startWith(null),
      map(() => this.router.url),
    ),
    { initialValue: this.router.url },
  );

  readonly activeRoute = computed(() => {
    const routes = this.groups().flatMap((group) => group.items.map((item) => item.route));
    return resolveActiveNavRoute(this.currentUrl(), routes);
  });

  isActive(route: string): boolean {
    return this.activeRoute() === route;
  }
}
