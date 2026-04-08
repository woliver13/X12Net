using X12Net.IO;

namespace X12Net.Validation;

/// <summary>
/// Validates a 271 Health Care Eligibility/Benefit Response interchange by
/// combining interchange-level structural checks (<see cref="X12Validator"/>)
/// with EB segment element-level semantic checks (<see cref="EbSegmentValidator"/>).
/// Neither existing class is modified.
/// </summary>
/// <remarks>
/// Validation runs in two phases:
/// <list type="number">
///   <item><description>
///     <see cref="X12Validator.Validate"/> is called first. If a required envelope
///     segment (ISA or IEA) is missing the reader cannot safely scan the body, so
///     the structural errors are returned immediately without running phase 2.
///   </description></item>
///   <item><description>
///     Every EB segment in the interchange is passed to
///     <see cref="EbSegmentValidator.Validate"/>. All resulting errors are appended
///     to the structural error list before the combined result is returned.
///   </description></item>
/// </list>
/// </remarks>
public static class Ts271Validator
{
    /// <summary>
    /// Validates the raw 271 EDI interchange text and returns a combined result
    /// containing both structural and EB element-level errors.
    /// </summary>
    /// <param name="input">Raw EDI X12 interchange text for a 271 transaction.</param>
    /// <returns>
    /// An <see cref="X12ValidationResult"/> whose <see cref="X12ValidationResult.Errors"/>
    /// list combines all structural errors (from <see cref="X12Validator"/>) followed by
    /// all EB element errors (from <see cref="EbSegmentValidator"/>), one entry per
    /// failing element per EB segment.
    /// </returns>
    public static X12ValidationResult Validate(string input)
    {
        // ── Phase 1: interchange-level structural check ───────────────────
        var structural = X12Validator.Validate(input);

        // A missing required envelope segment means the interchange is malformed at the
        // outermost level — scanning for EB segments is unsafe, so return immediately.
        if (structural.Errors.Any(e => e.Code == X12ErrorCode.MissingRequiredSegment))
            return structural;

        // ── Phase 2: EB element semantic check ────────────────────────────
        var allErrors = new List<X12ValidationError>(structural.Errors);

        using var reader = new X12Reader(input);
        foreach (var seg in reader.ReadAllSegments())
        {
            if (seg.SegmentId != "EB") continue;
            allErrors.AddRange(EbSegmentValidator.Validate(seg).Errors);
        }

        return new X12ValidationResult(allErrors);
    }
}
