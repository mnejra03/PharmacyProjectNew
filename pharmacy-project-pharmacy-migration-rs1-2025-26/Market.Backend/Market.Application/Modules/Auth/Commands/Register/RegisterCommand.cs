namespace Market.Application.Modules.Auth.Commands.Register;
using Market.Application.Modules.Auth.Commands.Login;

public sealed class RegisterCommand : IRequest<LoginCommandDto>
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
}
