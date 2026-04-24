using woliver13.X12Net.Core;
using woliver13.X12Net.IO;

namespace woliver13.X12Net.Validation;

/// <summary>
/// Validates the structural integrity of an EDI X12 interchange.
/// No external dependencies — usable standalone or as the engine behind
/// a FluentValidation <c>AbstractValidator</c> adapter.
/// </summary>
public static class X12Validator
{
    /// <summary>The seven built-in structural rules, in execution order.</summary>
    public static IReadOnlyList<X12ValidationRule> DefaultRules { get; } = new X12ValidationRule[]
    {
        CheckRequiredEnvelopeSegments,
        CheckIsaFieldLengths,
        CheckInterchangeControlNumber,
        CheckGroupControlNumbers,
        CheckIeaGroupCount,
        CheckGeTransactionCounts,
        CheckSeSegmentCounts,
    };

    /// <summary>Validates the raw EDI X12 text and returns a result containing any errors.</summary>
    /// <param name="input">Raw EDI X12 text.</param>
    /// <param name="extraRules">Additional rules appended after the built-in rules.</param>
    /// <param name="builtInRules">When <c>false</c>, built-in rules are skipped.</param>
    public static X12ValidationResult Validate(string input,
        IEnumerable<X12ValidationRule>? extraRules = null,
        bool builtInRules = true)
    {
        var errors = new List<X12ValidationError>();
        using var reader = new X12Reader(input);
        var segments = reader.ReadAllSegments().ToList();

        if (builtInRules)
        {
            CheckRequiredEnvelopeSegments(segments, errors);
            if (errors.Any(e => e.Code == X12ErrorCode.MissingRequiredSegment))
                return new X12ValidationResult(errors);  // structural check failed — stop early

            foreach (var rule in DefaultRules.Skip(1))
                rule(segments, errors);
        }

        if (extraRules is not null)
            foreach (var rule in extraRules)
                rule(segments, errors);

        return new X12ValidationResult(errors);
    }

    // ── Rules ─────────────────────────────────────────────────────────────

    private static void CheckRequiredEnvelopeSegments(
        IReadOnlyList<X12Segment> segments, List<X12ValidationError> errors)
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
        IReadOnlyList<X12Segment> segments, List<X12ValidationError> errors)
    {
        var isa = segments.First(s => s.SegmentId == "ISA");

        // ISA06 = sender ID (element index 6), ISA08 = receiver ID (element index 8)
        // Each must be exactly X12Constants.IsaIdFieldWidth chars in the fixed-width ISA —
        // raw value is already padded; we flag values that are longer before padding.
        var senderId = isa[6].TrimEnd();
        if (senderId.Length > X12Constants.IsaIdFieldWidth)
            errors.Add(new X12ValidationError(
                X12ErrorCode.IsaSenderIdTooLong,
                $"ISA06 sender ID '{senderId}' exceeds {X12Constants.IsaIdFieldWidth} characters."));
    }

    private static void CheckInterchangeControlNumber(
        IReadOnlyList<X12Segment> segments, List<X12ValidationError> errors)
    {
        var isa = segments.First(s => s.SegmentId == "ISA");
        var iea = segments.First(s => s.SegmentId == "IEA");

        // ISA13 must equal IEA02
        var isaControlNumber = isa[13];
        var ieaControlNumber = iea[2];
        if (isaControlNumber.TrimStart('0') != ieaControlNumber.TrimStart('0'))
            errors.Add(new X12ValidationError(
                X12ErrorCode.ControlNumberMismatch,
                $"ISA13 control number '{isaControlNumber}' does not match IEA02 '{ieaControlNumber}'."));
    }

    private static void CheckGroupControlNumbers(
        IReadOnlyList<X12Segment> segments, List<X12ValidationError> errors)
    {
        // Pair each GS with its matching GE by stack depth
        var stack = new Stack<X12Segment>();
        foreach (var seg in segments)
        {
            if (seg.SegmentId == "GS") { stack.Push(seg); continue; }
            if (seg.SegmentId == "GE" && stack.Count > 0)
            {
                var gs = stack.Pop();
                var gsControlNumber = gs[6];
                var geControlNumber = seg[2];
                if (gsControlNumber.TrimStart('0') != geControlNumber.TrimStart('0'))
                    errors.Add(new X12ValidationError(
                        X12ErrorCode.GroupControlNumberMismatch,
                        $"GS06 '{gsControlNumber}' does not match GE02 '{geControlNumber}'."));
            }
        }
    }

    private static void CheckIeaGroupCount(
        IReadOnlyList<X12Segment> segments, List<X12ValidationError> errors)
    {
        var iea    = segments.First(s => s.SegmentId == "IEA");
        int actual = segments.Count(s => s.SegmentId == "GS");

        var ieaDeclaredGroupCount = iea[1];
        if (!int.TryParse(ieaDeclaredGroupCount, out int declared))
        {
            errors.Add(new X12ValidationError(
                X12ErrorCode.MalformedControlField,
                $"IEA01 '{ieaDeclaredGroupCount}' is not a valid integer."));
            return;
        }

        if (declared != actual)
            errors.Add(new X12ValidationError(
                X12ErrorCode.IeaGroupCountMismatch,
                $"IEA01 declares {declared} functional group(s) but found {actual}."));
    }

    private static void CheckGeTransactionCounts(
        IReadOnlyList<X12Segment> segments, List<X12ValidationError> errors)
    {
        // For each GS/GE pair, GE01 must equal the number of ST segments inside the group.
        int stCount = 0;
        bool inGroup = false;

        foreach (var seg in segments)
        {
            if (seg.SegmentId == "GS")  { inGroup = true; stCount = 0; continue; }
            if (seg.SegmentId == "ST" && inGroup) { stCount++; continue; }
            if (seg.SegmentId == "GE" && inGroup)
            {
                var geDeclaredTxCount = seg[1];
                if (!int.TryParse(geDeclaredTxCount, out int declared))
                {
                    errors.Add(new X12ValidationError(
                        X12ErrorCode.MalformedControlField,
                        $"GE01 '{geDeclaredTxCount}' is not a valid integer."));
                }
                else if (declared != stCount)
                {
                    errors.Add(new X12ValidationError(
                        X12ErrorCode.GeTransactionCountMismatch,
                        $"GE01 declares {declared} transaction(s) but group contains {stCount}."));
                }
                inGroup = false;
            }
        }
    }

    private static void CheckSeSegmentCounts(
        IReadOnlyList<X12Segment> segments, List<X12ValidationError> errors)
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
                int actual = (i - stIndex.Value) + 1; // ST through SE inclusive
                var seDeclaredCount = seg[1];
                if (!int.TryParse(seDeclaredCount, out int declared))
                {
                    errors.Add(new X12ValidationError(
                        X12ErrorCode.MalformedControlField,
                        $"SE01 '{seDeclaredCount}' is not a valid integer."));
                }
                else if (declared != actual)
                {
                    errors.Add(new X12ValidationError(
                        X12ErrorCode.SeSegmentCountMismatch,
                        $"SE01 declares {declared} segment(s) but transaction contains {actual}."));
                }
                stIndex = null;
            }
        }
    }
}
