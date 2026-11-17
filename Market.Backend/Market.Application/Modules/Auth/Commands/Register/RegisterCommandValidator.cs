namespace Market.Application.Modules.Auth.Commands.Register;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(100);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PhoneNumber).MaximumLength(30);
        RuleFor(x => x.PhoneNumber)
            .Matches(@"^(?:(?:060\d{7}|06[1-9]\d{6})|\+?387(?:60\d{7}|6[1-9]\d{6}))$")
            .WithMessage("Unesite ispravan broj telefona.")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
    }
}
