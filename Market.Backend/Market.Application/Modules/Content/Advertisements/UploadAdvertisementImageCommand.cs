namespace Market.Application.Modules.Content.Advertisements;
public sealed record UploadedImageDto(string RelativeUrl);
public sealed record UploadAdvertisementImageCommand(string ContentType, byte[] Content) : IRequest<UploadedImageDto>;
public sealed class UploadAdvertisementImageValidator : AbstractValidator<UploadAdvertisementImageCommand>
{ public UploadAdvertisementImageValidator() { RuleFor(x => x.ContentType).Must(x => new[] { "image/jpeg", "image/png", "image/webp", "image/gif" }.Contains(x)).WithMessage("Dozvoljene su JPEG, PNG, WebP i GIF slike."); RuleFor(x => x.Content).NotEmpty().Must(x => x.Length <= 5 * 1024 * 1024).WithMessage("Slika može imati najviše 5 MB."); } }
public sealed class UploadAdvertisementImageHandler(IAppCurrentUser user, IFileStorage files) : IRequestHandler<UploadAdvertisementImageCommand, UploadedImageDto>
{
    public async Task<UploadedImageDto> Handle(UploadAdvertisementImageCommand request, CancellationToken ct)
    {
        if (!user.IsAdmin) throw new MarketConflictException("Samo administrator može postavljati slike oglasa.");
        var file = await files.SaveAsync(request.Content, request.ContentType, "advertisement-images", ct); return new(file.RelativeUrl);
    }
}
