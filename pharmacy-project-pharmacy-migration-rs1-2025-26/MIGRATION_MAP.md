# Pharmacy feature migration map

Source project: `../NewPharmacy` and `../frontend` (when available in the workspace). Target: `Market.Backend` and `Market.Frontend/rs1-frontend-2025-26`.

## Architecture requirements

- Backend is split into `Market.Domain`, `Market.Application`, `Market.Infrastructure`, `Market.API`, and `Market.Shared`.
- Backend use cases go through MediatR commands/queries and handlers. FluentValidation validators are discovered from the Application assembly and run through the validation pipeline.
- JWT authentication and authorization are configured in the API. Entity Framework migrations run on startup; Development seeds sample accounts and application data.
- Angular uses NgModules, lazy-loaded feature modules, non-standalone components, and Reactive Forms.

## Migrated features

| Feature | Target implementation | Status |
|---|---|---|
| Login, registration, logout, token refresh | Auth API, JWT, CQRS commands and validators; lazy-loaded auth module | Migrated |
| User profile, password change, profile image | Account commands and user API; image upload uses configured Azure Blob Storage or local fallback | Migrated |
| Admin user management and dashboard | Paged user query, filters, role updates, dashboard statistics | Migrated |
| Chat | Authenticated history/send API and SignalR notifications | Migrated |
| Notifications | User-scoped list, unread count, read and delete operations | Migrated |
| Recipes and prescription scans | Customer submission, pharmacist review/status, protected scan download | Migrated; scans are stored in SQL with a 10 MB limit |
| Advertisements | Public display and admin CRUD/image upload | Migrated; storage provider is configurable |
| Product catalog, categories, brands, product image upload | CQRS catalog API, admin screens, product details and reviews | Retained and migrated by the later project decision |
| Cart, save for later, checkout, Stripe, customer orders | CQRS commerce handlers, payment intent, order history and status updates | Retained and migrated by the later project decision |
| Favorites and product reviews | Wishlist and review APIs and screens | Retained and migrated with the catalog |
| Direct stock intake | Authorized product restock command and admin product screen | Migrated |
| Pharmacist supply requests | Pharmacist can request product quantities; admin can approve/reject; approved quantities update stock and notify the pharmacist | Migrated in `AddSupplyOrders` |

## Deliberate decision about catalog and orders

The original migration note requested removal of `Product`, `ProductCategory`, and `Order`. A later explicit instruction requested that the restored catalog and commerce remain. This project follows the later instruction, so these entities, tables, API routes, and screens remain. If the professor expects the original removal rule to be followed literally, that requirement conflicts with the current project direction and needs a decision before removing these features.

## Features not migrated / scope limits

- Password reset is implemented with one-time, hashed, 30-minute tokens. SMTP delivery is configurable; in local Development without SMTP, the reset URL is written to the API log. Account activation/invite email or SMS flows are not present in the old project and remain outside the migration.
- Drag-and-drop is implemented for product and advertisement image uploads in the admin UI. Other drag-and-drop interactions were not part of the old project and are not included.
- Supplier records existed as seeded data in the source, but its supply-order endpoint did not select a supplier. The migrated supply request therefore tracks product, quantity, pharmacist, status, and stock update, not supplier purchasing/contact management.
- Recipe scan files are stored in the database. Product, advertisement, and profile images use the configured file-storage provider; without Azure configuration, the backend falls back to local files.

## Verification

- Frontend: `npm ci` and `npm start` from `Market.Frontend/rs1-frontend-2025-26`; `npm run build` verifies compilation.
- Backend: `dotnet build Market.Backend.sln` from `Market.Backend`; startup requires an available SQL Server and applies migrations automatically.
- Run `dotnet test Market.Backend.sln` and `npm test` when exercising the test suites; automated UI and integration coverage should be extended for new business flows.
