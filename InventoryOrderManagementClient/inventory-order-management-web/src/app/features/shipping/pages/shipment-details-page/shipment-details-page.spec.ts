import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import { LOCALE_ID } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { AuthService } from '../../../../core/auth/services/auth.service';
import { permittedAuth, permittedUser } from '../../../../core/auth/testing/permitted-auth';
import { ToastService } from '../../../../core/notifications/toast.service';
import { createToastServiceSpy } from '../../../../core/notifications/testing/toast-testing';
import { FinancialTitleSummary } from '../../../finance/models/financial-title.model';
import { FinanceService } from '../../../finance/services/finance.service';
import { OrdersService } from '../../../orders/services/orders.service';
import { Shipment } from '../../models/shipment.model';
import { ShipmentsService } from '../../services/shipments.service';
import { ShipmentDetailsPage } from './shipment-details-page';
import { provideTestTranslate } from '../../../../core/i18n/testing/provide-test-translate';


registerLocaleData(localePt);

describe('ShipmentDetailsPage', () => {
  let fixture: ComponentFixture<ShipmentDetailsPage>;
  let component: ShipmentDetailsPage;
  let shipmentsService: {
    getById: ReturnType<typeof vi.fn>;
    getHistory: ReturnType<typeof vi.fn>;
    dispatch: ReturnType<typeof vi.fn>;
  };
  let financeService: {
    getTitles: ReturnType<typeof vi.fn>;
  };

  function buildShipment(status: Shipment['status'] = 'Preparing'): Shipment {
    return {
      id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
      orderId: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
      orderNumber: 'ORD-1',
      customerId: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
      customerName: 'Jane',
      status,
      carrierName: 'Acme',
      trackingCode: 'TRK-1',
      notes: 'Ready',
      failureReason: status === 'Failed' ? 'Customer absent' : null,
      createdAtUtc: '2026-11-02T12:00:00Z',
      updatedAtUtc: null,
      shippedAtUtc: status === 'Preparing' ? null : '2026-11-02T13:00:00Z',
      deliveredAtUtc: status === 'Delivered' ? '2026-11-02T15:00:00Z' : null,
      createdByUserId: null,
      version: 0,
      shippingAddress: {
        postalCode: '01000-000',
        street: 'Main',
        number: '1',
        complement: null,
        neighborhood: null,
        city: 'Sao Paulo',
        state: 'SP',
        country: 'BR',
      },
      history: [
        {
          id: 'history-1',
          previousStatus: null,
          newStatus: 'Preparing',
          notes: 'Ready',
          occurredAtUtc: '2026-11-02T12:00:00Z',
          changedByUserId: null,
        },
      ],
    };
  }

  async function setup(
    shipment: Shipment,
    permissions?: string[],
    receivable: FinancialTitleSummary | null = null,
  ): Promise<void> {
    shipmentsService = {
      getById: vi.fn().mockReturnValue(of(shipment)),
      getHistory: vi.fn().mockReturnValue(of(shipment.history)),
      dispatch: vi.fn().mockReturnValue(of({ ...shipment, status: 'Shipped' })),
    };
    financeService = {
      getTitles: vi.fn().mockReturnValue(
        of({ items: receivable ? [receivable] : [], page: 1, pageSize: 1, totalCount: receivable ? 1 : 0 }),
      ),
    };

    await TestBed.configureTestingModule({
      imports: [ShipmentDetailsPage],
      providers: [...provideTestTranslate(),
        provideRouter([]),
        { provide: ShipmentsService, useValue: shipmentsService },
        { provide: FinanceService, useValue: financeService },
        {
          provide: OrdersService,
          useValue: {
            getOrderById: vi.fn().mockReturnValue(
              of({
                ...shipment,
                id: shipment.orderId,
                status: 'Confirmed',
                subtotal: 50,
                items: [],
                shippingAddress: shipment.shippingAddress,
                updatedAtUtc: null,
                confirmedAtUtc: '2026-11-02T11:00:00Z',
                shippedAtUtc: null,
                deliveredAtUtc: null,
                cancelledAtUtc: null,
                cancellationReason: null,
              }),
            ),
          },
        },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({ id: shipment.id }) } },
        },
        { provide: ToastService, useValue: createToastServiceSpy() },
        { provide: LOCALE_ID, useValue: 'pt-BR' },
        {
          provide: AuthService,
          useValue: permissions ? permittedAuth(permittedUser({ permissions })) : permittedAuth(),
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ShipmentDetailsPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  afterEach(() => {
    TestBed.resetTestingModule();
  });

  it('should show preparing actions and timeline history', async () => {
    await setup(buildShipment('Preparing'));
    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('ORD-1');
    expect(text).toContain('Dispatch');
    expect(text).toContain('Edit transport');
    expect(text).toContain('Status history');
    expect(text).toContain('Preparing');
    expect(component.canDispatch()).toBe(true);
    expect(component.canDeliver()).toBe(false);
  });

  it('should show the generated receivable with a link to finance', async () => {
    const shipment = buildShipment('Shipped');
    await setup(shipment, undefined, {
      id: 'title-9',
      titleNumber: 'FT-20261108-FFFF0000',
      description: 'Receivable for sales order',
      counterpartyType: 'Customer',
      counterpartyId: shipment.customerId,
      counterpartyName: 'Customer',
      sourceType: 'SalesOrder',
      documentNumber: 'SO-ORDER',
      issueDate: '2026-11-08',
      totalAmount: 50,
      outstandingBalance: 50,
      status: 'Pending',
      installmentCount: 1,
      dueDate: '2026-12-08',
    });
    const element = fixture.nativeElement as HTMLElement;

    expect(financeService.getTitles).toHaveBeenCalledWith(
      'receivable',
      expect.objectContaining({ sourceId: shipment.orderId }),
    );
    expect(element.textContent).toContain('FT-20261108-FFFF0000');
    expect(element.textContent).toContain('08/12/2026');
    expect(element.querySelector('app-financial-status')).not.toBeNull();
    expect(element.querySelector('a[href="/finance/receivables/title-9"]')).not.toBeNull();
  });

  it('should show advance actions for shipped shipments', async () => {
    await setup(buildShipment('Shipped'));
    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('Mark in transit');
    expect(text).toContain('Confirm delivery');
    expect(text).toContain('Register failure');
    expect(text).not.toContain('Dispatch');
  });

  it('should hide mutation actions without permissions', async () => {
    await setup(buildShipment('Preparing'), ['Shipments.View']);
    const text = fixture.nativeElement.textContent as string;

    expect(text).not.toContain('Dispatch');
    expect(text).not.toContain('Edit transport');
  });
});
