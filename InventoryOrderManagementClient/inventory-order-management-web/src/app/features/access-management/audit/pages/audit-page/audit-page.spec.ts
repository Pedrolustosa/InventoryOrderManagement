import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { AuditService } from '../../services/audit.service';
import { AuditPage } from './audit-page';

describe('AuditPage', () => {
  let fixture: ComponentFixture<AuditPage>;
  let auditService: { getLogs: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    auditService = {
      getLogs: vi.fn().mockReturnValue(of({ items: [], page: 1, pageSize: 20, totalItems: 0, totalPages: 0 })),
    };
    await TestBed.configureTestingModule({
      imports: [AuditPage],
      providers: [{ provide: AuditService, useValue: auditService }],
    }).compileComponents();
    fixture = TestBed.createComponent(AuditPage);
    fixture.detectChanges();
  });

  it('includes the chosen end day by sending the following local midnight', () => {
    fixture.componentInstance.filters.setValue({
      actorUserId: '',
      targetUserId: '',
      action: '',
      entityType: '',
      startDate: '2026-11-01',
      endDate: '2026-11-01',
    });
    fixture.componentInstance.load(1);
    const query = auditService.getLogs.mock.calls.at(-1)?.[0] as { startDateUtc: string; endDateUtc: string };
    expect(query.endDateUtc > query.startDateUtc).toBe(true);
  });
});
