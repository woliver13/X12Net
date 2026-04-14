using woliver13.HL7Net.Core;

namespace woliver13.HL7Net.IO;

/// <summary>
/// Reads and parses HL7 v2.x messages from text, yielding one <see cref="Hl7Segment"/> per segment.
/// Tolerant: accepts \r, \n, or \r\n as segment terminators.
/// </summary>
public sealed class Hl7Reader
{
    private readonly string _text;
    private readonly int _maxSegments;

    /// <summary>Default maximum number of segments allowed before throwing <see cref="Hl7MemoryCapException"/>.</summary>
    public const int DefaultMaxSegments = 10_000;

    /// <summary>Initializes an <see cref="Hl7Reader"/>.</summary>
    /// <param name="text">The full HL7 message text.</param>
    /// <param name="maxSegments">Maximum segments to read before throwing. Defaults to 10 000.</param>
    public Hl7Reader(string text, int maxSegments = DefaultMaxSegments)
    {
        _text        = text ?? throw new ArgumentNullException(nameof(text));
        _maxSegments = maxSegments;
    }

    /// <summary>
    /// Enumerates all segments in the message.
    /// </summary>
    /// <exception cref="Hl7MemoryCapException">Thrown when more than <c>maxSegments</c> segments are encountered.</exception>
    public IEnumerable<Hl7Segment> ReadAllSegments()
    {
        int count = 0;
        foreach (var line in SplitSegments(_text))
        {
            if (line.Length == 0) continue;
            count++;
            if (count > _maxSegments)
                throw new Hl7MemoryCapException(_maxSegments);
            yield return ParseSegment(line);
        }
    }

    /// <summary>
    /// Asynchronously enumerates all segments in the message.
    /// </summary>
    public async IAsyncEnumerable<Hl7Segment> ReadAllSegmentsAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] System.Threading.CancellationToken cancellationToken = default)
    {
        int count = 0;
        foreach (var line in SplitSegments(_text))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (line.Length == 0) continue;
            count++;
            if (count > _maxSegments)
                throw new Hl7MemoryCapException(_maxSegments);
            yield return ParseSegment(line);
            await System.Threading.Tasks.Task.CompletedTask.ConfigureAwait(false);
        }
    }

    private static IEnumerable<string> SplitSegments(string text)
    {
        // Normalise \r\n → \r, then split on \r or \n
        var normalized = text.Replace("\r\n", "\r").Replace('\n', '\r');
        return normalized.Split('\r');
    }

    private static Hl7Segment ParseSegment(string line)
    {
        // MSH is special: MSH-1 is the field separator character itself
        if (line.Length >= 4 && line.StartsWith("MSH", StringComparison.Ordinal))
        {
            char sep = line[3];
            // Split on the separator, but MSH[1] = sep char, MSH[2] = encoding chars (positions 4-7)
            // Split normally — sep char and encoding chars will appear as fields 1 and 2
            var parts = line.Split(sep);
            var fields = new List<string>(parts.Length);
            // parts[0] = "MSH", parts[1] = "^~\&" (encoding chars)
            // MSH-1 is the field separator character itself (not in parts)
            fields.Add(sep.ToString());  // MSH-1: field separator
            for (int i = 1; i < parts.Length; i++)
                fields.Add(parts[i]);    // MSH-2 = parts[1] = "^~\&", MSH-3 = parts[2], ...
            return new Hl7Segment("MSH", fields.AsReadOnly());
        }
        else
        {
            // Non-MSH: split on field separator (detect from MSH or fall back to '|')
            // For simplicity, use '|' as default — non-MSH always uses standard '|'
            var parts = line.Split('|');
            var fields = new List<string>(parts.Length - 1);
            for (int i = 1; i < parts.Length; i++)
                fields.Add(parts[i]);
            return new Hl7Segment(parts[0], fields.AsReadOnly());
        }
    }
}
