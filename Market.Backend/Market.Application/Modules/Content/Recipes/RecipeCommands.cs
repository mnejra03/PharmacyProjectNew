namespace Market.Application.Modules.Content.Recipes;
using Market.Application.Modules.Communication.Notifications;
using Market.Domain.Entities.Communication;
public sealed record RecipeDto(int Id, int UserId, DateTime DateOfIssue, string DoctorFirstName, string DoctorLastName, string Status, bool HasScan, string SenderName = "", string SenderEmail = "");
public sealed class CreateRecipeCommand : IRequest<RecipeDto>
{ public string DoctorFirstName { get; init; } = string.Empty; public string DoctorLastName { get; init; } = string.Empty; public byte[]? Scan { get; init; } }
public sealed class CreateRecipeValidator : AbstractValidator<CreateRecipeCommand>
{ public CreateRecipeValidator() { RuleFor(x => x.DoctorFirstName).NotEmpty().MaximumLength(100); RuleFor(x => x.DoctorLastName).NotEmpty().MaximumLength(100); RuleFor(x => x.Scan).Must(x => x is null || x.Length <= 10 * 1024 * 1024).WithMessage("Sken može imati najviše 10 MB."); } }
public sealed class CreateRecipeHandler(IAppDbContext db, IAppCurrentUser user, IChatNotifier notifier) : IRequestHandler<CreateRecipeCommand, RecipeDto>
{
    public async Task<RecipeDto> Handle(CreateRecipeCommand request, CancellationToken ct)
    {
        if (!user.IsAuthenticated || user.UserId is not int userId || !user.IsCustomer) throw new MarketConflictException("Samo prijavljeni kupci mogu poslati recept.");
        var recipe = new RecipeEntity { UserId = userId, DateOfIssue = DateTime.UtcNow, DoctorFirstName = request.DoctorFirstName.Trim(), DoctorLastName = request.DoctorLastName.Trim(), Scan = request.Scan, Status = "Pending" };
        db.Recipes.Add(recipe);
        var sender = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId, ct);
        var pharmacistNotifications = new List<NotificationEntity>();
        var pharmacists = await db.Users.Where(x => x.IsPharmacist && x.IsEnabled).ToListAsync(ct);
        foreach (var pharmacist in pharmacists)
        {
            var notification = new NotificationEntity
            {
                UserId = pharmacist.Id, SenderId = userId, Title = "Novi recept za pregled",
                Message = $"Kupac {sender?.FirstName} {sender?.LastName} poslao je recept za dr. {recipe.DoctorFirstName} {recipe.DoctorLastName}.",
                Type = "recipe", CreatedAt = DateTime.UtcNow
            };
            db.Notifications.Add(notification);
            pharmacistNotifications.Add(notification);
        }
        await db.SaveChangesAsync(ct);
        foreach (var notification in pharmacistNotifications)
            await notifier.SendNotificationAsync(notification.UserId,
                new NotificationDto(notification.Id, notification.Title, notification.Message, notification.CreatedAt, notification.IsRead, notification.Type, notification.SenderId), ct);
        return new(recipe.Id, recipe.UserId, recipe.DateOfIssue, recipe.DoctorFirstName, recipe.DoctorLastName, recipe.Status, recipe.Scan is not null);
    }
}
public sealed record GetRecipesQuery(bool Mine) : IRequest<IReadOnlyList<RecipeDto>>;
public sealed class GetRecipesHandler(IAppDbContext db, IAppCurrentUser user) : IRequestHandler<GetRecipesQuery, IReadOnlyList<RecipeDto>>
{
    public async Task<IReadOnlyList<RecipeDto>> Handle(GetRecipesQuery request, CancellationToken ct)
    {
        if (!user.IsAuthenticated || user.UserId is null) throw new MarketConflictException("Prijava je obavezna.");
        if (!request.Mine && !user.IsPharmacist && !user.IsAdmin) throw new MarketConflictException("Samo farmaceut može pregledati recepte.");
        var q = db.Recipes.AsNoTracking()
            .Join(db.Users.AsNoTracking(), recipe => recipe.UserId, sender => sender.Id,
                (recipe, sender) => new { Recipe = recipe, Sender = sender });
        if (request.Mine) q = q.Where(x => x.Recipe.UserId == user.UserId);
        return await q.OrderByDescending(x => x.Recipe.DateOfIssue)
            .Select(x => new RecipeDto(x.Recipe.Id, x.Recipe.UserId, x.Recipe.DateOfIssue,
                x.Recipe.DoctorFirstName, x.Recipe.DoctorLastName, x.Recipe.Status, x.Recipe.Scan != null,
                x.Sender.FirstName + " " + x.Sender.LastName, x.Sender.Email))
            .ToListAsync(ct);
    }
}
public sealed record UpdateRecipeStatusCommand(int Id, string Status) : IRequest;
public sealed class UpdateRecipeStatusValidator : AbstractValidator<UpdateRecipeStatusCommand>
{ public UpdateRecipeStatusValidator() { RuleFor(x => x.Id).GreaterThan(0); RuleFor(x => x.Status).Must(x => new[] { "Pending", "Approved", "Rejected" }.Contains(x)).WithMessage("Status mora biti Pending, Approved ili Rejected."); } }
public sealed class UpdateRecipeStatusHandler(IAppDbContext db, IAppCurrentUser user, IChatNotifier notifier) : IRequestHandler<UpdateRecipeStatusCommand>
{
    public async Task Handle(UpdateRecipeStatusCommand request, CancellationToken ct)
    {
        if (!user.IsPharmacist && !user.IsAdmin) throw new MarketConflictException("Samo farmaceut može mijenjati status recepta.");
        var recipe = await db.Recipes.FirstOrDefaultAsync(x => x.Id == request.Id, ct) ?? throw new MarketNotFoundException("Recept nije pronađen.");
        var changed = !string.Equals(recipe.Status, request.Status, StringComparison.Ordinal);
        recipe.Status = request.Status;
        var notification = changed ? new NotificationEntity
        {
            UserId = recipe.UserId, SenderId = user.UserId,
            Title = request.Status == "Approved" ? "Recept je odobren" : "Recept je odbijen",
            Message = $"Farmaceut je promijenio status recepta za dr. {recipe.DoctorFirstName} {recipe.DoctorLastName} u {request.Status}.",
            Type = "recipe_status", CreatedAt = DateTime.UtcNow
        } : null;
        if (notification is not null) db.Notifications.Add(notification);
        await db.SaveChangesAsync(ct);
        if (notification is not null)
            await notifier.SendNotificationAsync(notification.UserId,
                new NotificationDto(notification.Id, notification.Title, notification.Message, notification.CreatedAt, notification.IsRead, notification.Type, notification.SenderId), ct);
    }
}
public sealed record GetRecipeScanQuery(int Id) : IRequest<(byte[] Content, string ContentType)>;
public sealed class GetRecipeScanHandler(IAppDbContext db, IAppCurrentUser user) : IRequestHandler<GetRecipeScanQuery, (byte[] Content, string ContentType)>
{
    public async Task<(byte[] Content, string ContentType)> Handle(GetRecipeScanQuery request, CancellationToken ct)
    {
        var recipe = await db.Recipes.FirstOrDefaultAsync(x => x.Id == request.Id, ct) ?? throw new MarketNotFoundException("Recept nije pronađen.");
        if (user.UserId != recipe.UserId && !user.IsPharmacist && !user.IsAdmin) throw new MarketConflictException("Nemate pristup ovom skenu.");
        return (recipe.Scan ?? throw new MarketNotFoundException("Recept nema priložen sken."), "application/octet-stream");
    }
}
