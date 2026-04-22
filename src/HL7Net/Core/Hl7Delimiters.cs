namespace woliver13.HL7Net.Core;

/// <summary>
/// HL7 v2.x delimiter characters detected from the MSH segment header.
/// </summary>
public sealed class Hl7Delimiters
{
    /// <summary>Field separator character. Always '|' in standard HL7.</summary>
    public char FieldSeparator { get; }

    /// <summary>Component separator character. Usually '^'.</summary>
    public char ComponentSeparator { get; }

    /// <summary>Repetition separator. Usually '~'.</summary>
    public char RepetitionSeparator { get; }

    /// <summary>Escape character. Usually '\'.</summary>
    public char EscapeCharacter { get; }

    /// <summary>Sub-component separator. Usually '&amp;'.</summary>
    public char SubComponentSeparator { get; }

    private Hl7Delimiters(char field, char component, char repetition, char escape, char subComponent)
    {
        FieldSeparator       = field;
        ComponentSeparator   = component;
        RepetitionSeparator  = repetition;
        EscapeCharacter      = escape;
        SubComponentSeparator = subComponent;
    }

    /// <summary>
    /// Detects delimiters from the first line of an HL7 message.
    /// The line must start with "MSH" followed immediately by the field separator.
    /// MSH-2 must contain at least 4 encoding characters: component, repetition, escape, sub-component.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the MSH line is malformed.</exception>
    public static Hl7Delimiters FromMsh(string mshLine)
    {
        if (mshLine is null) throw new ArgumentNullException(nameof(mshLine));
        if (!mshLine.StartsWith("MSH", StringComparison.Ordinal) || mshLine.Length < 8)
            throw new ArgumentException("MSH line must start with 'MSH' and contain delimiter characters.", nameof(mshLine));

        char field     = mshLine[3];
        char component = mshLine[4];
        char repeat    = mshLine[5];
        char escape    = mshLine[6];
        char sub       = mshLine[7];

        return new Hl7Delimiters(field, component, repeat, escape, sub);
    }
}
