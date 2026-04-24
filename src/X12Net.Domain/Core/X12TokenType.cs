namespace woliver13.X12Net.Core;

/// <summary>Classifies a token emitted by <see cref="X12Tokenizer"/>.</summary>
public enum X12TokenType
{
    /// <summary>The segment identifier (first element, e.g. "ISA", "GS", "ST").</summary>
    SegmentId,

    /// <summary>A data element value within a segment.</summary>
    ElementData,

    /// <summary>A component (sub-element) value within a composite element.</summary>
    ComponentData,

    /// <summary>Marks the end of a segment.</summary>
    SegmentTerminator,

    /// <summary>A repeated value within an element, separated by the repetition separator (ISA11).</summary>
    RepetitionData,
}
