namespace Market.Application.Abstractions;

public interface IAccessTokenAccountValidator
{
    Task<bool> IsCurrentAsync(int userId, int tokenVersion, CancellationToken cancellationToken);
}
