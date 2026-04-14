#if NETSTANDARD2_0
// Polyfill required for C# 9 init-only setters on netstandard2.0.
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
#endif
