import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import { LOCALE_ID } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { AuthService } from '../../../../core/auth/services/auth.service';
import { permittedAuth } from '../../../../core/auth/testing/permitted-auth';
import { CustomersService } from '../../../customers/services/customers.service';
import { OrdersService } from '../../../orders/services/orders.service';
import { PagedResult } from '../../models/shipment-requests.model';
import { ShipmentSummary } from '../../models/shipment.model';
import { ShipmentsService } from '../../services/shipments.service';
import { ShipmentsPage } from './shipments-page';
import { provideTestTranslate } from '../../../../core/i18n/testing/provide-test-translate';


registerLocaleData(localePt);

describe('ShipmentsPage', () => {
  let fixture: ComponentFixture<ShipmentsPage>;
  let component: ShipmentsPage;
  let shipmentsService: {
    getAll: ReturnType<typeof vi.fn>;
  };

  const summary: ShipmentSummary = {
    id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
    orderId: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
    orderNumber: 'ORD-1',
    customerId: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
    customerName: 'Jane',
    status: 'Preparing',
    carrierName: 'Acme',
    trackingCode: 'TRK-1',
    createdAtUtc: '2026-11-02T12:00:00Z',
    shippedAtUtc: null,
    deliveredAtUtc: null,
  };

  const page: PagedResult<ShipmentSummary> = {
    items: [summary],
    page: 1,
    pageSize: 20,
    totalItems: 1,
    totalPages: 1,
  };

  const emptyPage: PagedResult<ShipmentSummary> = {
    items: [],
    page: 1,
    pageSize: 20,
    totalItems: 0,
    totalPages: 0,
  };

  beforeEach(async () => {
    shipmentsService = {
      getAll: vi.fn((filters) => {
        if (filters.status) {
          return of({
            ...emptyPage,
            totalItems: filters.status === 'Preparing' ? 1 : 0,
            pageSize: 1,
          });
        }
        return of(page);
      }),
    };

    await TestBed.configureTestingModule({
      imports: [ShipmentsPage],
      providers: [...provideTestTranslate(), 
        provideRouter([]),
        { provide: ShipmentsService, useValue: shipmentsService },
        { provide: CustomersService, useValue: { getCustomers: vi.fn().mockReturnValue(of([])) } },
        { provide: OrdersService, useValue: { getOrders: vi.fn().mockReturnValue(of([])) } },
        { provide: LOCALE_ID, useValue: 'pt-BR' },
        { provide: AuthService, useValue: permittedAuth() },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ShipmentsPage);
    component = fixture.componentInstance;
  });

  afterEach(() => {
    TestBed.resetTestingModule();
  });

  it('should load shipments and status indicator counts', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(component.shipments()).toEqual([summary]);
    expect(component.statusCounts().Preparing).toBe(1);
    expect(fixture.nativeElement.textContent).toContain('ORD-1');
    expect(fixture.nativeElement.textContent).toContain('Acme');
  });

  it('should show an empty state', async () => {
    shipmentsService.getAll.mockReturnValue(of(emptyPage));
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('No shipments found');
  });

  it('should show load errors', async () => {
    shipmentsService.getAll.mockReturnValue(throwError(() => new Error('Unable to reach the API.')));
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Unable to reach the API.');
  });
});
