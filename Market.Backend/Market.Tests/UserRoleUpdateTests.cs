using Market.Application.Abstractions;
using Market.Application.Modules.Accounts;
using Market.Domain.Entities.Identity;
using Market.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Market.Tests;

public sealed class UserRoleUpdateTests
{
    [Fact]
    public async Task Updating_personal_details_preserves_all_existing_roles()
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new DatabaseContext(options, TimeProvider.System);
        var user = new MarketUserEntity
        {
            Id = 41,
            Email = "multi-role@example.com",
            FirstName = "Original",
            LastName = "Name",
            IsAdmin = true,
            IsPharmacist = true,
            IsCustomer = true,
            IsEnabled = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var handler = new UpdateUserHandler(db, new AdminCurrentUser());
        var command = new UpdateUserCommand(
            user.Id,
            FirstName: "Updated",
            LastName: "Name",
            PhoneNumber: "061123456",
            IsAdmin: true,
            IsPharmacist: true,
            IsCustomer: true);

        Assert.True(new UpdateUserValidator().Validate(command).IsValid);
        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal("Updated", result.FirstName);
        Assert.Equal("061123456", result.PhoneNumber);
        Assert.True(result.IsAdmin);
        Assert.True(result.IsPharmacist);
        Assert.True(result.IsCustomer);
        Assert.Equal(0, user.TokenVersion);
    }

    private sealed class AdminCurrentUser : IAppCurrentUser
    {
        public int? UserId => 99;
        public string? Email => "admin@example.com";
        public bool IsAuthenticated => true;
        public bool IsAdmin => true;
        public bool IsPharmacist => false;
        public bool IsCustomer => false;
    }
}
