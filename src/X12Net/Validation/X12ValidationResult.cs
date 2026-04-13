namespace woliver13.X12Net.Validation;

/// <summary>The outcome of an <see cref="X12Validator"/> run.</summary>
public sealed class X12ValidationResult
{
    internal X12ValidationResult(IReadOnlyList<X12ValidationError> errors)
    {
        Errors = errors;
    }

    /// <summary><c>true</c> when no validation errors were found.</summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>All validation errors; empty when <see cref="IsValid"/> is <c>true</c>.</summary>
    public IReadOnlyList<X12ValidationError> Errors { get; }
}
