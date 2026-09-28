using System.Linq.Expressions;
using FluentValidation;

namespace InvoiceApp.Features.Account.Validators;

public static class PasswordValidationExtensions
{
    public static IRuleBuilderOptions<T, string> ApplyPasswordRules<T>(
        this IRuleBuilder<T, string> ruleBuilder
    )
    {
        return ruleBuilder
            .NotEmpty()
            .WithMessage("Password is required.")
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters.");
    }

    public static IRuleBuilderOptions<T, string> ApplyConfirmPasswordRules<T>(
        this IRuleBuilder<T, string> ruleBuilder,
        Expression<Func<T, string>> passwordExpression
    )
    {
        return ruleBuilder
            .NotEmpty()
            .WithMessage("Please confirm the password.")
            .Equal(passwordExpression)
            .WithMessage("Passwords do not match.");
    }
}
