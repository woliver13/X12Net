namespace woliver13.X12Net.Core;

/// <summary>An atomic unit produced by <see cref="X12Tokenizer"/>.</summary>
public sealed class X12Token
{
    /// <summary>Initializes an <see cref="X12Token"/>.</summary>
    public X12Token(X12TokenType type, string value)
    {
        Type = type;
        Value = value;
    }

    /// <summary>The kind of token.</summary>
    public X12TokenType Type { get; }

    /// <summary>The raw text value of the token.</summary>
    public string Value { get; }
}
