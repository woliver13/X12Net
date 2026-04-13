namespace woliver13.X12Net.Core;

/// <summary>
/// Extracts the three delimiter characters from the first ISA segment in a string
/// by counting element separators rather than relying on fixed character positions.
/// This handles both standard 106-char ISA and any non-standard-width variants.
/// </summary>
internal static class X12IsaParser
{
    private const int IsaElementCount = 16;
    private const int MinIsaLength    = 106;

    /// <summary>
    /// Parses delimiter characters from an ISA header.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Input is null/empty, shorter than <see cref="MinIsaLength"/>, or does not start with "ISA".
    /// </exception>
    internal static X12Delimiters Parse(string isaInput)
    {
        if (string.IsNullOrEmpty(isaInput) || isaInput.Length < MinIsaLength)
            throw new ArgumentException(
                $"ISA segment must be at least {MinIsaLength} characters.", nameof(isaInput));

        if (!isaInput.StartsWith("ISA", StringComparison.Ordinal))
            throw new ArgumentException("Input does not start with 'ISA'.", nameof(isaInput));

        char elementSep = isaInput[3];
        int  sepCount   = 0;

        for (int i = 4; i < isaInput.Length; i++)
        {
            if (isaInput[i] == elementSep)
            {
                sepCount++;
                if (sepCount == IsaElementCount - 1)
                {
                    char componentSep = i + 1 < isaInput.Length ? isaInput[i + 1] : ':';
                    char segmentTerm  = i + 2 < isaInput.Length ? isaInput[i + 2] : '~';
                    return new X12Delimiters(elementSep, componentSep, segmentTerm);
                }
            }
        }

        // Malformed ISA — enough chars but fewer than 15 element separators
        return new X12Delimiters(elementSep, ':', '~');
    }
}
