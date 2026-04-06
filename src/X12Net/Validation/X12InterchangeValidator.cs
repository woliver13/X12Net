using FluentValidation;
using FluentValidation.Results;

namespace X12Net.Validation;

/// <summary>
/// FluentValidation adapter for <see cref="X12Validator"/>.
/// Validates raw EDI X12 interchange text using FluentValidation's standard API.
/// </summary>
public sealed class X12InterchangeValidator : AbstractValidator<string>
{
    public X12InterchangeValidator()
    {
        RuleFor(input => input)
            .Custom((input, ctx) =>
            {
                var result = X12Validator.Validate(input);
                foreach (var error in result.Errors)
                    ctx.AddFailure(new ValidationFailure(error.Code.ToString(), error.Message)
                    {
                        ErrorCode = error.Code.ToString()
                    });
            });
    }
}
