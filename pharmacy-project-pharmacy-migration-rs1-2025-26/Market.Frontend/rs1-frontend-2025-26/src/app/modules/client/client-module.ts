import { NgModule } from '@angular/core'; import { SharedModule } from '../shared/shared-module'; import { ClientRoutingModule } from './client-routing-module';
import { NotificationsComponent } from './notifications/notifications.component'; import { RecipesComponent } from './recipes/recipes.component'; import { PharmacistRecipesComponent } from './recipes/pharmacist-recipes.component'; import { ChatComponent } from './chat/chat.component'; import { ProfileComponent } from './profile/profile.component';
import { ClientLayoutComponent } from './client-layout/client-layout.component';
import { CartComponent } from './cart/cart.component'; import { WishlistComponent } from './wishlist/wishlist.component'; import { OrdersComponent } from './orders/orders.component';
import { SupplyOrdersComponent } from './orders/supply-orders.component';
@NgModule({ declarations:[ClientLayoutComponent,NotificationsComponent,RecipesComponent,PharmacistRecipesComponent,ChatComponent,ProfileComponent,CartComponent,WishlistComponent,OrdersComponent,SupplyOrdersComponent], imports:[SharedModule,ClientRoutingModule] }) export class ClientModule {}
