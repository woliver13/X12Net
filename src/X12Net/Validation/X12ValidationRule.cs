using woliver13.X12Net.Core;

namespace woliver13.X12Net.Validation;

/// <summary>
/// A single validation rule: inspects a flat list of segments and appends any
/// errors it finds to <paramref name="errors"/>.
/// </summary>
public delegate void X12ValidationRule(
    IReadOnlyList<X12Segment> segments,
    List<X12ValidationError>  errors);
