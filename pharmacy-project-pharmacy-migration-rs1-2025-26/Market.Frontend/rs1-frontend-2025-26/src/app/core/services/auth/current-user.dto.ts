export interface CurrentUserDto {
  userId: number;
  email: string;
  isAdmin: boolean;
  isPharmacist: boolean;
  isCustomer: boolean;
  tokenVersion: number;
}
