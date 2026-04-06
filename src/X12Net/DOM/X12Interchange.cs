using System.Text;
using X12Net.Core;
using X12Net.IO;

namespace X12Net.DOM;

/// <summary>
/// The top-level hierarchical model of an EDI X12 interchange.
/// Parses an ISA/IEA envelope into <see cref="FunctionalGroups"/>,
/// each of which contains <see cref="X12Transaction"/> objects.
/// </summary>
public sealed class X12Interchange
{
    private X12Interchange(
        X12Segment isa,
        IReadOnlyList<X12FunctionalGroup> groups,
        X12Segment iea,
        X12Delimiters delimiters)
    {
        ISA              = isa;
        FunctionalGroups = groups;
        IEA              = iea;
        Delimiters       = delimiters;
    }

    /// <summary>The delimiters detected from the ISA header.</summary>
    public X12Delimiters Delimiters { get; }

    /// <summary>
    /// Projects every transaction in this interchange through <paramref name="factory"/>,
    /// returning a typed sequence. Use a generated <c>Parse</c> method as the factory,
    /// for example: <c>interchange.GetTransactions(Ts271.Parse)</c>.
    /// </summary>
    public IEnumerable<T> GetTransactions<T>(Func<string, T> factory)
    {
        foreach (var group in FunctionalGroups)
            foreach (var tx in group.Transactions)
                yield return factory(tx.ToEdi(Delimiters));
    }

    /// <summary>The ISA (Interchange Control Header) segment.</summary>
    public X12Segment ISA { get; }

    /// <summary>The IEA (Interchange Control Trailer) segment.</summary>
    public X12Segment IEA { get; }

    /// <summary>All functional groups within this interchange.</summary>
    public IReadOnlyList<X12FunctionalGroup> FunctionalGroups { get; }

    // ── Factory ───────────────────────────────────────────────────────────

    /// <summary>Parses a raw EDI X12 interchange into a hierarchical <see cref="X12Interchange"/>.</summary>
    public static X12Interchange Parse(string input)
    {
        var delimiters = X12Delimiters.FromIsa(input);
        using var reader = new X12Reader(input, delimiters);
        var all = reader.ReadAllSegments().ToList();
        return Build(all, delimiters);
    }

    /// <summary>Serializes the interchange back to EDI X12 text.</summary>
    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.Append(ISA.ToEdi(Delimiters));
        foreach (var group in FunctionalGroups)
        {
            sb.Append(group.GS.ToEdi(Delimiters));
            foreach (var tx in group.Transactions)
                sb.Append(tx.ToEdi(Delimiters));
            sb.Append(group.GE.ToEdi(Delimiters));
        }
        sb.Append(IEA.ToEdi(Delimiters));

        return sb.ToString();
    }

    // ── Builder ───────────────────────────────────────────────────────────

    private static X12Interchange Build(List<X12Segment> segments, X12Delimiters delimiters)
    {
        X12Segment? isa = null;
        X12Segment? iea = null;
        var groups = new List<X12FunctionalGroup>();

        int i = 0;
        while (i < segments.Count)
        {
            var seg = segments[i];
            switch (seg.SegmentId)
            {
                case "ISA":
                    isa = seg;
                    i++;
                    break;

                case "IEA":
                    iea = seg;
                    i++;
                    break;

                case "GS":
                    var (group, advance) = ParseGroup(segments, i);
                    groups.Add(group);
                    i += advance;
                    break;

                default:
                    i++;
                    break;
            }
        }

        if (isa is null) throw new InvalidOperationException("No ISA segment found.");
        if (iea is null) throw new InvalidOperationException("No IEA segment found.");

        return new X12Interchange(isa, groups, iea, delimiters);
    }

    private static (X12FunctionalGroup group, int segmentsConsumed) ParseGroup(
        List<X12Segment> segments, int start)
    {
        var gs = segments[start];
        X12Segment? ge = null;
        var transactions = new List<X12Transaction>();

        int i = start + 1;
        while (i < segments.Count)
        {
            var seg = segments[i];
            if (seg.SegmentId == "GE") { ge = seg; i++; break; }
            if (seg.SegmentId == "ST")
            {
                var (tx, advance) = ParseTransaction(segments, i);
                transactions.Add(tx);
                i += advance;
            }
            else
            {
                i++;
            }
        }

        if (ge is null) throw new InvalidOperationException("No GE segment found for GS.");
        return (new X12FunctionalGroup(gs, transactions, ge), i - start);
    }

    private static (X12Transaction tx, int segmentsConsumed) ParseTransaction(
        List<X12Segment> segments, int start)
    {
        var st = segments[start];
        X12Segment? se = null;
        var body = new List<X12Segment>();

        int i = start + 1;
        while (i < segments.Count)
        {
            var seg = segments[i];
            if (seg.SegmentId == "SE") { se = seg; i++; break; }
            body.Add(seg);
            i++;
        }

        if (se is null) throw new InvalidOperationException("No SE segment found for ST.");
        return (new X12Transaction(st, body, se), i - start);
    }
}
