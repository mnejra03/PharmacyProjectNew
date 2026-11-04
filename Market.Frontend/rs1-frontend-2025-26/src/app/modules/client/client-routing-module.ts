import { NgModule } from '@angular/core'; import { RouterModule, Routes } from '@angular/router';
import { NotificationsComponent } from './notifications/notifications.component'; import { RecipesComponent } from './recipes/recipes.component'; import { PharmacistRecipesComponent } from './recipes/pharmacist-recipes.component'; import { ChatComponent } from './chat/chat.component'; import { ProfileComponent } from './profile/profile.component';
import { ClientLayoutComponent } from './client-layout/client-layout.component';
import { CartComponent } from './cart/cart.component'; import { WishlistComponent } from './wishlist/wishlist.component'; import { OrdersComponent } from './orders/orders.component';
import { SupplyOrdersComponent } from './orders/supply-orders.component';
const routes: Routes = [{ path:'', component:ClientLayoutComponent, children:[
  { path: 'notifications', component: NotificationsComponent }, { path: 'recipes', component: RecipesComponent },
  { path: 'cart', component: CartComponent }, { path: 'wishlist', component: WishlistComponent }, { path: 'orders', component: OrdersComponent },
  { path: 'supply-orders', component: SupplyOrdersComponent },
  { path: 'pharmacist/recipes', component: PharmacistRecipesComponent },
  { path: 'chat', component: ChatComponent }, { path: 'profile', component: ProfileComponent },
  { path: '', redirectTo: 'profile', pathMatch: 'full' }
]}];
@NgModule({ imports:[RouterModule.forChild(routes)], exports:[RouterModule] }) export class ClientRoutingModule {}
