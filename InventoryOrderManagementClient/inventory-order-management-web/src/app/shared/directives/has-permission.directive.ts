import { Directive, TemplateRef, ViewContainerRef, effect, inject, input } from '@angular/core';
import { AuthService } from '../../core/auth/services/auth.service';

@Directive({
  selector: '[hasPermission]',
  standalone: true,
})
export class HasPermissionDirective {
  private readonly template = inject(TemplateRef<unknown>);
  private readonly view = inject(ViewContainerRef);
  private readonly auth = inject(AuthService);
  readonly permission = input('', { alias: 'hasPermission' });
  private rendered = false;

  constructor() {
    effect(() => {
      const allowed = this.auth.currentUser()?.permissions.includes(this.permission()) === true;
      if (allowed && !this.rendered) {
        this.view.createEmbeddedView(this.template);
        this.rendered = true;
      } else if (!allowed && this.rendered) {
        this.view.clear();
        this.rendered = false;
      }
    });
  }
}
