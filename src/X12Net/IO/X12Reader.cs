using X12Net.Core;

namespace X12Net.IO;

/// <summary>
/// Reads EDI X12 text and surfaces it as a sequence of <see cref="X12Segment"/> objects.
/// Supports both synchronous and asynchronous enumeration.
/// </summary>
public sealed class X12Reader : IDisposable
{
    private readonly string _input;
    private readonly int    _maxSegments;
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

    // ── Synchronous API ───────────────────────────────────────────────────

    /// <summary>Returns all segments in the interchange, respecting the configured segment cap.</summary>
    public IEnumerable<X12Segment> ReadAllSegments()
    {
        ThrowIfDisposed();
        return EnumerateWithCap(ParseSegments(_input));
    }

    // ── Asynchronous API ──────────────────────────────────────────────────

    /// <summary>Asynchronously enumerates all segments in the interchange.</summary>
    public async IAsyncEnumerable<X12Segment> ReadAllSegmentsAsync()
    {
        ThrowIfDisposed();
        await Task.Yield();
        foreach (var segment in EnumerateWithCap(ParseSegments(_input)))
            yield return segment;
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

    private static IEnumerable<X12Segment> ParseSegments(string input)
    {
        var segmentId    = string.Empty;
        var elements     = new List<string>();
        var composite    = new System.Text.StringBuilder();
        // True once the first ElementData token has been seen in the current segment.
        // Used to know whether to flush composite before starting the next element.
        bool pendingFlush = false;

        foreach (var token in X12Tokenizer.Tokenize(input))
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
