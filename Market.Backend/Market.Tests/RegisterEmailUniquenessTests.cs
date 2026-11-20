using Market.Application.Abstractions;
using Market.Application.Common.Exceptions;
using Market.Application.Modules.Accounts;
using Market.Application.Modules.Auth.Commands.Login;
using Market.Application.Modules.Auth.Commands.Register;
using Market.Domain.Entities.Identity;
using Market.Infrastructure.Database;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Market.Tests;

public sealed class RegisterEmailUniquenessTests
{
    [Fact]
    public async Task Register_creates_a_new_account_when_email_is_not_in_use()
    {
        await using var db = CreateDatabase();
        var handler = CreateHandler(db);

        var result = await handler.Handle(CreateCommand(" New.User@Example.com "), CancellationToken.None);

        var createdUser = await db.Users.IgnoreQueryFilters().SingleAsync();
        Assert.Equal("new.user@example.com", createdUser.Email);
        Assert.True(createdUser.IsEnabled);
        Assert.False(createdUser.IsDeleted);
        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        Assert.Single(await db.RefreshTokens.IgnoreQueryFilters().ToListAsync());
    }

    [Fact]
    public async Task Register_rejects_email_of_an_existing_active_account_with_business_message()
    {
        await using var db = CreateDatabase();
        db.Users.Add(CreateExistingUser("active@example.com"));
        await db.SaveChangesAsync();
        var handler = CreateHandler(db);

        var exception = await Assert.ThrowsAsync<MarketConflictException>(() =>
            handler.Handle(CreateCommand("ACTIVE@example.com"), CancellationToken.None));

        Assert.Equal("Korisnik sa ovom e-mail adresom već postoji.", exception.Message);
        Assert.Single(await db.Users.IgnoreQueryFilters().ToListAsync());
    }

    [Fact]
    public async Task Register_rejects_email_of_a_soft_deleted_account_with_business_message()
    {
        await using var db = CreateDatabase();
        var removedUser = CreateExistingUser("removed@example.com");
        var admin = CreateExistingUser("admin@example.com");
        admin.IsAdmin = true;
        db.Users.AddRange(admin, removedUser);
        await db.SaveChangesAsync();
        var deleteHandler = new DeleteUserHandler(db, new TestCurrentUser(admin.Id));
        await deleteHandler.Handle(new DeleteUserCommand(removedUser.Id), CancellationToken.None);
        var handler = CreateHandler(db);

        var exception = await Assert.ThrowsAsync<MarketConflictException>(() =>
            handler.Handle(CreateCommand("removed@example.com"), CancellationToken.None));

        Assert.Equal("Račun s ovom e-mail adresom je ranije uklonjen. Ponovna registracija s istom adresom nije moguća.", exception.Message);
        Assert.Equal(2, await db.Users.IgnoreQueryFilters().CountAsync());
    }

    private static DatabaseContext CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new DatabaseContext(options, TimeProvider.System);
    }

    private static RegisterCommandHandler CreateHandler(DatabaseContext db) =>
        new(db, new TestJwtTokenService(), new PasswordHasher<MarketUserEntity>());

    private static RegisterCommand CreateCommand(string email) => new()
    {
        Email = email,
        Password = "StrongPass123!",
        FirstName = "New",
        LastName = "Customer"
    };

    private static MarketUserEntity CreateExistingUser(string email) => new()
    {
        Email = email,
        PasswordHash = "already-hashed",
        FirstName = "Existing",
        LastName = "Customer",
        IsCustomer = true,
        IsEnabled = true
    };

    private sealed class TestJwtTokenService : IJwtTokenService
    {
        public JwtTokenPair IssueTokens(MarketUserEntity user) => new()
        {
            AccessToken = $"access-{user.Id}",
            AccessTokenExpiresAtUtc = DateTime.UtcNow.AddMinutes(15),
            RefreshTokenRaw = $"refresh-{user.Id}",
            RefreshTokenHash = $"hash-{user.Id}",
            RefreshTokenExpiresAtUtc = DateTime.UtcNow.AddDays(14)
        };

        public string HashRefreshToken(string rawToken) => $"hash-{rawToken}";
    }

    private sealed class TestCurrentUser(int userId) : IAppCurrentUser
    {
        public int? UserId => userId;
        public string? Email => "admin@example.com";
        public bool IsAuthenticated => true;
        public bool IsAdmin => true;
        public bool IsPharmacist => false;
        public bool IsCustomer => false;
    }
}
