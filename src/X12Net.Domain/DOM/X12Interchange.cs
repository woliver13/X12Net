using System.Text;
using woliver13.X12Net.Core;

namespace woliver13.X12Net.DOM;

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
    /// <remarks>
    /// This overload serializes each <see cref="X12Transaction"/> back to an EDI string
    /// before calling the factory. Use the
    /// <see cref="GetTransactions{T}(Func{X12Transaction, X12Delimiters, T})"/> overload
    /// to avoid that allocation when the factory can accept an <see cref="X12Transaction"/>
    /// directly.
    /// </remarks>
    public IEnumerable<T> GetTransactions<T>(Func<string, T> factory)
    {
        foreach (var group in FunctionalGroups)
            foreach (var tx in group.Transactions)
                yield return factory(tx.ToEdi(Delimiters));
    }

    /// <summary>
    /// Projects every transaction in this interchange through <paramref name="factory"/>,
    /// passing the parsed <see cref="X12Transaction"/> and the interchange
    /// <see cref="Delimiters"/> directly — without re-serializing to EDI text.
    /// </summary>
    /// <remarks>
    /// Prefer this overload over <see cref="GetTransactions{T}(Func{string, T})"/> when
    /// the factory can consume an <see cref="X12Transaction"/> directly, as it avoids
    /// the per-transaction EDI serialization allocation.
    /// </remarks>
    public IEnumerable<T> GetTransactions<T>(Func<X12Transaction, X12Delimiters, T> factory)
    {
        foreach (var group in FunctionalGroups)
            foreach (var tx in group.Transactions)
                yield return factory(tx, Delimiters);
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
        var all = X12SegmentParser.ParseAll(input, delimiters).ToList();
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
        var isa = segments.FirstOrDefault(s => s.SegmentId == "ISA")
            ?? throw new InvalidOperationException("No ISA segment found.");

        var (interchangeBody, iea, _) = ConsumeUntil(segments, segments.IndexOf(isa), "IEA");

        var groups = new List<X12FunctionalGroup>();
        int i = 0;
        while (i < interchangeBody.Count)
        {
            if (interchangeBody[i].SegmentId == "GS")
            {
                var (group, consumed) = ParseGroup(interchangeBody, i);
                groups.Add(group);
                i += consumed;
            }
            else i++;
        }

        return new X12Interchange(isa, groups, iea, delimiters);
    }

    private static (X12FunctionalGroup group, int segmentsConsumed) ParseGroup(
        IReadOnlyList<X12Segment> segments, int start)
    {
        var gs = segments[start];
        var (groupBody, ge, next) = ConsumeUntil(segments, start, "GE");

        var transactions = new List<X12Transaction>();
        int i = 0;
        while (i < groupBody.Count)
        {
            if (groupBody[i].SegmentId == "ST")
            {
                var (tx, consumed) = ParseTransaction(groupBody, i);
                transactions.Add(tx);
                i += consumed;
            }
            else i++;
        }

        return (new X12FunctionalGroup(gs, transactions, ge), next - start);
    }

    private static (X12Transaction tx, int segmentsConsumed) ParseTransaction(
        IReadOnlyList<X12Segment> segments, int start)
    {
        var st = segments[start];
        var (body, se, next) = ConsumeUntil(segments, start, "SE");
        return (new X12Transaction(st, body, se), next - start);
    }

    // Collects all segments from segments[startIndex+1] up to (but not including) the
    // first segment whose SegmentId equals closingId. Returns the body, the closer, and
    // the index one past the closer (for the caller to resume scanning).
    private static (IReadOnlyList<X12Segment> body, X12Segment closer, int next)
        ConsumeUntil(IReadOnlyList<X12Segment> segments, int startIndex, string closingId)
    {
        var body = new List<X12Segment>();
        for (int i = startIndex + 1; i < segments.Count; i++)
        {
            if (segments[i].SegmentId == closingId)
                return (body, segments[i], i + 1);
            body.Add(segments[i]);
        }
        throw new InvalidOperationException($"No {closingId} segment found.");
    }
}
