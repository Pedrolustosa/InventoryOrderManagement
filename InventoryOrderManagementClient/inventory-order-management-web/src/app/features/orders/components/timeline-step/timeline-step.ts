import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable, map, of, switchMap } from 'rxjs';
import { AuthService } from '../../../../core/auth/services/auth.service';
import { LanguageService } from '../../../../core/i18n/language.service';
import { OrderTimelineEntry, timelineModuleLabel } from '../../models/order-timeline.model';
import { presentTimelineStep } from './timeline-step.presenter';

/** Number of details shown before the "show more" toggle. */
export const TIMELINE_STEP_VISIBLE_DETAILS = 4;

let nextStepId = 0;

/**
 * Presentational card of one timeline entry: everything shown comes from the pure presenter
 * (timeline-step.presenter.ts). It only reads the signed-in user's permissions to decide whether the reference
 * is a link; it never loads data.
 */
@Component({
  selector: 'app-timeline-step',
  imports: [CurrencyPipe, DatePipe, DecimalPipe, RouterLink, TranslatePipe],
  templateUrl: './timeline-step.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TimelineStep {
  private readonly translate = inject(TranslateService);
  private readonly languageService = inject(LanguageService);
  private readonly auth = inject(AuthService);

  readonly entry = input.required<OrderTimelineEntry>();

  readonly localeId = this.languageService.localeId;
  readonly detailsId = `timeline-step-details-${nextStepId++}`;
  readonly expanded = signal(false);

  readonly view = computed(() => presentTimelineStep(this.entry()));

  readonly visibleDetails = computed(() => {
    const details = this.view().details;
    return this.expanded() ? details : details.slice(0, TIMELINE_STEP_VISIBLE_DETAILS);
  });

  readonly hiddenDetailCount = computed(() =>
    Math.max(0, this.view().details.length - TIMELINE_STEP_VISIBLE_DETAILS),
  );

  /** The link only renders when the user may open the destination route. */
  readonly canFollowLink = computed(() => {
    const link = this.view().link;
    return link !== null && this.auth.currentUser()?.permissions.includes(link.permission) === true;
  });

  /** Translated event title; an unknown code falls back to the code itself instead of the raw key. */
  readonly title = toSignal(
    toObservable(this.view).pipe(switchMap((view) => this.translateOr(view.titleKey, view.fallbackTitle))),
    { initialValue: '' },
  );

  readonly moduleLabel = toSignal(
    toObservable(computed(() => this.entry().module)).pipe(
      switchMap((module) => this.translateOr(timelineModuleLabel(module), module)),
    ),
    { initialValue: '' },
  );

  readonly statusText = toSignal(
    toObservable(computed(() => ({ key: this.view().statusKey, status: this.entry().status }))).pipe(
      switchMap(({ key, status }) => (key && status ? this.translateOr(key, status) : of(null))),
    ),
    { initialValue: null },
  );

  toggleDetails(): void {
    this.expanded.update((value) => !value);
  }

  private translateOr(key: string, fallback: string): Observable<string> {
    return this.translate
      .stream(key)
      .pipe(map((value: unknown) => (typeof value === 'string' && value !== key ? value : fallback)));
  }
}
