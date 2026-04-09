namespace X12Net.Validation;

/// <summary>Identifies the category of a validation failure.</summary>
public enum X12ErrorCode
{
    /// <summary>ISA06 or ISA08 sender/receiver ID exceeds 15 characters.</summary>
    IsaSenderIdTooLong,

    /// <summary>ISA13 interchange control number does not match IEA02.</summary>
    ControlNumberMismatch,

    /// <summary>GS06 group control number does not match GE02.</summary>
    GroupControlNumberMismatch,

    /// <summary>SE01 transaction segment count does not match actual body segment count.</summary>
    SeSegmentCountMismatch,

    /// <summary>IEA01 functional group count does not match the number of GS segments.</summary>
    IeaGroupCountMismatch,

    /// <summary>GE01 transaction set count does not match the number of ST segments in the group.</summary>
    GeTransactionCountMismatch,

    /// <summary>IEA01, GE01, or SE01 contains a non-numeric value that cannot be parsed as an integer.</summary>
    MalformedControlField,

    /// <summary>A structurally required segment (ISA, IEA, GS, GE, ST, SE) is absent.</summary>
    MissingRequiredSegment,

    // ── EB segment (271) ──────────────────────────────────────────────────

    /// <summary>EB01 Eligibility or Benefit Information Code is absent or blank.</summary>
    EbMissingEligibilityCode,

    /// <summary>EB01 value is not in code set 235.</summary>
    EbInvalidEligibilityCode,

    /// <summary>EB02 Coverage Level Code is not in code set 1205.</summary>
    EbInvalidCoverageLevelCode,

    /// <summary>EB03 Service Type Code component is not in code set 1365.</summary>
    EbInvalidServiceTypeCode,

    /// <summary>EB04 Insurance Type Code is not in the allowed subset.</summary>
    EbInvalidInsuranceTypeCode,

    /// <summary>EB05 Plan Coverage Description exceeds 50 characters.</summary>
    EbPlanDescriptionTooLong,

    /// <summary>EB06 Time Period Qualifier is not in code set 615.</summary>
    EbInvalidTimePeriodQualifier,

    /// <summary>EB07 Monetary Amount is negative.</summary>
    EbNegativeMonetaryAmount,

    /// <summary>EB08 Percent is less than 0 or greater than 100.00.</summary>
    EbPercentOutOfRange,

    /// <summary>EB09 Quantity Qualifier is not in code set 673.</summary>
    EbInvalidQuantityQualifier,

    /// <summary>EB10 Quantity is negative.</summary>
    EbNegativeQuantity,

    /// <summary>EB11 Authorization/Certification Required is not N, Y, or U.</summary>
    EbInvalidAuthorizationIndicator,

    /// <summary>EB12 In-Plan Network Indicator is not N, U, W, or Y.</summary>
    EbInvalidInPlanNetworkIndicator,

    /// <summary>EB09 and EB10 must both be present or both absent.</summary>
    EbQuantityQualifierWithoutQuantity,

    /// <summary>EB06 is present but none of EB07, EB08, or EB10 contain a value.</summary>
    EbTimePeriodRequiresAmount,
}
