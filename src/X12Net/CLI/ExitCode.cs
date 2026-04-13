namespace woliver13.X12Net.CLI;

public static class ExitCode
{
    public const int Success         = 0;   // EX_OK
    public const int UnexpectedError = 1;   // catch-all for unhandled exceptions
    public const int UsageError      = 2;   // bad arguments / startup validation failure
    public const int TempFailure     = 75;  // EX_TEMPFAIL — transient, retry may help
    public const int ConfigError     = 78;  // EX_CONFIG   — bad configuration
}
