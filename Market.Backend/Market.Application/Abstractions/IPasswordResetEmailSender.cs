namespace Market.Application.Abstractions;

public interface IPasswordResetEmailSender
{
    Task SendAsync(string email, string token, CancellationToken cancellationToken);
}
