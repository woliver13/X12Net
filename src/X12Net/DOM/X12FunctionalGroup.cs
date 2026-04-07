using X12Net.Core;

namespace X12Net.DOM;

/// <summary>
/// A GS/GE functional group within an interchange.
/// Contains one or more <see cref="X12Transaction"/> objects.
/// </summary>
public sealed class X12FunctionalGroup
{
    internal X12FunctionalGroup(X12Segment gs, IReadOnlyList<X12Transaction> transactions, X12Segment ge)
    {
        GS           = gs;
        Transactions = transactions;
        GE           = ge;
    }

    /// <summary>The GS (Functional Group Header) segment.</summary>
    public X12Segment GS { get; }

    /// <summary>The GE (Functional Group Trailer) segment.</summary>
    public X12Segment GE { get; }

    /// <summary>All transactions within this functional group.</summary>
    public IReadOnlyList<X12Transaction> Transactions { get; }
}
