using X12Net.Core;
using X12Net.DOM;

namespace X12Net.IO;

/// <summary>
/// Reads EDI X12 text and surfaces it as a sequence of <see cref="X12Segment"/> objects.
/// Supports both synchronous and asynchronous enumeration.
/// </summary>
public sealed class X12Reader : IDisposable
{
    private readonly string          _input;
    private readonly int             _maxSegments;
    private readonly X12Delimiters?  _delimiters;
    private bool _disposed;

    /// <summary>
    /// Initializes an <see cref="X12Reader"/> over raw EDI text.
    /// </summary>
    /// <param name="input">The raw EDI X12 text to read.</param>
    /// <param name="maxSegments">
    /// Optional segment cap. When positive, <see cref="X12MemoryCapException"/> is thrown
    /// if the interchange contains more segments than this limit.
    /// Use 0 (default) for no limit.
    /// </param>
    public X12Reader(string input, int maxSegments = 0)
    {
        _input       = input ?? throw new ArgumentNullException(nameof(input));
        _maxSegments = maxSegments;
    }

    /// <summary>
    /// Initializes an <see cref="X12Reader"/> with explicit delimiters (no auto-detection).
    /// </summary>
    public X12Reader(string input, X12Delimiters delimiters, int maxSegments = 0)
    {
        _input       = input ?? throw new ArgumentNullException(nameof(input));
        _delimiters  = delimiters;
        _maxSegments = maxSegments;
    }

    // ── Synchronous API ───────────────────────────────────────────────────

    /// <summary>Returns all segments in the interchange, respecting the configured segment cap.</summary>
    public IEnumerable<X12Segment> ReadAllSegments()
    {
        ThrowIfDisposed();
        return EnumerateWithCap(ParseSegments(_input, _delimiters));
    }

    /// <summary>
    /// Streams all ST/SE transaction sets from the interchange without building
    /// a full <see cref="X12Net.DOM.X12Interchange"/> in memory.
    /// </summary>
    public IEnumerable<X12Transaction> ReadTransactions()
    {
        ThrowIfDisposed();
        X12Segment? st = null;
        var body = new List<X12Segment>();

        foreach (var seg in EnumerateWithCap(ParseSegments(_input, _delimiters)))
        {
            if (seg.SegmentId == "ST")
            {
                st = seg;
                body = new List<X12Segment>();
            }
            else if (seg.SegmentId == "SE" && st is not null)
            {
                yield return new X12Transaction(st, body.AsReadOnly(), seg);
                st = null;
                body = new List<X12Segment>();
            }
            else if (st is not null)
            {
                body.Add(seg);
            }
        }
    }

    // ── Asynchronous API ──────────────────────────────────────────────────

    /// <summary>Asynchronously enumerates all segments in the interchange.</summary>
    public async IAsyncEnumerable<X12Segment> ReadAllSegmentsAsync()
    {
        ThrowIfDisposed();
        await Task.Yield();
        foreach (var segment in EnumerateWithCap(ParseSegments(_input, _delimiters)))
            yield return segment;
    }

    /// <summary>
    /// Asynchronously streams all ST/SE transaction sets from the interchange.
    /// </summary>
    public async IAsyncEnumerable<X12Transaction> ReadTransactionsAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await Task.Yield();
        foreach (var tx in ReadTransactions())
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return tx;
        }
    }

    // ── Cap enforcement ───────────────────────────────────────────────────

    private IEnumerable<X12Segment> EnumerateWithCap(IEnumerable<X12Segment> source)
    {
        if (_maxSegments <= 0) { foreach (var s in source) yield return s; yield break; }
        int count = 0;
        foreach (var segment in source)
        {
            if (++count > _maxSegments)
                throw new X12MemoryCapException(_maxSegments);
            yield return segment;
        }
    }

    // ── Core parsing ──────────────────────────────────────────────────────

    private static IEnumerable<X12Segment> ParseSegments(string input, X12Delimiters? delimiters)
    {
        var segmentId    = string.Empty;
        var elements     = new List<string>();
        var composite    = new System.Text.StringBuilder();
        // True once the first ElementData token has been seen in the current segment.
        // Used to know whether to flush composite before starting the next element.
        bool pendingFlush = false;

        var tokens = delimiters.HasValue
            ? X12Tokenizer.Tokenize(input, delimiters.Value)
            : X12Tokenizer.Tokenize(input);
        foreach (var token in tokens)
        {
            switch (token.Type)
            {
                case X12TokenType.SegmentId:
                    segmentId    = token.Value;
                    elements     = new List<string>();
                    composite.Clear();
                    pendingFlush = false;
                    break;

                case X12TokenType.ElementData:
                    if (pendingFlush)
                    {
                        // Flush previous element (may be empty, simple, or composite)
                        elements.Add(composite.ToString());
                        composite.Clear();
                    }
                    composite.Append(token.Value);
                    pendingFlush = true;
                    break;

                case X12TokenType.ComponentData:
                    composite.Append(':');
                    composite.Append(token.Value);
                    break;

                case X12TokenType.SegmentTerminator:
                    if (pendingFlush)
                        elements.Add(composite.ToString());

                    if (segmentId.Length > 0)
                        yield return new X12Segment(segmentId, elements.AsReadOnly());

                    segmentId    = string.Empty;
                    elements     = new List<string>();
                    composite.Clear();
                    pendingFlush = false;
                    break;
            }
        }

        // Yield any trailing segment without a terminator
        if (pendingFlush)
            elements.Add(composite.ToString());
        if (segmentId.Length > 0)
            yield return new X12Segment(segmentId, elements.AsReadOnly());
    }

    // ── IDisposable ───────────────────────────────────────────────────────

    /// <inheritdoc/>
    public void Dispose() => _disposed = true;

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(X12Reader));
    }
}
