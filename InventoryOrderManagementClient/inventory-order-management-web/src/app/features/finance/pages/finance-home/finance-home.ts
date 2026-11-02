import { Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { firstFinancePath } from '../../../../core/auth/finance-landing';
import { AuthService } from '../../../../core/auth/services/auth.service';

@Component({
  selector: 'app-finance-home',
  template: '',
})
export class FinanceHome implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  ngOnInit(): void {
    void this.router.navigateByUrl(firstFinancePath(this.auth.getCurrentUser()?.permissions ?? []));
  }
}
