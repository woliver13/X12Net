namespace X12Net.Schema;

/// <summary>
/// A registry of <see cref="X12TransactionSchema"/> instances.
/// Supports user-defined custom schemas alongside the built-in ones.
/// </summary>
public sealed class X12SchemaRegistry
{
    private readonly Dictionary<string, X12TransactionSchema> _schemas =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Registers or replaces a transaction schema.</summary>
    public void Register(X12TransactionSchema schema) =>
        _schemas[schema.TransactionSetId] = schema;

    /// <summary>
    /// Returns the schema for <paramref name="transactionSetId"/>,
    /// or <c>null</c> if none has been registered.
    /// </summary>
    public X12TransactionSchema? Get(string transactionSetId) =>
        _schemas.TryGetValue(transactionSetId, out var s) ? s : null;

    /// <summary>All registered schemas.</summary>
    public IReadOnlyCollection<X12TransactionSchema> All => _schemas.Values;
}
