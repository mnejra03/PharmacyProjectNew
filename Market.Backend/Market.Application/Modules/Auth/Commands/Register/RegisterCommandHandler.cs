using Market.Application.Modules.Auth.Commands.Login;
using Market.Application.Modules.Auth.Commands.Register;
using Microsoft.EntityFrameworkCore;

public sealed class RegisterCommandHandler(
    IAppDbContext ctx,
    IJwtTokenService jwt,
    IPasswordHasher<MarketUserEntity> hasher)
    : IRequestHandler<RegisterCommand, LoginCommandDto>
{
    public async Task<LoginCommandDto> Handle(RegisterCommand request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var existingUser = await FindByEmailAsync(email, ct);
        if (existingUser is not null)
            throw CreateEmailConflict(existingUser);

        var user = new MarketUserEntity
        {
            Email = email,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            IsCustomer = true,
            IsEnabled = true
        };
        user.PasswordHash = hasher.HashPassword(user, request.Password);
        ctx.Users.Add(user);
        try
        {
            await ctx.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // The unique index remains the final guard if two registrations race.
            existingUser = await FindByEmailAsync(email, ct);
            if (existingUser is not null)
                throw CreateEmailConflict(existingUser);
            throw;
        }

        var tokens = jwt.IssueTokens(user);
        ctx.RefreshTokens.Add(new RefreshTokenEntity
        {
            TokenHash = tokens.RefreshTokenHash,
            ExpiresAtUtc = tokens.RefreshTokenExpiresAtUtc,
            UserId = user.Id
        });
        await ctx.SaveChangesAsync(ct);

        return new LoginCommandDto
        {
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshTokenRaw,
            ExpiresAtUtc = tokens.AccessTokenExpiresAtUtc
        };
    }

    private Task<MarketUserEntity?> FindByEmailAsync(string email, CancellationToken ct) =>
        ctx.Users.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email.ToLower() == email, ct);

    private static MarketConflictException CreateEmailConflict(MarketUserEntity user) =>
        user.IsDeleted
            ? new MarketConflictException("Račun s ovom e-mail adresom je ranije uklonjen. Ponovna registracija s istom adresom nije moguća.")
            : new MarketConflictException("Korisnik sa ovom e-mail adresom već postoji.");
}
