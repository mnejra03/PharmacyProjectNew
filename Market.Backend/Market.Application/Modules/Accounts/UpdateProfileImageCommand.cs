namespace Market.Application.Modules.Accounts;
public sealed record UpdateProfileImageCommand(string ContentType, byte[] Content) : IRequest<UserProfileDto>;
public sealed class UpdateProfileImageValidator : AbstractValidator<UpdateProfileImageCommand>
{ public UpdateProfileImageValidator() { RuleFor(x => x.ContentType).Must(x => new[] { "image/jpeg", "image/png", "image/webp", "image/gif" }.Contains(x)).WithMessage("Dozvoljene su JPEG, PNG, WebP i GIF slike."); RuleFor(x => x.Content).NotEmpty().Must(x => x.Length <= 5 * 1024 * 1024).WithMessage("Slika može imati najviše 5 MB."); } }
public sealed class UpdateProfileImageHandler(IAppDbContext db, IAppCurrentUser user, IFileStorage files) : IRequestHandler<UpdateProfileImageCommand, UserProfileDto>
{
    public async Task<UserProfileDto> Handle(UpdateProfileImageCommand request, CancellationToken ct)
    {
        var account = await db.Users.FirstOrDefaultAsync(x => x.Id == user.UserId, ct) ?? throw new MarketNotFoundException("Korisnik nije pronađen.");
        var file = await files.SaveAsync(request.Content, request.ContentType, "profile-images", ct); account.ProfileImageUrl = file.RelativeUrl;
        await db.SaveChangesAsync(ct); return new(account.Id, account.Email, account.FirstName, account.LastName, account.PhoneNumber, account.IsAdmin, account.IsPharmacist, account.IsCustomer, account.ProfileImageUrl);
    }
}
