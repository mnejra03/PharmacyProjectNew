import { Component, OnInit, inject } from '@angular/core';
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
  searchQuery = '';
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
    const query = this.searchQuery.trim();
    if (!query) {
      this.searchResults = [];
      return;
    }
    this.store.getProducts({ search: query, page: 1, pageSize: 8 }).subscribe({
      next: page => this.searchResults = page.items,
      error: () => this.searchResults = []
    });
  }

  goToProduct(productId: number): void {
    void this.router.navigate(['/product', productId]);
    this.searchQuery = '';
    this.searchResults = [];
  }
}
