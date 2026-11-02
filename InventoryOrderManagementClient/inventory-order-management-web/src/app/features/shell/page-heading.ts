import { Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { filter, map, startWith } from 'rxjs';

interface Crumb {
  labelKey: string;
  url: string;
}

@Component({
  selector: 'app-page-heading',
  imports: [RouterLink, TranslatePipe],
  templateUrl: './page-heading.html',
})
export class PageHeading {
  private readonly router = inject(Router);

  private readonly trail = toSignal(
    this.router.events.pipe(
      filter((event) => event instanceof NavigationEnd),
      startWith(null),
      map(() => this.readTrail()),
    ),
    { initialValue: this.readTrail() },
  );

  readonly titleKey = computed(() => this.trail().titleKey);
  readonly crumbs = computed(() => this.trail().crumbs);

  private readTrail(): { titleKey: string; crumbs: Crumb[] } {
    const crumbs: Crumb[] = [];
    let titleKey = '';
    let url = '';
    let route = this.router.routerState.snapshot.root;

    while (route.firstChild) {
      route = route.firstChild;
      const segment = route.url.map((part) => part.path).filter(Boolean).join('/');
      if (segment) {
        url += `/${segment}`;
      }

      // Child snapshots inherit parent data. Read the route's own config so a list page is not repeated.
      const ownData = route.routeConfig?.data;
      const breadcrumb = ownData?.['breadcrumb'];
      if (typeof breadcrumb === 'string' && breadcrumb.length > 0) {
        crumbs.push({ labelKey: breadcrumb, url: url || '/' });
      }

      const routeTitle = ownData?.['title'];
      if (typeof routeTitle === 'string' && routeTitle.length > 0) {
        titleKey = routeTitle;
      }
    }

    return { titleKey: titleKey || crumbs.at(-1)?.labelKey || '', crumbs };
  }
}
