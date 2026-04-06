using X12Net.DOM;
using X12Net.IO;
using X12Net.Validation;

namespace X12Net.Schema;

/// <summary>
/// Validates a raw EDI X12 transaction against an <see cref="X12TransactionSchema"/>,
/// checking that all required segments are present.
/// </summary>
public static class X12SchemaValidator
{
    /// <summary>Validates <paramref name="input"/> against the supplied schema.</summary>
    public static X12ValidationResult Validate(string input, X12TransactionSchema schema)
    {
        using var reader = new X12Reader(input);
        var presentIds = new HashSet<string>(
            reader.ReadAllSegments().Select(s => s.SegmentId),
            StringComparer.OrdinalIgnoreCase);

        var errors = new List<X12ValidationError>();
        foreach (var seg in schema.Segments)
        {
            if (seg.IsRequired && !presentIds.Contains(seg.SegmentId))
                errors.Add(new X12ValidationError(
                    X12ErrorCode.MissingRequiredSegment,
                    $"Required segment '{seg.SegmentId}' is missing from the transaction."));
        }

        return new X12ValidationResult(errors);
    }

    /// <summary>
    /// Validates every transaction in <paramref name="interchange"/> against the matching
    /// schema in <paramref name="registry"/>. Transactions whose set ID has no registered
    /// schema are skipped. Returns all errors across all transactions.
    /// </summary>
    public static IReadOnlyList<X12ValidationError> ValidateInterchange(
        X12Interchange interchange, X12SchemaRegistry registry)
    {
        var errors = new List<X12ValidationError>();
        foreach (var group in interchange.FunctionalGroups)
        {
            foreach (var tx in group.Transactions)
            {
                var schema = registry.Get(tx.ST[1]);
                if (schema is null) continue;
                var result = Validate(tx.ToEdi(interchange.Delimiters), schema);
                errors.AddRange(result.Errors);
            }
        }
        return errors;
    }
}
