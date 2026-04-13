using System.IO;
using System.Text;
using woliver13.X12Net.Core;

namespace woliver13.X12Net.IO;

/// <summary>
/// Reads EDI X12 text and surfaces it as a sequence of <see cref="X12Segment"/> objects.
/// Supports both synchronous and asynchronous enumeration.
/// </summary>
public sealed class X12Reader : IDisposable
{
    private readonly string?          _input;
    private readonly Stream?          _stream;
    private readonly Encoding?        _encoding;
    private readonly int              _maxSegments;
    private readonly X12Delimiters?   _delimiters;
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

    /// <summary>
    /// Initializes an <see cref="X12Reader"/> over a stream of EDI X12 text.
    /// The reader takes ownership of the stream and will dispose it when
    /// <see cref="Dispose"/> is called.
    /// </summary>
    /// <param name="stream">A readable stream containing EDI X12 text.</param>
    /// <param name="encoding">
    /// Character encoding to use when reading the stream.
    /// Defaults to UTF-8 with BOM detection when <c>null</c>.
    /// </param>
    /// <param name="maxSegments">
    /// Optional segment cap. When positive, <see cref="X12MemoryCapException"/> is thrown
    /// if the interchange contains more segments than this limit.
    /// Use 0 (default) for no limit.
    /// </param>
    /// <remarks>
    /// When using the async API (<see cref="ReadAllSegmentsAsync"/> /
    /// <see cref="ReadTransactionsAsync{T}"/>), the stream is read asynchronously before
    /// parsing begins, providing genuine async I/O unlike the string-based overloads.
    /// </remarks>
    public X12Reader(Stream stream, Encoding? encoding = null, int maxSegments = 0)
    {
        _stream      = stream ?? throw new ArgumentNullException(nameof(stream));
        _encoding    = encoding;
        _maxSegments = maxSegments;
    }

    // ── Synchronous API ───────────────────────────────────────────────────

    /// <summary>Returns all segments in the interchange, respecting the configured segment cap.</summary>
    public IEnumerable<X12Segment> ReadAllSegments()
    {
        ThrowIfDisposed();
        var content  = ReadContent();
        var resolved = _delimiters ?? X12Delimiters.FromIsa(content);
        return EnumerateWithCap(ParseSegments(content, resolved));
    }

    // ── Asynchronous API ──────────────────────────────────────────────────

    /// <summary>Asynchronously enumerates all segments in the interchange.</summary>
    /// <remarks>
    /// When constructed from a <see cref="Stream"/>, the stream is read asynchronously
    /// before parsing begins — this is genuine async I/O.
    /// When constructed from a string, this overload is CPU-bound; the async enumeration
    /// yields between segments but does not release the thread during parsing.
    /// </remarks>
    public async IAsyncEnumerable<X12Segment> ReadAllSegmentsAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        var content  = await ReadContentAsync().ConfigureAwait(false);
        var resolved = _delimiters ?? X12Delimiters.FromIsa(content);
        foreach (var segment in EnumerateWithCap(ParseSegments(content, resolved)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return segment;
        }
    }

    /// <summary>
    /// Streams all ST/SE transaction sets through a caller-provided factory,
    /// yielding the mapped result. X12Reader never knows the result type.
    /// </summary>
    public IEnumerable<T> ReadTransactions<T>(
        Func<X12Segment, IReadOnlyList<X12Segment>, X12Segment, T> factory)
    {
        ThrowIfDisposed();
        X12Segment? st = null;
        var body = new List<X12Segment>();

        var content  = ReadContent();
        var resolved = _delimiters ?? X12Delimiters.FromIsa(content);
        foreach (var seg in EnumerateWithCap(ParseSegments(content, resolved)))
        {
            if (seg.SegmentId == "ST")
            {
                st = seg;
                body = new List<X12Segment>();
            }
            else if (seg.SegmentId == "SE" && st is not null)
            {
                yield return factory(st, body.AsReadOnly(), seg);
                st = null;
                body = new List<X12Segment>();
            }
            else if (st is not null)
            {
                body.Add(seg);
            }
        }
    }

    /// <summary>
    /// Asynchronously streams all ST/SE transaction sets through a caller-provided factory.
    /// </summary>
    /// <remarks>
    /// When constructed from a <see cref="Stream"/>, the stream is read asynchronously
    /// before processing begins — this is genuine async I/O.
    /// When constructed from a string, this overload is CPU-bound.
    /// </remarks>
    public async IAsyncEnumerable<T> ReadTransactionsAsync<T>(
        Func<X12Segment, IReadOnlyList<X12Segment>, X12Segment, T> factory,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        var content  = await ReadContentAsync().ConfigureAwait(false);
        var resolved = _delimiters ?? X12Delimiters.FromIsa(content);
        X12Segment? st = null;
        var body = new List<X12Segment>();
        foreach (var seg in EnumerateWithCap(ParseSegments(content, resolved)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (seg.SegmentId == "ST")
            {
                st = seg;
                body = new List<X12Segment>();
            }
            else if (seg.SegmentId == "SE" && st is not null)
            {
                yield return factory(st, body.AsReadOnly(), seg);
                st = null;
                body = new List<X12Segment>();
            }
            else if (st is not null)
            {
                body.Add(seg);
            }
        }
    }

    // ── Content helpers ───────────────────────────────────────────────────

    private string ReadContent()
    {
        if (_stream != null)
        {
            using var sr = new StreamReader(_stream, _encoding ?? Encoding.UTF8,
                detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: true);
            return sr.ReadToEnd();
        }
        return _input!;
    }

    private async Task<string> ReadContentAsync()
    {
        if (_stream != null)
        {
            using var sr = new StreamReader(_stream, _encoding ?? Encoding.UTF8,
                detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: true);
            return await sr.ReadToEndAsync().ConfigureAwait(false);
        }
        await Task.Yield();
        return _input!;
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

    private static IEnumerable<X12Segment> ParseSegments(string input, X12Delimiters delimiters)
    {
        var segmentId    = string.Empty;
        var elements     = new List<string>();
        var composite    = new System.Text.StringBuilder();
        // True once the first ElementData token has been seen in the current segment.
        // Used to know whether to flush composite before starting the next element.
        bool pendingFlush = false;

        var tokens = X12Tokenizer.Tokenize(input, delimiters);
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
                    composite.Append(delimiters.ComponentSeparator);
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

    /// <summary>
    /// Releases resources held by this reader.
    /// </summary>
    /// <remarks>
    /// Dispose is a no-op when constructed from a string.
    /// When constructed from a <see cref="Stream"/>, Dispose releases the stream.
    /// Safe to call more than once.
    /// </remarks>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _stream?.Dispose();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(X12Reader));
    }
}
