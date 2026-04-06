using System.Text;
using X12Net.Core;
using X12Net.IO;

namespace X12Net.DOM;

/// <summary>
/// An ST/SE transaction set within a functional group.
/// <see cref="Segments"/> contains the body segments (excluding ST and SE themselves).
/// </summary>
public sealed class X12Transaction
{
    internal X12Transaction(X12Segment st, IReadOnlyList<X12Segment> body, X12Segment se)
    {
        ST       = st;
        Segments = body;
        SE       = se;
    }

    /// <summary>The ST (Transaction Set Header) segment.</summary>
    public X12Segment ST { get; }

    /// <summary>The SE (Transaction Set Trailer) segment.</summary>
    public X12Segment SE { get; }

    /// <summary>Body segments between ST and SE (excluding ST and SE).</summary>
    public IReadOnlyList<X12Segment> Segments { get; }

    /// <summary>Serializes the transaction (ST…body…SE) back to EDI X12 text.</summary>
    public string ToEdi(X12Delimiters delimiters)
    {
        var sb = new StringBuilder();
        sb.Append(ST.ToEdi(delimiters));
        foreach (var seg in Segments)
            sb.Append(seg.ToEdi(delimiters));
        sb.Append(SE.ToEdi(delimiters));
        return sb.ToString();
    }
}
