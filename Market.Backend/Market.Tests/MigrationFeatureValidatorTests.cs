using Market.Application.Modules.Auth.Commands.Register;
using Market.Application.Modules.Communication.Chat;
using Market.Application.Modules.Content.Recipes;

namespace Market.Tests;

public sealed class MigrationFeatureValidatorTests
{
    [Fact]
    public void Register_rejects_short_password_and_invalid_email()
    {
        var result = new RegisterCommandValidator().Validate(new RegisterCommand
        {
            Email = "not-an-email", Password = "123", FirstName = "A", LastName = "B"
        });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "Email");
        Assert.Contains(result.Errors, error => error.PropertyName == "Password");
    }

    [Fact]
    public void Chat_message_requires_a_recipient_and_nonempty_content()
    {
        var result = new SendChatMessageValidator().Validate(new SendChatMessageCommand { ReceiverId = 0, Message = " " });
        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("Pending", true)]
    [InlineData("Approved", true)]
    [InlineData("Rejected", true)]
    [InlineData("Unknown", false)]
    public void Recipe_status_is_restricted_to_supported_values(string status, bool valid)
    {
        var result = new UpdateRecipeStatusValidator().Validate(new UpdateRecipeStatusCommand(1, status));
        Assert.Equal(valid, result.IsValid);
    }
}
