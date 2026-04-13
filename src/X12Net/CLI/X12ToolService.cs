using Microsoft.Extensions.Logging;

namespace woliver13.X12Net.CLI;

/// <summary>
/// DI-friendly implementation of <see cref="IX12ToolService"/> that delegates to the
/// <see cref="X12Tool"/> static facade and uses an injected logger.
/// </summary>
public sealed class X12ToolService : IX12ToolService
{
    private readonly ILogger<X12ToolService> _logger;

    /// <summary>Initialises the service with the provided logger.</summary>
    public X12ToolService(ILogger<X12ToolService> logger) => _logger = logger;

    /// <inheritdoc/>
    public ParseResult Parse(string input) =>
        X12Tool.Parse(input, _logger);

    /// <inheritdoc/>
    public ValidateResult Validate(string input) =>
        X12Tool.Validate(input, _logger);

    /// <inheritdoc/>
    public EditResult Edit(string input, string segmentId, int elementIndex, string newValue) =>
        X12Tool.Edit(input, segmentId, elementIndex, newValue, _logger);
}
