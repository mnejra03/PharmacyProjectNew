using System.Reflection;
using System.Text.Json;
using Market.Domain.Entities.Communication;
using Market.Domain.Entities.Content;

namespace Market.Infrastructure.Database.Seeders;

public static class DynamicDataSeeder
{
    public static async Task SeedAsync(DatabaseContext context)
    {
        var hasher = new PasswordHasher<MarketUserEntity>();
        var admin = await context.Users.FirstOrDefaultAsync(u => u.Email == "admin@pharmacy.local");
        if (admin is null)
        {
            admin = new MarketUserEntity { Email = "admin@pharmacy.local", PasswordHash = hasher.HashPassword(null!, "Admin123!"), IsAdmin = true, IsCustomer = false, IsEnabled = true };
            context.Users.Add(admin);
        }
        if ((admin.FirstName == "System" && admin.LastName == "Administrator") || string.IsNullOrWhiteSpace(admin.FirstName))
        {
            admin.FirstName = "Amra";
            admin.LastName = "Hadžić";
        }
        const string pharmacistAvatar = "https://pharmacyprojectimg2026.blob.core.windows.net/profile-images/bbffdfd7-9ad6-474c-922d-0b88701966a0.jpg";
        var pharmacist = await context.Users.FirstOrDefaultAsync(u => u.Email == "pharmacist@pharmacy.local");
        if (pharmacist is null)
        {
            pharmacist = new MarketUserEntity { Email = "pharmacist@pharmacy.local", PasswordHash = hasher.HashPassword(null!, "Pharmacist123!"), IsPharmacist = true, IsCustomer = false, IsEnabled = true, ProfileImageUrl = pharmacistAvatar };
            context.Users.Add(pharmacist);
        }
        if ((pharmacist.FirstName == "Demo" && pharmacist.LastName == "Pharmacist") || string.IsNullOrWhiteSpace(pharmacist.FirstName))
        {
            pharmacist.FirstName = "Emina";
            pharmacist.LastName = "Kovačević";
        }
        if (string.IsNullOrWhiteSpace(pharmacist.ProfileImageUrl)
            || pharmacist.ProfileImageUrl.StartsWith("https://rs1pharmacyimages.blob.core.windows.net/", StringComparison.OrdinalIgnoreCase))
            pharmacist.ProfileImageUrl = pharmacistAvatar;
        var customer = await context.Users.FirstOrDefaultAsync(u => u.Email == "customer@pharmacy.local");
        if (customer is null)
        {
            customer = new MarketUserEntity { Email = "customer@pharmacy.local", PasswordHash = hasher.HashPassword(null!, "Customer123!"), IsCustomer = true, IsEnabled = true };
            context.Users.Add(customer);
        }
        if ((customer.FirstName == "Demo" && customer.LastName == "Customer") || string.IsNullOrWhiteSpace(customer.FirstName))
        {
            customer.FirstName = "Lejla";
            customer.LastName = "Delić";
        }
        await context.SaveChangesAsync();

        await SeedCatalogAsync(context);

        var advertisements = new[]
        {
            ("B.Well toothbrush", "https://pharmacyprojectimg2026.blob.core.windows.net/advertisement-images/00c78858-8991-429e-a22b-ecc3d68562c6.jpg"),
            ("Nature's Aid Osteo Advance", "https://pharmacyprojectimg2026.blob.core.windows.net/advertisement-images/11cd32d8-3b83-4af2-a4f0-3f566981069c.jpg"),
            ("Nature's Aid Osteo Advance", "https://pharmacyprojectimg2026.blob.core.windows.net/advertisement-images/1e1b1d67-e8a7-4e5e-b082-a6f29ac848f0.jpg"),
            ("Super dermokozmetika", "https://pharmacyprojectimg2026.blob.core.windows.net/advertisement-images/27c1ec92-770c-4e44-86fe-3624fa36d088.jpg"),
            ("Eucerin Anti-Pigment", "https://pharmacyprojectimg2026.blob.core.windows.net/advertisement-images/3d126650-35ab-49ed-93bc-ba0e9b350702.jpg"),
            ("Saga skincare", "https://pharmacyprojectimg2026.blob.core.windows.net/advertisement-images/f3ccd92d-fd42-4a67-832f-036785f7ec3e.jpg"),
            ("Eucerin Sun", "https://pharmacyprojectimg2026.blob.core.windows.net/advertisement-images/f5d9bd6f-10ca-43d9-9734-9cfbb7fd5129.jpg")
        };

        // Remove only obsolete seeded ads that point to the retired storage account.
        var retiredAds = await context.Advertisements
            .Where(a => a.ImageUrl.StartsWith("https://rs1pharmacyimages.blob.core.windows.net/"))
            .ToListAsync();
        context.Advertisements.RemoveRange(retiredAds);

        foreach (var (title, imageUrl) in advertisements)
        {
            var existingAd = await context.Advertisements.FirstOrDefaultAsync(a => a.ImageUrl == imageUrl);
            if (existingAd is null)
                context.Advertisements.Add(new AdvertisementEntity { Title = title, ImageUrl = imageUrl });
            else
                existingAd.Title = title;
        }
        await context.SaveChangesAsync();
        await SeedDemoOrdersAsync(context);
        await SeedDemoActivityAsync(context);
    }

