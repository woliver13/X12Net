# X12Net

A modern, high-performance C# library for parsing, generating, and editing EDI X12 documents.

## Quick start

```csharp
using X12Net.IO;

using var reader = new X12Reader(rawEdiText);
foreach (var segment in reader.ReadAllSegments())
    Console.WriteLine(segment.SegmentId);
```

## Packages

| Package | Description |
|---------|-------------|
| `X12Net` | Core library — tokenizer, reader/writer, DOM, validation, schema |

## API reference

Browse the [API reference](api/index.md) for full type documentation.
