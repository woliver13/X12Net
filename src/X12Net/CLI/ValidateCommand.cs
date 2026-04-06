using X12Net.Validation;

namespace X12Net.CLI;

/// <summary>Result of a <see cref="ValidateCommand"/> execution.</summary>
public sealed class ValidateResult
{
    internal ValidateResult(X12ValidationResult inner)
    {
        IsValid = inner.IsValid;
        Errors  = inner.Errors.Select(e => e.ToString()).ToList();
    }

    /// <summary><c>true</c> when the interchange passed all structural validation rules.</summary>
    public bool IsValid { get; }

    /// <summary>Human-readable error strings; empty when valid.</summary>
    public IReadOnlyList<string> Errors { get; }
}

/// <summary>
/// CLI command: validate the structural integrity of an EDI X12 interchange.
/// </summary>
public static class ValidateCommand
{
    /// <summary>Executes validation against <paramref name="input"/>.</summary>
    public static ValidateResult Execute(string input) =>
        new(X12Validator.Validate(input));
}
