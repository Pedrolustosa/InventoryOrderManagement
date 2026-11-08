import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { Observable, map, of, switchMap } from 'rxjs';
import { LanguageService } from '../../../../core/i18n/language.service';
import {
  OrderTimelineEntry,
  timelineEventLabel,
  timelineModuleLabel,
  timelineStatusLabel,
} from '../../models/order-timeline.model';

/**
 * Minimal timeline step: module icon, translated title, local time and status. The full card (summary values
 * and links to each module) comes later.
 */
@Component({
  selector: 'app-timeline-step',
  imports: [DatePipe],
  templateUrl: './timeline-step.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TimelineStep {
  private readonly translate = inject(TranslateService);
  private readonly languageService = inject(LanguageService);

  readonly entry = input.required<OrderTimelineEntry>();

  readonly localeId = this.languageService.localeId;

  /** Translated event title; an unknown code falls back to the code itself instead of the raw key. */
  readonly title = toSignal(
    toObservable(computed(() => this.entry().eventType)).pipe(
      switchMap((eventType) => this.translateOr(timelineEventLabel(eventType), eventType)),
    ),
    { initialValue: '' },
  );

  readonly moduleLabel = toSignal(
    toObservable(computed(() => this.entry().module)).pipe(
      switchMap((module) => this.translateOr(timelineModuleLabel(module), module)),
    ),
    { initialValue: '' },
  );

  readonly statusText = toSignal(
    toObservable(computed(() => ({ module: this.entry().module, status: this.entry().status }))).pipe(
      switchMap(({ module, status }) =>
        status ? this.translateOr(timelineStatusLabel(module, status), status) : of(null),
      ),
    ),
    { initialValue: null },
  );

  private translateOr(key: string, fallback: string): Observable<string> {
    return this.translate
      .stream(key)
      .pipe(map((value: unknown) => (typeof value === 'string' && value !== key ? value : fallback)));
  }
}
