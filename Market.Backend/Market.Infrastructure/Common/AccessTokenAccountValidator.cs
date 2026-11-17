using Market.Application.Abstractions;
using Market.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Market.Infrastructure.Common;

public sealed class AccessTokenAccountValidator(DatabaseContext db) : IAccessTokenAccountValidator
{
    public Task<bool> IsCurrentAsync(int userId, int tokenVersion, CancellationToken cancellationToken) =>
        db.Users.IgnoreQueryFilters().AsNoTracking().AnyAsync(
            user => user.Id == userId &&
                    user.TokenVersion == tokenVersion &&
                    user.IsEnabled &&
                    !user.IsDeleted,
            cancellationToken);
}
