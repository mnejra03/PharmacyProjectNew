import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { AuthFacadeService } from './auth-facade.service';
import { CurrentUserService } from './current-user.service';

describe('CurrentUserService', () => {
  function setup(user: { userId:number; email:string; isAdmin:boolean; isPharmacist:boolean; isCustomer:boolean; tokenVersion:number } | null) {
    const auth = {
      currentUser: signal(user), isAuthenticated: signal(!!user), isAdmin: signal(user?.isAdmin ?? false),
      isPharmacist: signal(user?.isPharmacist ?? false), isCustomer: signal(user?.isCustomer ?? false)
    };
    TestBed.configureTestingModule({ providers: [{ provide: AuthFacadeService, useValue: auth }] });
    return TestBed.inject(CurrentUserService);
  }

  it('routes administrators to the admin area', () => {
    const service = setup({ userId:1, email:'admin@test.local', isAdmin:true, isPharmacist:false, isCustomer:false, tokenVersion:0 });
    expect(service.getDefaultRoute()).toBe('/admin');
  });

  it('routes signed-in customers and pharmacists to the client area', () => {
    const service = setup({ userId:2, email:'person@test.local', isAdmin:false, isPharmacist:true, isCustomer:false, tokenVersion:0 });
    expect(service.getDefaultRoute()).toBe('/client');
  });

  it('routes missing users to login', () => {
    const service = setup(null);
    expect(service.getDefaultRoute()).toBe('/login');
  });
});
