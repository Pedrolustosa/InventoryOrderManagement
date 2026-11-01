import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import { LOCALE_ID } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CustomerResponse } from '../../../customers/models/customer.model';
import { InventoryBalance } from '../../../inventory/models/inventory-balance.model';
import { ProductResponse } from '../../../products/models/product.model';
import { CreateOrderRequest } from '../../models/create-order-request.model';
import { OrderForm } from './order-form';

registerLocaleData(localePt);

describe('OrderForm', () => {
  let fixture: ComponentFixture<OrderForm>;
  let component: OrderForm;
  const saved: CreateOrderRequest[] = [];

  const activeCustomer: CustomerResponse = {
    id: 'customer-1',
    customerType: 'Individual',
    name: 'John Smith',
    taxIdentifier: '123',
    email: 'john@example.com',
    phone: '999',
    address: {
      postalCode: '01310-100',
      street: 'Avenida Paulista',
      number: '1000',
      complement: null,
      neighborhood: null,
      city: 'Sao Paulo',
      state: 'SP',
      country: 'Brazil',
    },
    isActive: true,
    createdAtUtc: '2026-11-01T12:00:00Z',
    updatedAtUtc: null,
  };

  const customerWithoutAddress: CustomerResponse = {
    ...activeCustomer,
    id: 'customer-2',
    name: 'Walk-in Guest',
    address: null,
  };

  const inactiveCustomer: CustomerResponse = {
    ...activeCustomer,
    id: 'customer-3',
    name: 'Closed Store',
    isActive: false,
  };

  const mouse: ProductResponse = {
    id: 'product-1',
    categoryId: 'category-1',
    sku: 'MS-001',
    name: 'Wireless Mouse',
    description: null,
    currentPrice: 10,
    isActive: true,
    createdAtUtc: '2026-11-01T12:00:00Z',
  };

  const inactiveProduct: ProductResponse = {
    ...mouse,
    id: 'product-2',
    sku: 'OLD-001',
    name: 'Retired Item',
    currentPrice: 5,
    isActive: false,
  };

  const balances: InventoryBalance[] = [
    {
      productId: mouse.id,
      sku: mouse.sku,
      productName: mouse.name,
      quantityOnHand: 4,
      minimumStockLevel: 1,
      isLowStock: false,
    },
  ];

  beforeEach(async () => {
    saved.length = 0;
    await TestBed.configureTestingModule({
      imports: [OrderForm],
      providers: [{ provide: LOCALE_ID, useValue: 'pt-BR' }],
    }).compileComponents();

    fixture = TestBed.createComponent(OrderForm);
    component = fixture.componentInstance;
    component.saved.subscribe((request) => saved.push(request));
    fixture.componentRef.setInput('customers', [activeCustomer, customerWithoutAddress, inactiveCustomer]);
    fixture.componentRef.setInput('products', [mouse, inactiveProduct]);
    fixture.componentRef.setInput('balances', balances);
    fixture.detectChanges();
  });

  it('should hide inactive customers and products', () => {
    const text = fixture.nativeElement.textContent as string;
    expect(text).not.toContain('Closed Store');
    expect(text).not.toContain('Retired Item');
    expect(component.activeCustomers().map((customer) => customer.id)).toEqual(['customer-1', 'customer-2']);
    expect(component.activeProducts().map((product) => product.id)).toEqual([mouse.id]);
  });

  it('should save a draft with the registered address and without prices', () => {
    component.form.controls.customerId.setValue(activeCustomer.id);
    component.itemForm.setValue({ productId: mouse.id, quantity: 2 });
    component.addItem();
    component.submit();

    expect(saved).toEqual([
      {
        customerId: activeCustomer.id,
        shippingAddress: null,
        items: [{ productId: mouse.id, quantity: 2 }],
      },
    ]);
    expect(component.estimatedSubtotal()).toBe(20);
    expect(component.totalItems()).toBe(2);
    expect(JSON.stringify(saved[0])).not.toContain('unitPrice');
    expect(JSON.stringify(saved[0])).not.toContain('subtotal');
  });

  it('should reject a missing customer and an order without items', () => {
    component.submit();
    expect(saved).toEqual([]);

    component.form.controls.customerId.setValue(activeCustomer.id);
    component.submit();
    expect(component.itemError()).toBe('Add at least one product to the order.');
    expect(saved).toEqual([]);
  });

  it('should reject an inactive customer', () => {
    component.form.controls.customerId.setValue(inactiveCustomer.id);
    component.itemForm.setValue({ productId: mouse.id, quantity: 1 });
    component.addItem();
    component.submit();

    expect(component.itemError()).toBe('Inactive customers cannot place orders.');
    expect(saved).toEqual([]);
  });

  it('should reject an invalid quantity and a duplicate product', () => {
    component.itemForm.setValue({ productId: mouse.id, quantity: 0 });
    component.addItem();
    expect(component.items()).toEqual([]);

    component.itemForm.setValue({ productId: mouse.id, quantity: 1 });
    component.addItem();
    component.itemForm.setValue({ productId: mouse.id, quantity: 3 });
    component.addItem();

    expect(component.items()).toHaveLength(1);
    expect(component.itemError()).toBe('The same product cannot be added more than once.');
  });

  it('should require a shipping address when the customer has none', () => {
    component.form.controls.customerId.setValue(customerWithoutAddress.id);
    component.itemForm.setValue({ productId: mouse.id, quantity: 1 });
    component.addItem();
    component.submit();
    expect(saved).toEqual([]);

    component.form.patchValue({
      postalCode: '50000-000',
      street: 'Example Street',
      number: '10',
      city: 'Recife',
      state: 'PE',
      country: 'Brazil',
    });
    component.submit();

    expect(saved[0]?.shippingAddress).toEqual({
      postalCode: '50000-000',
      street: 'Example Street',
      number: '10',
      complement: null,
      neighborhood: null,
      city: 'Recife',
      state: 'PE',
      country: 'Brazil',
    });
  });

  it('should ignore a second submit while saving', () => {
    fixture.componentRef.setInput('isSubmitting', true);
    fixture.detectChanges();
    component.form.controls.customerId.setValue(activeCustomer.id);
    component.itemForm.setValue({ productId: mouse.id, quantity: 1 });
    component.addItem();
    component.submit();

    expect(saved).toEqual([]);
  });
});
