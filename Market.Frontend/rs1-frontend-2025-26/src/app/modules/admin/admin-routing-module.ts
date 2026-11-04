import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AdminLayoutComponent } from './admin-layout/admin-layout.component';
import { AdminSettingsComponent } from './admin-settings/admin-settings.component';
import { UsersComponent } from './users/users.component';
import { DashboardComponent } from './dashboard/dashboard.component';
import { AdvertisementsComponent } from './advertisements/advertisements.component';
import { AdminProductsComponent } from './products/products.component';
import { AdminOrdersComponent } from './orders/admin-orders.component';

const routes: Routes = [{
  path: '', component: AdminLayoutComponent, children: [
    { path: 'settings', component: AdminSettingsComponent },
    { path: 'overview', component: DashboardComponent },
    { path: 'advertisements', component: AdvertisementsComponent },
    { path: 'users', component: UsersComponent },
    { path: 'products', component: AdminProductsComponent },
    { path: 'orders', component: AdminOrdersComponent },
    { path: '', redirectTo: 'overview', pathMatch: 'full' }
  ]
}];

@NgModule({ imports: [RouterModule.forChild(routes)], exports: [RouterModule] })
export class AdminRoutingModule {}
