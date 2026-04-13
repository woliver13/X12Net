namespace woliver13.X12Net.Validation;

/// <summary>A single validation failure produced by <see cref="X12Validator"/>.</summary>
public sealed class X12ValidationError
{
    /// <summary>Initializes an <see cref="X12ValidationError"/>.</summary>
    public X12ValidationError(X12ErrorCode code, string message)
    {
        Code    = code;
        Message = message;
    }

    /// <summary>Structured error category.</summary>
    public X12ErrorCode Code { get; }

    /// <summary>Human-readable description of the failure.</summary>
    public string Message { get; }

    /// <inheritdoc/>
    public override string ToString() => $"[{Code}] {Message}";
}
