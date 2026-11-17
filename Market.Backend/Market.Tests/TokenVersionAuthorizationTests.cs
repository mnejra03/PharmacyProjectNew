using Market.Application.Abstractions;
using Market.Application.Modules.Accounts;
using Market.Application.Modules.Auth.Commands.Login;
using Market.Domain.Entities.Identity;
using Market.Infrastructure.Common;
using Market.Infrastructure.Database;
using Market.Shared.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;

namespace Market.Tests;

public sealed class TokenVersionAuthorizationTests
{
    [Fact]
    public async Task Account_validator_rejects_stale_tokens_and_inactive_or_deleted_accounts()
    {
        await using var db = CreateDatabase();
        var user = new MarketUserEntity { Id = 17, Email = "user@example.com", IsEnabled = true, TokenVersion = 3 };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var validator = new AccessTokenAccountValidator(db);

        Assert.True(await validator.IsCurrentAsync(user.Id, 3, CancellationToken.None));

        user.TokenVersion++;
        await db.SaveChangesAsync();
        Assert.False(await validator.IsCurrentAsync(user.Id, 3, CancellationToken.None));
        Assert.True(await validator.IsCurrentAsync(user.Id, 4, CancellationToken.None));

        user.IsEnabled = false;
        await db.SaveChangesAsync();
        Assert.False(await validator.IsCurrentAsync(user.Id, 4, CancellationToken.None));

        user.IsEnabled = true;
        user.IsDeleted = true;
        await db.SaveChangesAsync();
        Assert.False(await validator.IsCurrentAsync(user.Id, 4, CancellationToken.None));
    }

    [Fact]
    public async Task Role_change_invalidates_old_token_and_new_login_contains_only_current_role()
    {
        await using var db = CreateDatabase();
        var hasher = new PasswordHasher<MarketUserEntity>();
        var jwt = CreateJwtService();
        var user = new MarketUserEntity
        {
            Id = 23,
            Email = "pharmacist@example.com",
            FirstName = "Test",
            LastName = "Pharmacist",
            IsEnabled = true,
            IsAdmin = true,
            IsPharmacist = false,
            IsCustomer = false
        };
        user.PasswordHash = hasher.HashPassword(user, "StrongPass123!");
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var oldToken = jwt.IssueTokens(user);

        var update = new UpdateUserHandler(db, new CurrentUser(99, isAdmin: true));
        await update.Handle(new UpdateUserCommand(
            user.Id, user.FirstName, user.LastName, null,
            IsAdmin: false, IsPharmacist: true, IsCustomer: false), CancellationToken.None);

        var accountValidator = new AccessTokenAccountValidator(db);
        Assert.False(await accountValidator.IsCurrentAsync(user.Id, 0, CancellationToken.None));

        var login = new LoginCommandHandler(db, jwt, hasher);
        var loginResult = await login.Handle(new LoginCommand
        {
            Email = user.Email,
            Password = "StrongPass123!"
        }, CancellationToken.None);
        var claims = new JwtSecurityTokenHandler().ReadJwtToken(loginResult.AccessToken).Claims
            .GroupBy(claim => claim.Type)
            .ToDictionary(group => group.Key, group => group.Last().Value);

        Assert.Equal("1", claims["ver"]);
        Assert.Equal("false", claims["is_admin"]);
        Assert.Equal("true", claims["is_pharmacist"]);
        Assert.Equal("false", claims["is_customer"]);
        Assert.True(await accountValidator.IsCurrentAsync(user.Id, 1, CancellationToken.None));
        Assert.NotEqual(oldToken.AccessToken, loginResult.AccessToken);
    }

    private static DatabaseContext CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new DatabaseContext(options, TimeProvider.System);
    }

    private static JwtTokenService CreateJwtService() => new(
        Options.Create(new JwtOptions
        {
            Issuer = "Market.Tests",
            Audience = "Market.Tests.Client",
            Key = "test-signing-key-must-have-at-least-32-bytes",
            AccessTokenMinutes = 20,
            RefreshTokenDays = 14
        }), TimeProvider.System);

    private sealed class CurrentUser(int userId, bool isAdmin) : IAppCurrentUser
    {
        public int? UserId => userId;
        public string? Email => "admin@example.com";
        public bool IsAuthenticated => true;
        public bool IsAdmin => isAdmin;
        public bool IsPharmacist => false;
        public bool IsCustomer => false;
    }
}
