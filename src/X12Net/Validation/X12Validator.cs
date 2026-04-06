using X12Net.IO;

namespace X12Net.Validation;

/// <summary>
/// Validates the structural integrity of an EDI X12 interchange.
/// No external dependencies — usable standalone or as the engine behind
/// a FluentValidation <c>AbstractValidator</c> adapter.
/// </summary>
public static class X12Validator
{
    /// <summary>Validates the raw EDI X12 text and returns a result containing any errors.</summary>
    public static X12ValidationResult Validate(string input)
    {
        var errors = new List<X12ValidationError>();
        using var reader = new X12Reader(input);
        var segments = reader.ReadAllSegments().ToList();

        CheckRequiredEnvelopeSegments(segments, errors);
        if (errors.Any(e => e.Code == X12ErrorCode.MissingRequiredSegment))
            return new X12ValidationResult(errors);  // structural check failed — stop early

        CheckIsaFieldLengths(segments, errors);
        CheckInterchangeControlNumber(segments, errors);
        CheckGroupControlNumbers(segments, errors);
        CheckIeaGroupCount(segments, errors);
        CheckGeTransactionCounts(segments, errors);
        CheckSeSegmentCounts(segments, errors);

        return new X12ValidationResult(errors);
    }

    // ── Rules ─────────────────────────────────────────────────────────────

    private static void CheckRequiredEnvelopeSegments(
        List<X12Segment> segments, List<X12ValidationError> errors)
    {
        foreach (var required in new[] { "ISA", "IEA" })
        {
            if (!segments.Any(s => s.SegmentId == required))
                errors.Add(new X12ValidationError(
                    X12ErrorCode.MissingRequiredSegment,
                    $"Required segment '{required}' is missing from the interchange."));
        }
    }

    private static void CheckIsaFieldLengths(
        List<X12Segment> segments, List<X12ValidationError> errors)
    {
        var isa = segments.First(s => s.SegmentId == "ISA");

        // ISA06 = sender ID (element index 6), ISA08 = receiver ID (element index 8)
        // Each must be exactly 15 chars in the fixed-width ISA — raw value is already padded;
        // we flag values that are longer than 15 before padding.
        if (isa[6].TrimEnd().Length > 15)
            errors.Add(new X12ValidationError(
                X12ErrorCode.IsaSenderIdTooLong,
                $"ISA06 sender ID '{isa[6].TrimEnd()}' exceeds 15 characters."));
    }

    private static void CheckInterchangeControlNumber(
        List<X12Segment> segments, List<X12ValidationError> errors)
    {
        var isa = segments.First(s => s.SegmentId == "ISA");
        var iea = segments.First(s => s.SegmentId == "IEA");

        // ISA13 must equal IEA02
        if (isa[13].TrimStart('0') != iea[2].TrimStart('0'))
            errors.Add(new X12ValidationError(
                X12ErrorCode.ControlNumberMismatch,
                $"ISA13 control number '{isa[13]}' does not match IEA02 '{iea[2]}'."));
    }

    private static void CheckGroupControlNumbers(
        List<X12Segment> segments, List<X12ValidationError> errors)
    {
        // Pair each GS with its matching GE by stack depth
        var stack = new Stack<X12Segment>();
        foreach (var seg in segments)
        {
            if (seg.SegmentId == "GS") { stack.Push(seg); continue; }
            if (seg.SegmentId == "GE" && stack.Count > 0)
            {
                var gs = stack.Pop();
                if (gs[6].TrimStart('0') != seg[2].TrimStart('0'))
                    errors.Add(new X12ValidationError(
                        X12ErrorCode.GroupControlNumberMismatch,
                        $"GS06 '{gs[6]}' does not match GE02 '{seg[2]}'."));
            }
        }
    }

    private static void CheckIeaGroupCount(
        List<X12Segment> segments, List<X12ValidationError> errors)
    {
        var iea      = segments.First(s => s.SegmentId == "IEA");
        int declared = int.Parse(iea[1]);
        int actual   = segments.Count(s => s.SegmentId == "GS");

        if (declared != actual)
            errors.Add(new X12ValidationError(
                X12ErrorCode.IeaGroupCountMismatch,
                $"IEA01 declares {declared} functional group(s) but found {actual}."));
    }

    private static void CheckGeTransactionCounts(
        List<X12Segment> segments, List<X12ValidationError> errors)
    {
        // For each GS/GE pair, GE01 must equal the number of ST segments inside the group.
        int stCount = 0;
        bool inGroup = false;
        X12Segment? currentGe = null;

        foreach (var seg in segments)
        {
            if (seg.SegmentId == "GS")  { inGroup = true; stCount = 0; continue; }
            if (seg.SegmentId == "ST" && inGroup) { stCount++; continue; }
            if (seg.SegmentId == "GE" && inGroup)
            {
                currentGe = seg;
                int declared = int.Parse(seg[1]);
                if (declared != stCount)
                    errors.Add(new X12ValidationError(
                        X12ErrorCode.GeTransactionCountMismatch,
                        $"GE01 declares {declared} transaction(s) but group contains {stCount}."));
                inGroup = false;
            }
        }
    }

    private static void CheckSeSegmentCounts(
        List<X12Segment> segments, List<X12ValidationError> errors)
    {
        // For each ST/SE pair, SE01 must equal the count of segments
        // from ST through SE inclusive.
        int? stIndex = null;
        for (int i = 0; i < segments.Count; i++)
        {
            var seg = segments[i];
            if (seg.SegmentId == "ST")  { stIndex = i; continue; }
            if (seg.SegmentId == "SE" && stIndex is not null)
            {
                int declared = int.Parse(seg[1]);
                int actual   = (i - stIndex.Value) + 1; // ST through SE inclusive
                if (declared != actual)
                    errors.Add(new X12ValidationError(
                        X12ErrorCode.SeSegmentCountMismatch,
                        $"SE01 declares {declared} segment(s) but transaction contains {actual}."));
                stIndex = null;
            }
        }
    }
}
