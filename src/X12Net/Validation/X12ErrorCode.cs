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

    /// <summary>A structurally required segment (ISA, IEA, GS, GE, ST, SE) is absent.</summary>
    MissingRequiredSegment,
}
