using Market.Application.Modules.Auth.Commands.Login;
using Market.Application.Modules.Auth.Commands.Register;

public sealed class RegisterCommandHandler(
    IAppDbContext ctx,
    IJwtTokenService jwt,
    IPasswordHasher<MarketUserEntity> hasher)
    : IRequestHandler<RegisterCommand, LoginCommandDto>
{
    public async Task<LoginCommandDto> Handle(RegisterCommand request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await ctx.Users.AnyAsync(x => x.Email.ToLower() == email, ct))
            throw new MarketConflictException("Korisnik sa ovom email adresom već postoji.");

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
        await ctx.SaveChangesAsync(ct);

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
}
