import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import { LOCALE_ID } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { AuthService } from '../../../../core/auth/services/auth.service';
import { permittedAuth } from '../../../../core/auth/testing/permitted-auth';
import { provideTestTranslate } from '../../../../core/i18n/testing/provide-test-translate';
import { ToastService } from '../../../../core/notifications/toast.service';
import { createToastServiceSpy } from '../../../../core/notifications/testing/toast-testing';
import { FinanceKind } from '../../../finance/models/finance.enums';
import { FinancialTitle, FinancialTitleSummary } from '../../../finance/models/financial-title.model';
import { FinanceService } from '../../../finance/services/finance.service';
import { ReturnRequest, ReturnStockMovement } from '../../models/return.model';
import { ReturnsService } from '../../services/returns.service';
import { ReturnDetailsPage } from './return-details-page';

registerLocaleData(localePt);

describe('ReturnDetailsPage', () => {
  let fixture: ComponentFixture<ReturnDetailsPage>;
  let financeService: {
    getTitle: ReturnType<typeof vi.fn>;
    getTitles: ReturnType<typeof vi.fn>;
  };

  afterEach(() => {
    TestBed.resetTestingModule();
  });

  function buildReturn(extra: Partial<ReturnRequest> = {}): ReturnRequest {
    return {
      id: 'return-1',
      returnNumber: 'RET-20261108-0001',
      type: 'CustomerReturn',
      status: 'Completed',
      customerId: 'customer-1',
      customerName: 'Jane Doe',
      supplierId: null,
      supplierName: null,
      orderId: 'order-1',
      purchaseOrderId: null,
      sourceReference: 'ORD-1',
      reason: 'Defective',
      reasonDescription: null,
      notes: null,
      estimatedAmount: 150,
      financialTreatment: 'CustomerRefund',
      financialStatus: 'Processed',
      estimatedFinancialAmount: 150,
      financialTitleId: 'refund-1',
      adjustedFinancialTitleId: 'receivable-1',
      financialAdjustedAmount: 100,
      financialNotes: null,
      authorizationReference: null,
      dispatchReference: null,
      createdAtUtc: '2026-11-08T10:00:00Z',
      updatedAtUtc: null,
      receivedAtUtc: '2026-11-08T11:00:00Z',
      inspectedAtUtc: '2026-11-08T12:00:00Z',
      authorizedAtUtc: null,
      dispatchedAtUtc: null,
      completedAtUtc: '2026-11-08T12:00:00Z',
      cancelledAtUtc: null,
      cancellationReason: null,
      createdByUserId: null,
      version: 4,
      items: [],
      replacementItems: [],
      ...extra,
    };
  }

  function summary(id: string, titleNumber: string, totalAmount: number): FinancialTitleSummary {
    return {
      id,
      titleNumber,
      description: 'Title',
      counterpartyType: 'Customer',
      counterpartyId: 'customer-1',
      counterpartyName: 'Jane Doe',
      sourceType: 'Return',
      documentNumber: null,
      issueDate: '2026-11-08',
      totalAmount,
      outstandingBalance: totalAmount,
      status: 'Pending',
      installmentCount: 1,
      dueDate: '2026-12-08',
    };
  }

  async function setup(ret: ReturnRequest): Promise<HTMLElement> {
    const adjusted = {
      ...summary('receivable-1', 'FT-RECV-1', 250),
      sourceType: 'SalesOrder',
      status: 'Cancelled',
      installments: [
        { id: 'inst-1', installmentNumber: 1, dueDate: '2026-12-01', amount: 250, settledPrincipalAmount: 0, outstandingBalance: 0, status: 'Cancelled' },
      ],
      settlements: [],
    } as unknown as FinancialTitle;

    financeService = {
      getTitle: vi.fn().mockReturnValue(of(adjusted)),
      getTitles: vi.fn((kind: FinanceKind) =>
        of({
          items: kind === 'payable' ? [summary('refund-1', 'FT-REFUND-1', 50)] : [],
          page: 1,
          pageSize: 10,
          totalCount: kind === 'payable' ? 1 : 0,
        }),
      ),
    };

    await TestBed.configureTestingModule({
      imports: [ReturnDetailsPage],
      providers: [
        ...provideTestTranslate(),
        provideRouter([]),
        {
          provide: ReturnsService,
          useValue: {
            getById: vi.fn().mockReturnValue(of(ret)),
            getHistory: vi.fn().mockReturnValue(of({ items: [], page: 1, pageSize: 20, totalCount: 0 })),
            getStockMovements: vi.fn().mockReturnValue(of([] as ReturnStockMovement[])),
          },
        },
        { provide: FinanceService, useValue: financeService },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({ id: ret.id }) } },
        },
        { provide: ToastService, useValue: createToastServiceSpy() },
        { provide: LOCALE_ID, useValue: 'pt-BR' },
        { provide: AuthService, useValue: permittedAuth() },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ReturnDetailsPage);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('should list the adjusted and generated titles with links to finance', async () => {
    const element = await setup(buildReturn());

    expect(financeService.getTitle).toHaveBeenCalledWith('receivable', 'receivable-1');
    expect(financeService.getTitles).toHaveBeenCalledWith('payable', expect.objectContaining({ sourceId: 'return-1' }));
    expect(element.textContent).toContain('FT-RECV-1');
    expect(element.textContent).toContain('FT-REFUND-1');
    expect(element.querySelectorAll('app-financial-status').length).toBe(2);
    expect(element.querySelector('a[href="/finance/receivables/receivable-1"]')).not.toBeNull();
    expect(element.querySelector('a[href="/finance/payables/refund-1"]')).not.toBeNull();
  });

  it('should show exchange replacement items', async () => {
    const element = await setup(
      buildReturn({
        type: 'Exchange',
        adjustedFinancialTitleId: null,
        financialTitleId: null,
        replacementItems: [
          {
            id: 'rep-1',
            productId: 'product-2',
            productSku: 'SKU-NEW',
            productName: 'Replacement Mouse',
            quantity: 1,
            unitPriceSnapshot: 120,
            lineAmount: 120,
          },
        ],
      }),
    );

    expect(element.textContent).toContain('Replacement Mouse');
    expect(element.textContent).toContain('SKU-NEW');
    expect(financeService.getTitle).not.toHaveBeenCalled();
  });
});
