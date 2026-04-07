using System.Globalization;
using X12Net.Core;
using X12Net.IO;

namespace X12Net.Validation;

/// <summary>
/// Validates the element-level content of an EDI X12 EB segment as used in a
/// 271 Health Care Eligibility/Benefit Response transaction.
/// </summary>
public static class EbSegmentValidator
{
    // ── Code sets ─────────────────────────────────────────────────────────

    /// <summary>Code set 235 — Eligibility or Benefit Information (EB01).</summary>
    private static readonly HashSet<string> Eb01Codes = new(StringComparer.Ordinal)
    {
        "1","2","3","4","5","6","7","8","9",
        "A","B","C","CB","D","E","F","G","H","I","J","K","L","M","MC",
        "N","O","P","Q","R","S","T","U","V","W","X",
    };

    /// <summary>Code set 1205 — Coverage Level Code (EB02).</summary>
    private static readonly HashSet<string> Eb02Codes = new(StringComparer.Ordinal)
    {
        "CHD","DEP","ECH","EMP","ESP","FAM","IND","SPC","TWO",
    };

    /// <summary>Code set 1365 — Service Type Code components (EB03).</summary>
    private static readonly HashSet<string> Eb03Codes = new(StringComparer.Ordinal)
    {
        "1","2","3","4","5","6","7","8","9","10","11","12","13","14","15",
        "23","24","25","26","27","28","30","32","33","34","35","36","37","38",
        "40","41","42","43","44","45","46","47","48","49","50","51","53","54",
        "55","56","57","58","59","60","61","62","63","64","65","66","67","68",
        "69","70","71","72","73","74","75","76","77","78","79","80","81","82",
        "83","84","85","86","87","88","89","90","91","92","93","94","95","96",
        "97","98",
        "A0","A1","A2","A3","A4","A5","A6","A7","A8","A9",
        "AB","AC","AD","AE","AF","AG","AH","AI","AJ","AK",
    };

    /// <summary>Insurance type subset — used for EB04.</summary>
    private static readonly HashSet<string> Eb04Codes = new(StringComparer.Ordinal)
    {
        "AP","C1","CO","D","GP","HM","MA","MB","MC","MH","MP","OT","PR","PS","SP","TF","WC",
    };

    /// <summary>Code set 615 — Time Period Qualifier (EB06).</summary>
    private static readonly HashSet<string> Eb06Codes = new(StringComparer.Ordinal)
    {
        "6","7","13","21","24","25","26","27","28","29","30","33","34","35",
    };

    /// <summary>Code set 673 — Quantity Qualifier (EB09).</summary>
    private static readonly HashSet<string> Eb09Codes = new(StringComparer.Ordinal)
    {
        "CA","LA","LE","NE","VS",
    };

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>
    /// Validates the element content of a pre-parsed EB segment.
    /// </summary>
    public static X12ValidationResult Validate(X12Segment ebSegment)
    {
        var errors = new List<X12ValidationError>();

        CheckEb01Required(ebSegment, errors);
        CheckCodeSet(ebSegment, 2,  Eb02Codes, X12ErrorCode.EbInvalidCoverageLevelCode,
            "EB02 Coverage Level Code", "code set 1205", errors);
        CheckEb03ServiceTypeCode(ebSegment, errors);
        CheckCodeSet(ebSegment, 4,  Eb04Codes, X12ErrorCode.EbInvalidInsuranceTypeCode,
            "EB04 Insurance Type Code", "the allowed subset", errors);
        CheckEb05PlanDescription(ebSegment, errors);
        CheckCodeSet(ebSegment, 6,  Eb06Codes, X12ErrorCode.EbInvalidTimePeriodQualifier,
            "EB06 Time Period Qualifier", "code set 615", errors);
        CheckNonNegative(ebSegment, 7,  X12ErrorCode.EbNegativeMonetaryAmount, "EB07 Monetary Amount", errors);
        CheckEb08Percent(ebSegment, errors);
        CheckCodeSet(ebSegment, 9,  Eb09Codes, X12ErrorCode.EbInvalidQuantityQualifier,
            "EB09 Quantity Qualifier", "code set 673", errors);
        CheckNonNegative(ebSegment, 10, X12ErrorCode.EbNegativeQuantity, "EB10 Quantity", errors);
        CheckEb11AuthorizationRequired(ebSegment, errors);
        CheckEb12InPlanNetwork(ebSegment, errors);
        // EB13 Procedure Identifier: full code-set validation (ICD-10/CPT/HCPCS) is out of scope.
        CheckEb09Eb10Pairing(ebSegment, errors);
        CheckEb06RequiresNumericElement(ebSegment, errors);

        return new X12ValidationResult(errors);
    }

    /// <summary>
    /// Convenience overload: parses a single raw EB segment string and validates it.
    /// </summary>
    public static X12ValidationResult ValidateRaw(string ebSegmentText)
    {
        using var reader = new X12Reader(ebSegmentText);
        var seg = reader.ReadAllSegments().FirstOrDefault(s => s.SegmentId == "EB");
        if (seg is null)
        {
            var missing = new List<X12ValidationError>
            {
                new(X12ErrorCode.EbMissingEligibilityCode, "No EB segment found in input."),
            };
            return new X12ValidationResult(missing);
        }
        return Validate(seg);
    }

    // ── Element-specific rules ────────────────────────────────────────────

    private static void CheckEb01Required(X12Segment seg, List<X12ValidationError> errors)
    {
        string val = GetElement(seg, 1);
        if (string.IsNullOrEmpty(val))
        {
            errors.Add(new(X12ErrorCode.EbMissingEligibilityCode,
                "EB01 Eligibility or Benefit Information Code is required."));
            return;
        }
        if (!Eb01Codes.Contains(val))
            errors.Add(new(X12ErrorCode.EbInvalidEligibilityCode,
                $"EB01 value '{val}' is not a valid code set 235 value."));
    }

