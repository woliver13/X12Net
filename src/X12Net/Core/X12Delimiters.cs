namespace woliver13.X12Net.Core;

/// <summary>
/// The three delimiter characters used in an EDI X12 interchange.
/// Detected from the ISA header or supplied explicitly.
/// </summary>
public readonly struct X12Delimiters : IEquatable<X12Delimiters>
{
    /// <summary>Initializes delimiter values.</summary>
    public X12Delimiters(char elementSeparator, char componentSeparator, char segmentTerminator)
    {
        ElementSeparator   = elementSeparator;
        ComponentSeparator = componentSeparator;
        SegmentTerminator  = segmentTerminator;
    }

    /// <summary>Separates elements within a segment (ISA position 3; typically <c>*</c>).</summary>
    public char ElementSeparator { get; }

    /// <summary>Separates components within a composite element (ISA16; typically <c>:</c>).</summary>
    public char ComponentSeparator { get; }

    /// <summary>Terminates each segment (character after ISA16; typically <c>~</c>).</summary>
    public char SegmentTerminator { get; }

    /// <summary>Default delimiters used when no ISA header is present.</summary>
    public static readonly X12Delimiters Default = new('*', ':', '~');

    /// <summary>
    /// Detects delimiters from an ISA header.
    /// Falls back to <see cref="Default"/> when the input is not a full ISA.
    /// </summary>
    public static X12Delimiters FromIsa(string input)
    {
        try   { return X12IsaParser.Parse(input); }
        // Intentional swallow: input that is too short or missing an ISA header
        // (e.g. a bare segment string) is valid in several read paths. Falling back
        // to Default delimiters lets callers handle partial input gracefully without
        // propagating an ArgumentException through every call site.
        catch (ArgumentException) { return Default; }
    }

    /// <inheritdoc/>
    public bool Equals(X12Delimiters other) =>
        ElementSeparator   == other.ElementSeparator &&
        ComponentSeparator == other.ComponentSeparator &&
        SegmentTerminator  == other.SegmentTerminator;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is X12Delimiters d && Equals(d);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + ElementSeparator.GetHashCode();
            hash = hash * 31 + ComponentSeparator.GetHashCode();
            hash = hash * 31 + SegmentTerminator.GetHashCode();
            return hash;
        }
    }

    /// <inheritdoc/>
    public override string ToString() =>
        $"ElementSep='{ElementSeparator}' ComponentSep='{ComponentSeparator}' SegTerm='{SegmentTerminator}'";
}
