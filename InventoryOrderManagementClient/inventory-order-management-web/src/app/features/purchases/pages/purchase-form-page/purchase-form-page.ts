import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { ProductResponse } from '../../../products/models/product.model';
import { ProductsService } from '../../../products/services/products.service';
import { SupplierResponse } from '../../../suppliers/models/supplier.model';
import { SuppliersService } from '../../../suppliers/services/suppliers.service';
import { PurchaseOrderForm } from '../../components/purchase-order-form/purchase-order-form';
import { CreatePurchaseOrderRequest } from '../../models/purchase-order-requests.model';
import { PurchaseOrder } from '../../models/purchase-order.model';
import { PurchaseRequestError, PurchasesService } from '../../services/purchases.service';

@Component({
  selector: 'app-purchase-form-page',
  imports: [RouterLink, PurchaseOrderForm],
  templateUrl: './purchase-form-page.html',
})
export class PurchaseFormPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly purchasesService = inject(PurchasesService);
  private readonly suppliersService = inject(SuppliersService);
  private readonly productsService = inject(ProductsService);

  readonly orderId = this.route.snapshot.paramMap.get('id');
  readonly suppliers = signal<SupplierResponse[]>([]);
  readonly products = signal<ProductResponse[]>([]);
  readonly order = signal<PurchaseOrder | null>(null);
  readonly isLoading = signal(true);
  readonly isSubmitting = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly submitError = signal<string | null>(null);

  ngOnInit(): void {
    forkJoin({
      suppliers: this.suppliersService.getSuppliers({ isActive: true }),
      products: this.productsService.getProducts(),
    }).subscribe({
      next: ({ suppliers, products }) => {
        this.suppliers.set(suppliers);
        this.products.set(products);
        this.loadOrder();
      },
      error: (error: Error) => {
        this.loadError.set(error.message);
        this.isLoading.set(false);
      },
    });
  }

  save(request: CreatePurchaseOrderRequest): void {
    if (this.isSubmitting()) {
      return;
    }

    this.isSubmitting.set(true);
    this.submitError.set(null);
    const orderId = this.orderId;

    if (!orderId) {
      this.purchasesService.create(request).subscribe({
        next: (created) => {
          this.isSubmitting.set(false);
          void this.router.navigate(['/purchases', created.id], {
            state: {
              successMessage: `Purchase order ${created.purchaseOrderNumber} was saved as ${created.status}.`,
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

    this.purchasesService
      .update(orderId, { notes: request.notes, items: request.items })
      .subscribe({
        next: (updated) => {
          this.isSubmitting.set(false);
          void this.router.navigate(['/purchases', orderId], {
            state: { successMessage: `Purchase order ${updated.purchaseOrderNumber} was updated.` },
          });
        },
        error: (error: PurchaseRequestError) => {
          this.isSubmitting.set(false);
          this.submitError.set(error.message);
          if (error.status === 409) {
            this.loadOrder();
          }
        },
      });
  }

  back(): void {
    if (this.orderId) {
      void this.router.navigate(['/purchases', this.orderId]);
      return;
    }

    void this.router.navigate(['/purchases']);
  }

  private loadOrder(): void {
    if (!this.orderId) {
      this.isLoading.set(false);
      return;
    }

    this.purchasesService.getById(this.orderId).subscribe({
      next: (order) => {
        if (order.status !== 'Draft') {
          void this.router.navigate(['/purchases', order.id]);
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
