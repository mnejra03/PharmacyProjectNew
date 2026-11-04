using System.Security.Cryptography;
using System.Text;

namespace Market.Application.Modules.Auth.Commands.PasswordReset;

public sealed record RequestPasswordResetCommand(string Email) : IRequest;
public sealed record ResetPasswordCommand(string Token, string NewPassword) : IRequest;

public sealed class RequestPasswordResetValidator : AbstractValidator<RequestPasswordResetCommand>
{
    public RequestPasswordResetValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
    }
}

public sealed class ResetPasswordValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MinimumLength(32).MaximumLength(200);
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8).MaximumLength(100);
    }
}

public sealed class RequestPasswordResetHandler(IAppDbContext db, IPasswordResetEmailSender emailSender)
    : IRequestHandler<RequestPasswordResetCommand>
{
    public async Task Handle(RequestPasswordResetCommand request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(x => x.Email.ToLower() == email && x.IsEnabled, ct);
        if (user is null) return;

        var now = DateTime.UtcNow;
        var priorTokens = await db.PasswordResetTokens
            .Where(x => x.UserId == user.Id && x.UsedAtUtc == null)
            .ToListAsync(ct);
        foreach (var oldToken in priorTokens) oldToken.UsedAtUtc = now;

        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
        db.PasswordResetTokens.Add(new PasswordResetTokenEntity
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAtUtc = now.AddMinutes(30)
        });
        await db.SaveChangesAsync(ct);
        await emailSender.SendAsync(user.Email, rawToken, ct);
    }
}

public sealed class ResetPasswordHandler(
    IAppDbContext db,
    IPasswordHasher<MarketUserEntity> passwordHasher) : IRequestHandler<ResetPasswordCommand>
{
    public async Task Handle(ResetPasswordCommand request, CancellationToken ct)
    {
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.Token)));
        var now = DateTime.UtcNow;
        var resetToken = await db.PasswordResetTokens.FirstOrDefaultAsync(
            x => x.TokenHash == tokenHash && x.UsedAtUtc == null && x.ExpiresAtUtc > now, ct);
        if (resetToken is null)
            throw new MarketConflictException("Link za promjenu lozinke je nevažeći ili je istekao.");

        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == resetToken.UserId && x.IsEnabled, ct);
        if (user is null)
            throw new MarketConflictException("Link za promjenu lozinke je nevažeći ili je istekao.");

        user.PasswordHash = passwordHasher.HashPassword(user, request.NewPassword);
        user.TokenVersion++;
        resetToken.UsedAtUtc = now;

        var activeRefreshTokens = await db.RefreshTokens
            .Where(x => x.UserId == user.Id && !x.IsRevoked)
            .ToListAsync(ct);
        foreach (var refreshToken in activeRefreshTokens) refreshToken.IsRevoked = true;

        var otherResetTokens = await db.PasswordResetTokens
            .Where(x => x.UserId == user.Id && x.Id != resetToken.Id && x.UsedAtUtc == null)
            .ToListAsync(ct);
        foreach (var otherToken in otherResetTokens) otherToken.UsedAtUtc = now;

        await db.SaveChangesAsync(ct);
    }
}