    private static void CheckEb03ServiceTypeCode(X12Segment seg, List<X12ValidationError> errors)
    {
        string val = GetElement(seg, 3);
        if (string.IsNullOrEmpty(val)) return;
        // EB03 is composite: X12Reader joins components with ':'.
        foreach (string component in val.Split(':'))
        {
            if (!string.IsNullOrEmpty(component) && !Eb03Codes.Contains(component))
                errors.Add(new(X12ErrorCode.EbInvalidServiceTypeCode,
                    $"EB03 Service Type Code component '{component}' is not a valid code set 1365 value."));
        }
    }

    private static void CheckEb05PlanDescription(X12Segment seg, List<X12ValidationError> errors)
    {
        string val = GetElement(seg, 5);
        if (!string.IsNullOrEmpty(val) && val.Length > 50)
            errors.Add(new(X12ErrorCode.EbPlanDescriptionTooLong,
                $"EB05 Plan Coverage Description exceeds 50 characters (length={val.Length})."));
    }

    private static void CheckEb08Percent(X12Segment seg, List<X12ValidationError> errors)
    {
        string val = GetElement(seg, 8);
        if (string.IsNullOrEmpty(val)) return;
        if (TryParseDecimal(val, out decimal pct) && (pct < 0 || pct > 100))
            errors.Add(new(X12ErrorCode.EbPercentOutOfRange,
                $"EB08 Percent '{val}' must be between 0.00 and 100.00."));
    }

    private static void CheckEb11AuthorizationRequired(X12Segment seg, List<X12ValidationError> errors)
    {
        string val = GetElement(seg, 11);
        if (!string.IsNullOrEmpty(val) && val != "N" && val != "Y" && val != "U")
            errors.Add(new(X12ErrorCode.EbInvalidAuthorizationIndicator,
                $"EB11 Authorization/Certification Required '{val}' must be N, Y, or U."));
    }

    private static void CheckEb12InPlanNetwork(X12Segment seg, List<X12ValidationError> errors)
    {
        string val = GetElement(seg, 12);
        if (!string.IsNullOrEmpty(val) && val != "N" && val != "U" && val != "W" && val != "Y")
            errors.Add(new(X12ErrorCode.EbInvalidInPlanNetworkIndicator,
                $"EB12 In-Plan Network Indicator '{val}' must be N, U, W, or Y."));
    }

    private static void CheckEb09Eb10Pairing(X12Segment seg, List<X12ValidationError> errors)
    {
        bool hasQualifier = !string.IsNullOrEmpty(GetElement(seg, 9));
        bool hasQuantity  = !string.IsNullOrEmpty(GetElement(seg, 10));
        if (hasQualifier != hasQuantity)
            errors.Add(new(X12ErrorCode.EbQuantityQualifierWithoutQuantity,
                "EB09 Quantity Qualifier and EB10 Quantity must both be present or both absent."));
    }

    private static void CheckEb06RequiresNumericElement(X12Segment seg, List<X12ValidationError> errors)
    {
        string eb06 = GetElement(seg, 6);
        if (string.IsNullOrEmpty(eb06)) return;
        bool hasAmount   = !string.IsNullOrEmpty(GetElement(seg, 7));
        bool hasPercent  = !string.IsNullOrEmpty(GetElement(seg, 8));
        bool hasQuantity = !string.IsNullOrEmpty(GetElement(seg, 10));
        if (!hasAmount && !hasPercent && !hasQuantity)
            errors.Add(new(X12ErrorCode.EbTimePeriodRequiresAmount,
                "EB06 Time Period Qualifier is present but none of EB07, EB08, or EB10 contain a value."));
    }

    // ── Shared rule helpers ───────────────────────────────────────────────

    /// <summary>
    /// Validates a single-valued element against a code set.
    /// Skips silently when the element is absent or empty.
    /// </summary>
    private static void CheckCodeSet(
        X12Segment seg, int index,
        HashSet<string> codes, X12ErrorCode errorCode,
        string elementLabel, string codeSetLabel,
        List<X12ValidationError> errors)
    {
        string val = GetElement(seg, index);
        if (string.IsNullOrEmpty(val)) return;
        if (!codes.Contains(val))
            errors.Add(new(errorCode,
                $"{elementLabel} '{val}' is not a valid {codeSetLabel} value."));
    }

    /// <summary>
    /// Validates that a numeric element is non-negative.
    /// Skips silently when the element is absent, empty, or non-numeric.
    /// </summary>
    private static void CheckNonNegative(
        X12Segment seg, int index,
        X12ErrorCode errorCode, string elementLabel,
        List<X12ValidationError> errors)
    {
        string val = GetElement(seg, index);
        if (string.IsNullOrEmpty(val)) return;
        if (TryParseDecimal(val, out decimal amount) && amount < 0)
            errors.Add(new(errorCode, $"{elementLabel} '{val}' must not be negative."));
    }

    // ── Low-level helpers ─────────────────────────────────────────────────

    /// <summary>
    /// Returns the element at <paramref name="oneBasedIndex"/>, or <see cref="string.Empty"/>
    /// when the segment has fewer elements (common for sparse segments with omitted trailing empties).
    /// </summary>
    private static string GetElement(X12Segment seg, int oneBasedIndex) =>
        oneBasedIndex <= seg.Elements.Count ? seg[oneBasedIndex] : string.Empty;

    private static bool TryParseDecimal(string value, out decimal result) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
}
