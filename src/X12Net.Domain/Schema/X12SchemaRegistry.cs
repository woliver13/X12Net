namespace woliver13.X12Net.Schema;

/// <summary>
/// A registry of <see cref="X12TransactionSchema"/> instances.
/// Supports user-defined custom schemas alongside the built-in ones.
/// </summary>
public sealed class X12SchemaRegistry
{
    private readonly Dictionary<string, X12TransactionSchema> _schemas =
        new(StringComparer.OrdinalIgnoreCase);

    private bool _frozen;

    /// <summary>Whether the registry has been frozen and accepts no further registrations.</summary>
    public bool IsReadOnly => _frozen;

    /// <summary>Prevents any further schema registrations.</summary>
    public void Freeze() => _frozen = true;

    /// <summary>Registers a transaction schema. Throws if the ID is already registered or the registry is frozen.</summary>
    /// <exception cref="InvalidOperationException">A schema with the same transaction set ID is already registered, or the registry is frozen.</exception>
    public void Register(X12TransactionSchema schema)
    {
        if (_frozen)
            throw new InvalidOperationException("The registry is frozen and cannot accept new schemas.");
        if (!TryRegister(schema))
            throw new InvalidOperationException(
                $"A schema for transaction set '{schema.TransactionSetId}' is already registered.");
    }

    /// <summary>
    /// Returns the schema for <paramref name="transactionSetId"/>,
    /// or <c>null</c> if none has been registered.
    /// </summary>
    public X12TransactionSchema? Get(string transactionSetId) =>
        _schemas.TryGetValue(transactionSetId, out var s) ? s : null;

    /// <summary>
    /// Attempts to register a schema. Returns <c>true</c> if registered;
    /// <c>false</c> if a schema with the same ID already exists (no overwrite).
    /// </summary>
    public bool TryRegister(X12TransactionSchema schema)
    {
        if (_frozen || _schemas.ContainsKey(schema.TransactionSetId))
            return false;
        _schemas[schema.TransactionSetId] = schema;
        return true;
    }

    /// <summary>All registered schemas.</summary>
    public IReadOnlyCollection<X12TransactionSchema> All => _schemas.Values;
}
