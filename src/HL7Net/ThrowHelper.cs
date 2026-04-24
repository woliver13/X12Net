namespace woliver13.HL7Net.IO;

// Backport of ArgumentNullException.ThrowIfNull for netstandard2.0 builds.
internal static class ThrowHelper
{
    internal static void ThrowIfNull(object? argument, string paramName)
    {
        if (argument is null)
            throw new ArgumentNullException(paramName);
    }
}
