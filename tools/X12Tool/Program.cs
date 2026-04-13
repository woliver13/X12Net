using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using woliver13.X12Net.CLI;

// x12tool <command> [options]
//   parse    <edi-text>                          — list segment IDs
//   validate <edi-text>                          — structural validation
//   edit     <edi-text> <seg> <elem-idx> <value> — edit one element

using var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((_, services) =>
    {
        services.AddApplication();
        services.AddInfrastructure();
    })
    .Build();

var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("x12tool");
var tool   = host.Services.GetRequiredService<IX12ToolService>();

try
{
    if (args.Length < 2)
    {
        logger.LogError("Usage: x12tool <parse|validate|edit> <edi-text> [segment element-index new-value]");
        return ExitCode.UsageError;
    }

    string command = args[0].ToLowerInvariant();
    string input   = args[1];

    logger.LogDebug("Dispatching command '{Command}'.", command);

    switch (command)
    {
        case "parse":
        {
            var result = tool.Parse(input);
            if (!result.Success) { logger.LogError("{Error}", result.Error); return ExitCode.UnexpectedError; }
            foreach (var id in result.SegmentIds)
                Console.WriteLine(id);
            return ExitCode.Success;
        }
        case "validate":
        {
            var result = tool.Validate(input);
            if (result.IsValid) { Console.WriteLine("OK"); return ExitCode.Success; }
            foreach (var err in result.Errors)
                logger.LogWarning("{ValidationError}", err);
            return ExitCode.UnexpectedError;
        }
        case "edit":
        {
            if (args.Length < 5)
            {
                logger.LogError("Usage: x12tool edit <edi-text> <segment-id> <element-index> <new-value>");
                return ExitCode.UsageError;
            }
            if (!int.TryParse(args[3], out int idx))
            {
                logger.LogError("element-index must be an integer.");
                return ExitCode.UsageError;
            }
            var result = tool.Edit(input, args[2], idx, args[4]);
            if (!result.Success) { logger.LogError("{Error}", result.Error); return ExitCode.UnexpectedError; }
            Console.Write(result.Output);
            return ExitCode.Success;
        }
        default:
            logger.LogError("Unknown command '{Command}'. Expected: parse, validate, edit.", command);
            return ExitCode.UsageError;
    }
}
catch (OperationCanceledException)
{
    return ExitCode.Success;
}
catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
{
    logger.LogCritical(ex, "Configuration or usage error: {Message}", ex.Message);
    return ExitCode.ConfigError;
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Unexpected error: {Message}", ex.Message);
    return ExitCode.UnexpectedError;
}
