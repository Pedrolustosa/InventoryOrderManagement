import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { PagedResult } from '../../../users/models/user.model';
import { SecurityAuditLog } from '../../models/audit.model';
import { AuditService } from '../../services/audit.service';

@Component({
  selector: 'app-audit-page',
  imports: [DatePipe, ReactiveFormsModule],
  templateUrl: './audit-page.html',
})
export class AuditPage implements OnInit {
  private readonly auditService = inject(AuditService);
  private readonly formBuilder = inject(FormBuilder);

  readonly logs = signal<SecurityAuditLog[]>([]);
  readonly page = signal(1);
  readonly totalPages = signal(1);
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly filters = this.formBuilder.nonNullable.group({
    actorUserId: '',
    targetUserId: '',
    action: '',
    entityType: '',
    startDate: '',
    endDate: '',
  });

  ngOnInit(): void {
    this.load();
  }

  load(page = this.page()): void {
    const value = this.filters.getRawValue();
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.auditService
      .getLogs({
        actorUserId: value.actorUserId.trim() || undefined,
        targetUserId: value.targetUserId.trim() || undefined,
        action: value.action.trim() || undefined,
        entityType: value.entityType.trim() || undefined,
        startDateUtc: this.toUtc(value.startDate, false),
        endDateUtc: this.toUtc(value.endDate, true),
        page,
        pageSize: 20,
      })
      .subscribe({
        next: (result: PagedResult<SecurityAuditLog>) => {
          this.logs.set(result.items);
          this.page.set(result.page);
          this.totalPages.set(Math.max(result.totalPages, 1));
          this.isLoading.set(false);
        },
        error: (error: Error) => {
          this.errorMessage.set(error.message);
          this.isLoading.set(false);
        },
      });
  }

  private toUtc(date: string, exclusiveEnd: boolean): string | undefined {
    if (!date) {
      return undefined;
    }

    const instant = new Date(`${date}T00:00:00`);
    if (exclusiveEnd) {
      instant.setDate(instant.getDate() + 1);
    }

    return instant.toISOString();
  }
}