    private static async Task SeedDemoActivityAsync(DatabaseContext context)
    {
        var customer = await context.Users.FirstOrDefaultAsync(u => u.Email == "customer@pharmacy.local");
        var pharmacist = await context.Users.FirstOrDefaultAsync(u => u.Email == "pharmacist@pharmacy.local");
        var products = await context.Products.OrderBy(p => p.Id).Take(4).ToListAsync();
        if (customer is null || pharmacist is null || products.Count < 2) return;

        if (!await context.Carts.AnyAsync(c => c.UserId == customer.Id && !c.IsCompleted))
        {
            var cart = new CartEntity { UserId = customer.Id };
            cart.Items.Add(new CartItemEntity { ProductId = products[0].Id, Quantity = 1 });
            context.Carts.Add(cart);
        }

        if (!await context.Wishlists.AnyAsync(w => w.UserId == customer.Id))
        {
            var wishlist = new WishlistEntity { UserId = customer.Id };
            wishlist.Items.Add(new WishlistItemEntity { ProductId = products[1].Id });
            if (products.Count > 2) wishlist.Items.Add(new WishlistItemEntity { ProductId = products[2].Id });
            context.Wishlists.Add(wishlist);
        }

        foreach (var (product, rating, text) in new[]
        {
            (products[0], 5, "Odličan demo proizvod, brzo se uklopio u svakodnevnu rutinu."),
            (products[1], 4, "Primjer recenzije: kvalitetan proizvod i jednostavna narudžba.")
        })
        {
            if (!await context.ProductReviews.AnyAsync(r => r.ProductId == product.Id && r.UserId == customer.Id))
                context.ProductReviews.Add(new ProductReviewEntity { ProductId = product.Id, UserId = customer.Id, Rating = rating, Text = text });
        }

        if (!await context.Notifications.AnyAsync(n => n.UserId == customer.Id))
            context.Notifications.AddRange(
                new NotificationEntity { UserId = customer.Id, Title = "Dobro došli, Lejla!", Message = "Ovdje ćete vidjeti obavijesti o receptima, narudžbama i novostima apoteke.", Type = "info", CreatedAt = DateTime.UtcNow.AddMinutes(-20), IsRead = false },
                new NotificationEntity { UserId = customer.Id, Title = "Narudžba je u pripremi", Message = "Primjer obavijesti o statusu narudžbe. Status možete pratiti na stranici Narudžbe.", Type = "order", CreatedAt = DateTime.UtcNow.AddDays(-1), IsRead = true });
        if (!await context.Notifications.AnyAsync(n => n.UserId == pharmacist.Id))
            context.Notifications.Add(new NotificationEntity { UserId = pharmacist.Id, Title = "Novi recept za pregled", Message = "Lejla Delić poslala je demo recept. Otvorite stranicu Recepti za pregled.", Type = "recipe", CreatedAt = DateTime.UtcNow.AddMinutes(-10), IsRead = false });
        var admin = await context.Users.FirstOrDefaultAsync(u => u.Email == "admin@pharmacy.local");
        if (admin is not null && !await context.Notifications.AnyAsync(n => n.UserId == admin.Id))
            context.Notifications.Add(new NotificationEntity { UserId = admin.Id, Title = "Dobro došli, Amra!", Message = "Demo podaci su spremni: katalog, korisnici, narudžbe i aktivnosti apoteke.", Type = "info", CreatedAt = DateTime.UtcNow.AddMinutes(-15), IsRead = false });

        if (!await context.Recipes.AnyAsync(r => r.UserId == customer.Id))
            context.Recipes.Add(new RecipeEntity { UserId = customer.Id, DateOfIssue = DateTime.UtcNow.AddDays(-1), DoctorFirstName = "Demo", DoctorLastName = "Primjer", Status = "Pending" });

        if (!await context.ChatMessages.AnyAsync())
        {
            context.ChatMessages.Add(new ChatMessageEntity { SenderId = customer.Id, ReceiverId = pharmacist.Id, Message = "Pozdrav, možete li mi pomoći pronaći odgovarajući proizvod? (demo razgovor)", Type = "question", Status = "new", SentAtUtc = DateTime.UtcNow.AddMinutes(-5) });
            context.ChatMessages.Add(new ChatMessageEntity { SenderId = pharmacist.Id, ReceiverId = customer.Id, Message = "Naravno, rado ćemo pomoći. Ova poruka služi kao primjer razgovora.", Type = "answer", Status = "answered", IsResponse = true, SentAtUtc = DateTime.UtcNow.AddMinutes(-3) });
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedDemoOrdersAsync(DatabaseContext context)
    {
        // Seed each demo order type independently and idempotently so existing
        // customer orders do not prevent the example supply request from appearing.
        var seedCustomerOrders = !await context.Orders.AnyAsync(o => !o.IsSupplyOrder);
        var seedSupplyOrder = !await context.Orders.AnyAsync(o => o.IsSupplyOrder);

        var admin = await context.Users.FirstOrDefaultAsync(u => u.Email == "admin@pharmacy.local");
        var customer = await context.Users.FirstOrDefaultAsync(u => u.Email == "customer@pharmacy.local");
        var pharmacist = await context.Users.FirstOrDefaultAsync(u => u.Email == "pharmacist@pharmacy.local");
        var products = await context.Products.OrderBy(p => p.Id).Take(4).ToListAsync();
        if (products.Count < 3) return;

        var now = DateTime.UtcNow;
        if (seedCustomerOrders && admin is not null && customer is not null)
        {
        var demoOrders = new[]
        {
            (User: customer, Status: "Pending", Payment: "Kartica (demo)", Address: "Sarajevo, BiH", DaysAgo: 5, Lines: new[] { (Product: products[0], Quantity: 2), (Product: products[1], Quantity: 1) }),
            (User: customer, Status: "Shipped", Payment: "Pouzećem", Address: "Mostar, BiH", DaysAgo: 2, Lines: new[] { (Product: products[2], Quantity: 3) }),
            (User: admin, Status: "Completed", Payment: "Kartica (demo)", Address: "Zenica, BiH", DaysAgo: 1, Lines: new[] { (Product: products[3], Quantity: 2) }),
            (User: admin, Status: "Completed", Payment: "Kartica (demo)", Address: "Sarajevo, BiH", DaysAgo: 0, Lines: new[] { (Product: products[2], Quantity: 1), (Product: products[1], Quantity: 2) })
        };

        foreach (var seed in demoOrders)
        {
            var items = seed.Lines.Select(line => new OrderItemEntity
            {
                ProductId = line.Product.Id,
                ProductName = line.Product.Name,
                Quantity = line.Quantity,
                UnitPrice = line.Product.Price
            }).ToList();
            context.Orders.Add(new OrderEntity
            {
                UserId = seed.User.Id,
                OrderedAtUtc = now.AddDays(-seed.DaysAgo),
                Status = seed.Status,
                TotalPrice = items.Sum(item => item.UnitPrice * item.Quantity),
                PaymentMethod = seed.Payment,
                ShippingAddress = seed.Address,
                Items = items
            });
        }
        }

        if (seedSupplyOrder && pharmacist is not null)
        {
            var product = products[0];
            const int quantity = 8;
            context.Orders.Add(new OrderEntity
            {
                UserId = pharmacist.Id,
                OrderedAtUtc = now.AddHours(-3),
                Status = "Pending",
                TotalPrice = product.Price * quantity,
                PaymentMethod = "Internal Transfer",
                ShippingAddress = "Apoteka Sarajevo",
                IsSupplyOrder = true,
                Items =
                {
                    new OrderItemEntity
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        Quantity = quantity,
                        UnitPrice = product.Price
                    }
                }
            });
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedCatalogAsync(DatabaseContext context)
    {
        await using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Market.Infrastructure.Database.Seeders.legacy-products.json")
            ?? throw new InvalidOperationException("Katalog proizvoda nije pronađen u resursima aplikacije.");
        var catalog = await JsonSerializer.DeserializeAsync<LegacyCatalog>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("Katalog proizvoda nije ispravan.");

        foreach (var name in catalog.Categories)
            if (!await context.ProductCategories.AnyAsync(c => c.Name == name)) context.ProductCategories.Add(new ProductCategoryEntity { Name = name });
        var brandLogos = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Flobian"] = "476930dc-4e39-4714-8137-39a0f4d5c631.png",
            ["Yasenka"] = "5f62773b-8e4b-43d9-8045-24cb5a5b6a4e.png",
            ["Cydonia"] = "009e9e60-0fd0-4fb9-acc7-8cfa56901dde.png",
            ["Medisana"] = "4e3b886d-8699-450a-b025-edfd2929ebf2.png",
            ["Humana"] = "99361ca6-386f-43c1-b8c3-2ec48759d059.png",
            ["Beurer"] = "2ddf2450-47fd-4a68-a246-50485363533c.png",
            ["CeraVe"] = "be212daa-a170-4ec6-b871-3e72dbde6908.png",
            ["Avene"] = "a77160d3-c5d2-46f9-bca4-654454b09b44.png",
            ["Clinique"] = "17106211-f942-4742-a21a-b571eb2b7a06.png",
            ["A-derma"] = "0285fd4d-678d-458c-856b-63e3cc5cd68b.png",
            ["Biomd"] = "beac29e1-827d-4742-85dd-3a23b0bfa1cf.png",
            ["Herbiko"] = "00ac463d-1fb4-4750-8aeb-0c3b372eba7d.png",
            ["Natures finest"] = "19e2a10e-8a85-4bf9-9bea-c29799ae7e2c.jpg",
            ["Advancis"] = "6192c15d-af04-4a6a-ab39-73f8ed141b62.jpg",
            ["Melem"] = "d17f97aa-6eb2-4824-b4ec-e5404e0fde1c.png",
            ["Gloria"] = "fc55b2b4-2117-46e9-b047-e25716e62344.png",
            ["Vichy"] = "993badd6-b66b-4794-a02f-b3f2005ff6af.png",
            ["Ecodenta"] = "82da9c23-68f4-4bf1-be76-a9a3dd5532b9.png",
            ["MAM"] = "199b0b73-d2a4-435e-b827-ae1d2ab172d8.png",
            ["Trudy"] = "21124373-a24a-4748-aadc-90cba1794eb6.png",
            ["Mapez"] = "6dafbd80-3e96-4074-a216-59cc2ea5f701.png",
            ["Hansaplast"] = "eff902ca-346e-4a18-8468-733b75c42bbb.png",
            ["BABE"] = "c5f479c2-8859-4b61-be30-d1cf2085e7f3.png",
            ["Uriage"] = "f67e87c2-3873-45e5-a97b-5f9d80ce305f.png",
            ["Eliksir"] = "bd81774c-0ad4-4d3b-86a9-5bfd2720f4ab.jpg"
        };

        foreach (var name in catalog.Brands)
        {
            var logoUrl = brandLogos.TryGetValue(name, out var logo)
                ? $"https://pharmacyprojectimg2026.blob.core.windows.net/brand-images/{logo}"
                : null;
            var brand = await context.Brands.FirstOrDefaultAsync(b => b.Name == name);
            if (brand is null)
                context.Brands.Add(new BrandEntity { Name = name, Description = $"{name} proizvodi", LogoUrl = logoUrl });
            else if (!string.IsNullOrWhiteSpace(logoUrl)
                && (string.IsNullOrWhiteSpace(brand.LogoUrl)
                    || brand.LogoUrl.StartsWith("https://rs1pharmacyimages.blob.core.windows.net/", StringComparison.OrdinalIgnoreCase)))
                brand.LogoUrl = logoUrl;
        }
        await context.SaveChangesAsync();

        var categories = await context.ProductCategories.ToDictionaryAsync(c => c.Name, c => c.Id);
        var brands = await context.Brands.ToDictionaryAsync(b => b.Name, b => b.Id);
        var existingProducts = await context.Products.ToDictionaryAsync(p => p.Name);
        foreach (var item in catalog.Products)
        {
            if (existingProducts.TryGetValue(item.Name, out var existing))
            {
                // Replace image links from the retired blob account while preserving
                // any image URL that an administrator has since uploaded or changed.
                if (existing.ImageUrl.StartsWith("https://rs1pharmacyimages.blob.core.windows.net/", StringComparison.OrdinalIgnoreCase)
                    || existing.ImageUrl.StartsWith("/images/products/", StringComparison.OrdinalIgnoreCase))
                    existing.ImageUrl = item.ImageUrl;
                continue;
            }
            var categoryName = catalog.Categories[item.CategoryIndex];
            var brandId = item.BrandIndex is int index && index >= 0 && index < catalog.Brands.Count ? brands[catalog.Brands[index]] : (int?)null;
            context.Products.Add(new ProductEntity
            {
                Name = item.Name, Description = item.Description, Price = item.Price, QuantityInStock = item.Quantity,
                ImageUrl = item.ImageUrl, CategoryId = categories[categoryName], BrandId = brandId,
                IsDiscounted = item.Discounted, DiscountPercentage = item.Discounted ? item.DiscountPercentage : null,
                ExpiryDate = item.ExpiryDate, AddedAtUtc = DateTime.UtcNow
            });
        }
        await context.SaveChangesAsync();
    }

    private sealed class LegacyCatalog
    {
        public List<string> Categories { get; set; } = [];
        public List<string> Brands { get; set; } = [];
        public List<LegacyProduct> Products { get; set; } = [];
    }

    private sealed class LegacyProduct
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public int CategoryIndex { get; set; }
        public int? BrandIndex { get; set; }
        public bool Discounted { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }
}
