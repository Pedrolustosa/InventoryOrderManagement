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

  async function setup(shipment: Shipment, permissions?: string[]): Promise<void> {
    shipmentsService = {
      getById: vi.fn().mockReturnValue(of(shipment)),
      getHistory: vi.fn().mockReturnValue(of(shipment.history)),
      dispatch: vi.fn().mockReturnValue(of({ ...shipment, status: 'Shipped' })),
    };

    await TestBed.configureTestingModule({
      imports: [ShipmentDetailsPage],
      providers: [...provideTestTranslate(), 
        provideRouter([]),
        { provide: ShipmentsService, useValue: shipmentsService },
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
