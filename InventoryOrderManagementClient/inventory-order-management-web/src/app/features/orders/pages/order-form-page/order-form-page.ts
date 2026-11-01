import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { CustomerResponse } from '../../../customers/models/customer.model';
import { CustomersService } from '../../../customers/services/customers.service';
import { InventoryBalance } from '../../../inventory/models/inventory-balance.model';
import { InventoryService } from '../../../inventory/services/inventory.service';
import { ProductResponse } from '../../../products/models/product.model';
import { ProductsService } from '../../../products/services/products.service';
import { OrderForm } from '../../components/order-form/order-form';
import { CreateOrderRequest } from '../../models/create-order-request.model';
import { Order } from '../../models/order.model';
import { OrdersService } from '../../services/orders.service';

@Component({
  selector: 'app-order-form-page',
  imports: [RouterLink, OrderForm],
  templateUrl: './order-form-page.html',
})
export class OrderFormPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly ordersService = inject(OrdersService);
  private readonly customersService = inject(CustomersService);
  private readonly productsService = inject(ProductsService);
  private readonly inventoryService = inject(InventoryService);

  readonly orderId = this.route.snapshot.paramMap.get('id');
  readonly customers = signal<CustomerResponse[]>([]);
  readonly products = signal<ProductResponse[]>([]);
  readonly balances = signal<InventoryBalance[]>([]);
  readonly order = signal<Order | null>(null);
  readonly isLoading = signal(true);
  readonly isSubmitting = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly submitError = signal<string | null>(null);

  ngOnInit(): void {
    forkJoin({
      customers: this.customersService.getCustomers(),
      products: this.productsService.getProducts(),
      balances: this.inventoryService.getInventoryBalances(),
    }).subscribe({
      next: ({ customers, products, balances }) => {
        this.customers.set(customers);
        this.products.set(products);
        this.balances.set(balances);
        this.loadOrder();
      },
      error: (error: Error) => {
        this.loadError.set(error.message);
        this.isLoading.set(false);
      },
    });
  }

  save(request: CreateOrderRequest): void {
    if (this.isSubmitting()) {
      return;
    }

    this.isSubmitting.set(true);
    this.submitError.set(null);
    const orderId = this.orderId;

    if (!orderId) {
      this.ordersService.createOrder(request).subscribe({
        next: (created) => {
          this.isSubmitting.set(false);
          void this.router.navigate(['/orders', created.id], {
            state: {
              successMessage: `Order ${created.orderNumber} was saved as ${created.status}.`,
            },
          });
        },
        error: (error: Error) => {
          this.isSubmitting.set(false);
          this.submitError.set(error.message);
        },
      });
      return;
    }

    this.ordersService.updateDraftOrder(orderId, request).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        const orderNumber = this.order()?.orderNumber ?? 'Draft';
        void this.router.navigate(['/orders', orderId], {
          state: { successMessage: `Order ${orderNumber} was updated.` },
        });
      },
      error: (error: Error) => {
        this.isSubmitting.set(false);
        this.submitError.set(error.message);
      },
    });
  }

  back(): void {
    if (this.orderId) {
      void this.router.navigate(['/orders', this.orderId]);
      return;
    }

    void this.router.navigate(['/orders']);
  }

  private loadOrder(): void {
    if (!this.orderId) {
      this.isLoading.set(false);
      return;
    }

    this.ordersService.getOrderById(this.orderId).subscribe({
      next: (order) => {
        if (order.status !== 'Draft') {
          void this.router.navigate(['/orders', order.id]);
          return;
        }

        this.order.set(order);
        this.isLoading.set(false);
      },
      error: (error: Error) => {
        this.loadError.set(error.message);
        this.isLoading.set(false);
      },
    });
  }
}
