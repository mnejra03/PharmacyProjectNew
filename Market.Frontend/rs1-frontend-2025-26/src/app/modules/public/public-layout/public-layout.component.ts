import { Component, OnInit, inject } from '@angular/core';
import { FormControl, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { Product, StoreApiService } from '../../../api-services/store/store-api.service';
import { NotificationsApiService } from '../../../api-services/notifications/notifications-api.service';
import { AuthFacadeService } from '../../../core/services/auth/auth-facade.service';

@Component({
  selector: 'app-public-layout',
  standalone: false,
  templateUrl: './public-layout.component.html',
  styleUrl: './public-layout.component.scss'
})
export class PublicLayoutComponent implements OnInit {
  readonly auth = inject(AuthFacadeService);
  private readonly store = inject(StoreApiService);
  private readonly notifications = inject(NotificationsApiService);
  private readonly router = inject(Router);

  readonly currentYear = new Date().getFullYear();
  readonly searchControl = new FormControl('', { nonNullable: true, validators: [Validators.maxLength(100)] });
  searchResults: Product[] = [];
  cartCount = 0;
  readonly unreadCount$ = this.notifications.unreadCount$;

  ngOnInit(): void {
    if (this.auth.isAuthenticated()) {
      this.store.getCart().subscribe({ next: cart => this.cartCount = cart.itemCount, error: () => this.cartCount = 0 });
      this.notifications.getAll().subscribe({ error: () => this.notifications.setUnreadCount(0) });
    }
  }

  isCustomerLoggedIn(): boolean {
    return this.auth.isAuthenticated() && this.auth.isCustomer();
  }

  onSearch(): void {
    const query = this.searchControl.value.trim();
    if (!query) {
      this.searchResults = [];
      return;
    }
    this.store.getProducts({ search: query, page: 1, pageSize: 8 }).subscribe({
      next: page => this.searchResults = page.items,
      error: () => this.searchResults = []
    });
  }

  searchDescription(product: Product): { before: string; matchedText: string; after: string } {
    const query = this.searchControl.value.trim();
    const description = product.description ?? '';
    const index = query ? description.toLocaleLowerCase().indexOf(query.toLocaleLowerCase()) : -1;
    if (index >= 0) {
      const start = Math.max(0, index - 24);
      const end = Math.min(description.length, index + query.length + 32);
      return {
        before: `${start > 0 ? '…' : ''}${description.slice(start, index)}`,
        matchedText: description.slice(index, index + query.length),
        after: `${description.slice(index + query.length, end).trimEnd()}${end < description.length ? '…' : ''}`
      };
    }
    return { before: description ? description.length > 100 ? description.slice(0, 100).trimEnd() + '…' : description : 'Opis nije dostupan', matchedText: '', after: '' };
  }

  goToProduct(productId: number): void {
    void this.router.navigate(['/product', productId]);
    this.searchControl.setValue('');
    this.searchResults = [];
  }
}
