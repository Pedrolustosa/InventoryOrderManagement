import { Injectable, inject } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { IndividualConfig, ToastrService } from 'ngx-toastr';

@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly toastr = inject(ToastrService);
  private readonly translate = inject(TranslateService);

  success(message: string, title?: string, override?: Partial<IndividualConfig>) {
    return this.toastr.success(message, title, override);
  }

  error(message: string, title?: string, override?: Partial<IndividualConfig>) {
    return this.toastr.error(message, title, override);
  }

  info(message: string, title?: string, override?: Partial<IndividualConfig>) {
    return this.toastr.info(message, title, override);
  }

  warning(message: string, title?: string, override?: Partial<IndividualConfig>) {
    return this.toastr.warning(message, title, override);
  }

  /** Translate a key (optionally with params) then show a success toast. */
  successKey(key: string, params?: Record<string, unknown>, titleKey?: string) {
    return this.success(this.translate.instant(key, params), titleKey ? this.translate.instant(titleKey) : undefined);
  }

  /** Translate a key (optionally with params) then show an error toast. */
  errorKey(key: string, params?: Record<string, unknown>, titleKey?: string) {
    return this.error(this.translate.instant(key, params), titleKey ? this.translate.instant(titleKey) : undefined);
  }
}
