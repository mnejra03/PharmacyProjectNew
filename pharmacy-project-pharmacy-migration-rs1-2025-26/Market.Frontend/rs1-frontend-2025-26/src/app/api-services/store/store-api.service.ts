import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';

export interface Product { id:number; name:string; description:string; price:number; currentPrice:number; quantityInStock:number; imageUrl:string; categoryId:number; categoryName:string; brandId?:number; brandName?:string; isDiscounted:boolean; discountPercentage?:number; expiryDate?:string; averageRating:number; reviewCount:number; }
export interface ProductPage { items:Product[]; pageSize:number; currentPage:number; totalItems:number; totalPages:number; includedTotal:boolean; }
export interface ProductCategory { id:number; name:string; }
export interface ProductBrand { id:number; name:string; logoUrl?:string; description:string; }
export interface CartItem { id:number; productId:number; name:string; imageUrl:string; quantity:number; unitPrice:number; lineTotal:number; savedForLater:boolean; stock:number; }
export interface Cart { items:CartItem[]; total:number; itemCount:number; }
export interface OrderItem { productId:number; name:string; quantity:number; unitPrice:number; }
export interface PharmacyOrder { id:number; orderedAtUtc:string; status:string; totalPrice:number; paymentMethod:string; shippingAddress:string; customerName:string; customerEmail:string; items:OrderItem[]; }
export interface PharmacySupplyOrder { id:number; orderedAtUtc:string; status:string; totalPrice:number; pharmacistName:string; pharmacistEmail:string; items:OrderItem[]; }
export interface CartPaymentIntent { paymentIntentId:string; clientSecret:string; publishableKey:string; amount:number; currency:string; }
export interface ProductReview { id:number; userName:string; rating:number; text:string; createdAtUtc:string; }
export interface SaveProduct { name:string; description:string; price:number; quantityInStock:number; imageUrl:string; categoryId:number; brandId?:number; isDiscounted:boolean; discountPercentage?:number; expiryDate?:string; }

@Injectable({ providedIn:'root' })
export class StoreApiService {
  private http=inject(HttpClient);
  private base=environment.apiUrl;
  getProducts(filter:{search?:string;categoryId?:number;brandId?:number;discounted?:boolean;page?:number;pageSize?:number}={}) {
    let params=new HttpParams();
    for(const [key,value] of Object.entries(filter)) if(value!==undefined && value!==null && value!=='') params=params.set(key,String(value));
    return this.http.get<ProductPage>(`${this.base}/api/products`,{params});
  }
  getProduct(id:number){return this.http.get<Product>(`${this.base}/api/products/${id}`);}
  getCategories(){return this.http.get<ProductCategory[]>(`${this.base}/api/categories`);}
  getBrands(){return this.http.get<ProductBrand[]>(`${this.base}/api/brands`);}
  createProduct(body:SaveProduct){return this.http.post<Product>(`${this.base}/api/products`,body);}
  updateProduct(id:number,body:SaveProduct){return this.http.put<Product>(`${this.base}/api/products/${id}`,body);}
  uploadProductImage(file:File){const form=new FormData();form.append('file',file);return this.http.post<{imageUrl:string}>(`${this.base}/api/products/image`,form);}
  deleteProduct(id:number){return this.http.delete<void>(`${this.base}/api/products/${id}`);}
  restockProduct(id:number,quantity:number){return this.http.post<number>(`${this.base}/api/products/${id}/restock`,{quantity});}
  getCart(){return this.http.get<Cart>(`${this.base}/api/cart`);}
  addToCart(productId:number,quantity=1){return this.http.post<Cart>(`${this.base}/api/cart/items`,{productId,quantity});}
  updateCartItem(id:number,quantity:number,savedForLater:boolean){return this.http.put<Cart>(`${this.base}/api/cart/items/${id}`,{id,quantity,savedForLater});}
  removeCartItem(id:number){return this.http.delete<void>(`${this.base}/api/cart/items/${id}`);}
  createPaymentIntent(){return this.http.post<CartPaymentIntent>(`${this.base}/api/payments/intent`,{});}
  checkout(shippingAddress:string,paymentMethod:string,paymentReference?:string){return this.http.post<PharmacyOrder>(`${this.base}/api/cart/checkout`,{shippingAddress,paymentMethod,paymentReference});}
  getOrders(all=false){return this.http.get<PharmacyOrder[]>(`${this.base}/api/orders`,{params:{all:String(all)}});}
  updateOrderStatus(id:number,status:string){return this.http.put<void>(`${this.base}/api/orders/${id}/status`,{id,status});}
  getSupplyOrders(all=false){return this.http.get<PharmacySupplyOrder[]>(`${this.base}/api/supply-orders`,{params:{all:String(all)}});}
  createSupplyOrder(productId:number,quantity:number){return this.http.post<number>(`${this.base}/api/supply-orders`,{productId,quantity});}
  updateSupplyOrderStatus(id:number,status:'Approved'|'Rejected'){return this.http.put<void>(`${this.base}/api/supply-orders/${id}/status`,{id,status});}
  getWishlist(){return this.http.get<Product[]>(`${this.base}/api/wishlist`);}
  addToWishlist(productId:number){return this.http.post<void>(`${this.base}/api/wishlist/${productId}`,{});}
  removeFromWishlist(productId:number){return this.http.delete<void>(`${this.base}/api/wishlist/${productId}`);}
  getReviews(productId:number){return this.http.get<ProductReview[]>(`${this.base}/api/products/${productId}/reviews`);}
  addReview(productId:number,rating:number,text:string){return this.http.post<ProductReview>(`${this.base}/api/products/${productId}/reviews`,{rating,text});}
}
