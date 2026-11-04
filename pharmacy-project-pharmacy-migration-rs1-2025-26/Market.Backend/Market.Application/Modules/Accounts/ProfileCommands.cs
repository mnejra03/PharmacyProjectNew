namespace Market.Application.Modules.Accounts;
public sealed record UserProfileDto(int Id, string Email, string FirstName, string LastName, string? PhoneNumber, bool IsAdmin, bool IsPharmacist, bool IsCustomer, string? ProfileImageUrl);
public sealed record GetMyProfileQuery : IRequest<UserProfileDto>;
public sealed class GetMyProfileHandler(IAppDbContext db, IAppCurrentUser current) : IRequestHandler<GetMyProfileQuery, UserProfileDto>
{
    public async Task<UserProfileDto> Handle(GetMyProfileQuery request, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == current.UserId, ct) ?? throw new MarketNotFoundException("Korisnik nije pronađen.");
        return new(user.Id, user.Email, user.FirstName, user.LastName, user.PhoneNumber, user.IsAdmin, user.IsPharmacist, user.IsCustomer, user.ProfileImageUrl);
    }
}
public sealed record UpdateMyProfileCommand(string FirstName, string LastName, string? PhoneNumber) : IRequest<UserProfileDto>;
public sealed class UpdateMyProfileValidator : AbstractValidator<UpdateMyProfileCommand>
{ public UpdateMyProfileValidator() { RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100); RuleFor(x => x.LastName).NotEmpty().MaximumLength(100); RuleFor(x => x.PhoneNumber).MaximumLength(30); } }
public sealed class UpdateMyProfileHandler(IAppDbContext db, IAppCurrentUser current) : IRequestHandler<UpdateMyProfileCommand, UserProfileDto>
{
    public async Task<UserProfileDto> Handle(UpdateMyProfileCommand request, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == current.UserId, ct) ?? throw new MarketNotFoundException("Korisnik nije pronađen.");
        user.FirstName = request.FirstName.Trim(); user.LastName = request.LastName.Trim(); user.PhoneNumber = request.PhoneNumber?.Trim();
        await db.SaveChangesAsync(ct); return new(user.Id, user.Email, user.FirstName, user.LastName, user.PhoneNumber, user.IsAdmin, user.IsPharmacist, user.IsCustomer, user.ProfileImageUrl);
    }
}
public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest;
public sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
{ public ChangePasswordValidator() { RuleFor(x => x.CurrentPassword).NotEmpty(); RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8).MaximumLength(100).NotEqual(x => x.CurrentPassword); } }
public sealed class ChangePasswordHandler(IAppDbContext db, IAppCurrentUser current, IPasswordHasher<MarketUserEntity> hasher) : IRequestHandler<ChangePasswordCommand>
{
    public async Task Handle(ChangePasswordCommand request, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == current.UserId, ct) ?? throw new MarketNotFoundException("Korisnik nije pronađen.");
        if (hasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword) == PasswordVerificationResult.Failed) throw new MarketConflictException("Trenutna lozinka nije tačna.");
        user.PasswordHash = hasher.HashPassword(user, request.NewPassword); user.TokenVersion++;
        var refreshTokens = await db.RefreshTokens.Where(x => x.UserId == user.Id && !x.IsRevoked).ToListAsync(ct);
        foreach (var token in refreshTokens) token.IsRevoked = true;
        await db.SaveChangesAsync(ct);
    }
}
public sealed record GetUsersQuery(int Page = 1, int PageSize = 20, string? Search = null, string? Role = null) : IRequest<PageResult<UserProfileDto>>;
public sealed class GetUsersValidator : AbstractValidator<GetUsersQuery>
{ public GetUsersValidator() { RuleFor(x => x.Page).GreaterThan(0); RuleFor(x => x.PageSize).InclusiveBetween(1, 100); RuleFor(x => x.Role).Must(x => string.IsNullOrWhiteSpace(x) || new[] { "admin", "pharmacist", "customer" }.Contains(x.Trim().ToLowerInvariant())); } }
public sealed class GetUsersHandler(IAppDbContext db, IAppCurrentUser current) : IRequestHandler<GetUsersQuery, PageResult<UserProfileDto>>
{
    public async Task<PageResult<UserProfileDto>> Handle(GetUsersQuery request, CancellationToken ct)
    {
        if (!current.IsAdmin) throw new MarketConflictException("Samo administrator može pregledati korisnike.");
        var q = db.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Search)) { var s = request.Search.Trim().ToLower(); q = q.Where(x => x.Email.ToLower().Contains(s) || x.FirstName.ToLower().Contains(s) || x.LastName.ToLower().Contains(s)); }
        q = request.Role?.Trim().ToLowerInvariant() switch { "admin" => q.Where(x => x.IsAdmin), "pharmacist" => q.Where(x => x.IsPharmacist), "customer" => q.Where(x => x.IsCustomer), _ => q };
        var total = await q.CountAsync(ct); var items = await q.OrderBy(x => x.Id).Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(x => new UserProfileDto(x.Id, x.Email, x.FirstName, x.LastName, x.PhoneNumber, x.IsAdmin, x.IsPharmacist, x.IsCustomer, x.ProfileImageUrl)).ToListAsync(ct);
        return new PageResult<UserProfileDto> { Items = items, TotalItems = total, PageSize = request.PageSize, CurrentPage = request.Page, IncludedTotal = true, TotalPages = (int)Math.Ceiling(total / (double)request.PageSize) };
    }
}
public sealed record UpdateUserCommand(int Id, string FirstName, string LastName, string? PhoneNumber, bool IsAdmin, bool IsPharmacist, bool IsCustomer) : IRequest<UserProfileDto>;
public sealed class UpdateUserValidator : AbstractValidator<UpdateUserCommand>
{ public UpdateUserValidator() { RuleFor(x => x.Id).GreaterThan(0); RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100); RuleFor(x => x.LastName).NotEmpty().MaximumLength(100); RuleFor(x => x.PhoneNumber).MaximumLength(30); RuleFor(x => x).Must(x => x.IsAdmin || x.IsPharmacist || x.IsCustomer).WithMessage("Korisnik mora imati najmanje jednu ulogu."); } }
public sealed class UpdateUserHandler(IAppDbContext db, IAppCurrentUser current) : IRequestHandler<UpdateUserCommand, UserProfileDto>
{
    public async Task<UserProfileDto> Handle(UpdateUserCommand request, CancellationToken ct)
    {
        if (!current.IsAdmin) throw new MarketConflictException("Samo administrator može mijenjati korisnike.");
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == request.Id, ct) ?? throw new MarketNotFoundException("Korisnik nije pronađen.");
        var changed = user.IsAdmin != request.IsAdmin || user.IsPharmacist != request.IsPharmacist || user.IsCustomer != request.IsCustomer;
        user.FirstName = request.FirstName.Trim(); user.LastName = request.LastName.Trim(); user.PhoneNumber = request.PhoneNumber?.Trim();
        user.IsAdmin = request.IsAdmin; user.IsPharmacist = request.IsPharmacist; user.IsCustomer = request.IsCustomer;
        if (changed) { user.TokenVersion++; var tokens = await db.RefreshTokens.Where(x => x.UserId == user.Id && !x.IsRevoked).ToListAsync(ct); foreach (var token in tokens) token.IsRevoked = true; }
        await db.SaveChangesAsync(ct); return new(user.Id, user.Email, user.FirstName, user.LastName, user.PhoneNumber, user.IsAdmin, user.IsPharmacist, user.IsCustomer, user.ProfileImageUrl);
    }
}
public sealed record DeleteUserCommand(int Id) : IRequest;
public sealed class DeleteUserValidator : AbstractValidator<DeleteUserCommand> { public DeleteUserValidator() => RuleFor(x => x.Id).GreaterThan(0); }
public sealed class DeleteUserHandler(IAppDbContext db, IAppCurrentUser current) : IRequestHandler<DeleteUserCommand>
{
    public async Task Handle(DeleteUserCommand request, CancellationToken ct)
    {
        if (!current.IsAdmin) throw new MarketConflictException("Samo administrator može ukloniti korisnike.");
        if (request.Id == current.UserId) throw new MarketConflictException("Ne možete ukloniti vlastiti administratorski račun.");
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == request.Id, ct) ?? throw new MarketNotFoundException("Korisnik nije pronađen.");
        user.IsEnabled = false; user.TokenVersion++; db.Users.Remove(user);
        var tokens = await db.RefreshTokens.Where(x => x.UserId == user.Id && !x.IsRevoked).ToListAsync(ct); foreach (var token in tokens) token.IsRevoked = true;
        await db.SaveChangesAsync(ct);
    }
}
