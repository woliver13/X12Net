namespace woliver13.X12Net.Core;

/// <summary>
/// Centralised constants for ISA segment fixed-width field specifications.
/// All values derive from the ANSI ASC X12 interchange control structure.
/// </summary>
internal static class X12Constants
{
    /// <summary>
    /// Minimum character length of a well-formed ISA segment string (106 chars).
    /// ISA01–ISA16 with element separators plus the segment terminator.
    /// </summary>
    internal const int IsaMinLength = 106;

    /// <summary>
    /// Length of the ISA segment body (105 chars), excluding the final segment terminator.
    /// Used to validate that all fixed-width fields are correctly padded before appending the terminator.
    /// </summary>
    internal const int IsaBodyLength = 105;

    /// <summary>
    /// Number of data elements in the ISA segment (ISA01–ISA16 = 16 elements).
    /// </summary>
    internal const int IsaElementCount = 16;

    /// <summary>
    /// Fixed width of the ISA sender/receiver ID fields (ISA06 and ISA08), in characters.
    /// Values shorter than 15 chars must be right-padded with spaces; longer values are invalid.
    /// </summary>
    internal const int IsaIdFieldWidth = 15;
}
