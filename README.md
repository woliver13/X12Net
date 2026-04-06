# X12Net

A modern, high-performance C# library for parsing, generating, and editing EDI X12 documents.

- **Zero dependencies** — no third-party runtime packages required for the core library
- **Multi-target** — ships `net8.0` and `netstandard2.0` assemblies
- **Streaming-first** — `X12Reader` yields segments lazily; optional segment cap guards memory
- **Hierarchical DOM** — `X12Interchange` → `X12FunctionalGroup` → `X12Transaction`
- **Mutable DOM** — `X12Document` lets you parse, edit, and re-serialize in place
- **Fluent builder** — `X12InterchangeBuilder` constructs standards-compliant envelopes
- **Structural validation** — six built-in rules; extensible with FluentValidation
- **Source generator** — compile-time typed classes for 999, 835, 837P/I/D, 270/271, 834, 276/277

---

## Installation

```
dotnet add package X12Net
```

---

## Quick start

```csharp
using X12Net.IO;

using var reader = new X12Reader(rawEdiText);
foreach (var segment in reader.ReadAllSegments())
    Console.WriteLine($"{segment.SegmentId}: {string.Join(" | ", segment.Elements)}");
```

---

## Working with 999 Functional Acknowledgments

The X12 999 transaction set is the acknowledgment sent in response to an 8XX submission. The key segments are:

| Segment | Purpose |
|---------|---------|
| `AK1` | Opens the acknowledgment; identifies the functional group being acknowledged |
| `AK2` | Opens acknowledgment for one transaction set |
| `AK3` | Reports a segment-level error (optional) |
| `AK4` | Reports an element-level error within AK3 (optional) |
| `AK5` | Closes the transaction acknowledgment; carries the accept/reject code |
| `AK9` | Closes the group acknowledgment; carries the group accept/reject code |

`AK5-01` and `AK9-01` codes:

| Code | Meaning |
|------|---------|
| `A`  | Accepted |
| `E`  | Accepted with errors |
| `R`  | Rejected |

### Sample documents

**Accepted 999**

```
ISA*00*          *00*          *ZZ*CLEARINGHOUSE   *ZZ*PROVIDER       *260101*0900*^*00501*000000001*0*P*:~
GS*FA*CLEARINGHOUSE*PROVIDER*20260101*0900*1*X*005010X231A1~
ST*999*0001~
AK1*HC*100*005010X222A2~
AK2*837*0001~
AK5*A~
AK9*A*1*1*1~
SE*5*0001~
GE*1*1~
IEA*1*000000001~
```

**Rejected 999** (bad segment in the original 837)

```
ISA*00*          *00*          *ZZ*CLEARINGHOUSE   *ZZ*PROVIDER       *260101*0900*^*00501*000000001*0*P*:~
GS*FA*CLEARINGHOUSE*PROVIDER*20260101*0900*1*X*005010X231A1~
ST*999*0001~
AK1*HC*100*005010X222A2~
AK2*837*0001~
AK3*CLM*7*2000B*8~
AK4*1**1*7~
AK5*R*5~
AK9*R*1*1*0~
SE*7*0001~
GE*1*1~
IEA*1*000000001~
```

---

## X12Reader — low-level segment streaming

`X12Reader` yields one `X12Segment` at a time. Delimiters are auto-detected from the ISA
header when one is present.

```csharp
using X12Net.IO;

const string edi =
    "ISA*00*          *00*          *ZZ*CLEARINGHOUSE   *ZZ*PROVIDER       *260101*0900*^*00501*000000001*0*P*:~" +
    "GS*FA*CLEARINGHOUSE*PROVIDER*20260101*0900*1*X*005010X231A1~" +
    "ST*999*0001~" +
    "AK1*HC*100*005010X222A2~" +
    "AK2*837*0001~" +
    "AK5*A~" +
    "AK9*A*1*1*1~" +
    "SE*5*0001~" +
    "GE*1*1~" +
    "IEA*1*000000001~";

using var reader = new X12Reader(edi);

foreach (var seg in reader.ReadAllSegments())
{
    switch (seg.SegmentId)
    {
        case "AK1":
            Console.WriteLine($"Acknowledging group {seg[2]} (version {seg[3]})");
            break;
        case "AK5":
            string txStatus = seg[1] switch { "A" => "Accepted", "R" => "Rejected", _ => seg[1] };
            Console.WriteLine($"Transaction status: {txStatus}");
            break;
        case "AK9":
            Console.WriteLine($"Group status: {seg[1]}  accepted={seg[4]}/{seg[3]}");
            break;
    }
}
// Acknowledging group 100 (version 005010X222A2)
// Transaction status: Accepted
// Group status: A  accepted=1/1
```

Elements use **1-based indexing** matching X12 field numbering: `seg[1]` is the first element
after the segment ID, `seg[2]` the second, and so on.

### Async streaming

```csharp
using var reader = new X12Reader(edi);

await foreach (var seg in reader.ReadAllSegmentsAsync())
    Console.WriteLine(seg.SegmentId);
```

### Memory cap

Pass `maxSegments` to prevent runaway allocations when processing untrusted input:

```csharp
try
{
    using var reader = new X12Reader(edi, maxSegments: 500);
    var segments = reader.ReadAllSegments().ToList();
}
catch (X12MemoryCapException ex)
{
    Console.WriteLine($"Interchange exceeded cap of {ex.MaxSegments} segments.");
}
```

### Explicit delimiters

For non-ISA input or non-standard delimiters, pass them directly:

```csharp
var delimiters = new X12Delimiters('|', '^', '\n');
using var reader = new X12Reader(rawText, delimiters);
```

---

## X12Writer — building segments

`X12Writer` accumulates segments and serializes them to a string. It does not manage envelopes;
use `X12InterchangeBuilder` when you need a complete ISA/IEA interchange.

```csharp
using X12Net.IO;

var writer = new X12Writer();

// 999 acceptance transaction body
writer.WriteSegment("ST",  "999", "0001");
writer.WriteSegment("AK1", "HC", "100", "005010X222A2");
writer.WriteSegment("AK2", "837", "0001");
writer.WriteSegment("AK5", "A");
writer.WriteSegment("AK9", "A", "1", "1", "1");
writer.WriteSegment("SE",  "5", "0001");

Console.WriteLine(writer.ToString());
// ST*999*0001~AK1*HC*100*005010X222A2~AK2*837*0001~AK5*A~AK9*A*1*1*1~SE*5*0001~
```

**Rejection** with segment and element error detail:

```csharp
var writer = new X12Writer();
writer.WriteSegment("ST",  "999", "0001");
writer.WriteSegment("AK1", "HC", "100", "005010X222A2");
writer.WriteSegment("AK2", "837", "0001");
writer.WriteSegment("AK3", "CLM", "7", "2000B", "8");  // segment error in loop 2000B, error code 8
writer.WriteSegment("AK4", "1", "", "1", "7");          // element position 1, error code 7
writer.WriteSegment("AK5", "R", "5");                   // rejected, error code 5
writer.WriteSegment("AK9", "R", "1", "1", "0");         // group rejected, 0 accepted
writer.WriteSegment("SE",  "7", "0001");
```

---

## X12InterchangeBuilder — complete envelope generation

`X12InterchangeBuilder` constructs a fully standards-compliant ISA/IEA interchange including
proper fixed-width field padding, control number alignment, and GS/GE pairing.

```csharp
using X12Net.Envelopes;

// ── Accepted 999 ──────────────────────────────────────────────────────────────
string accepted999 = new X12InterchangeBuilder(
        senderId:                 "CLEARINGHOUSE",
        receiverId:               "PROVIDER",
        date:                     "260101",
        time:                     "0900",
        interchangeControlNumber: 1)
    .BeginFunctionalGroup(
        functionCode:       "FA",
        senderId:           "CLEARINGHOUSE",
        receiverId:         "PROVIDER",
        date:               "20260101",
        version:            "005010X231A1",
        groupControlNumber: 1)
    .AddRawSegment("ST*999*0001")
    .AddRawSegment("AK1*HC*100*005010X222A2")
    .AddRawSegment("AK2*837*0001")
    .AddRawSegment("AK5*A")
    .AddRawSegment("AK9*A*1*1*1")
    .AddRawSegment("SE*5*0001")
    .EndFunctionalGroup()
    .Build();

// ── Rejected 999 ──────────────────────────────────────────────────────────────
string rejected999 = new X12InterchangeBuilder(
        senderId:                 "CLEARINGHOUSE",
        receiverId:               "PROVIDER",
        date:                     "260101",
        time:                     "0900",
        interchangeControlNumber: 2)
    .BeginFunctionalGroup(
        functionCode:       "FA",
        senderId:           "CLEARINGHOUSE",
        receiverId:         "PROVIDER",
        date:               "20260101",
        version:            "005010X231A1",
        groupControlNumber: 1)
    .AddRawSegment("ST*999*0001")
    .AddRawSegment("AK1*HC*100*005010X222A2")
    .AddRawSegment("AK2*837*0001")
    .AddRawSegment("AK3*CLM*7*2000B*8")
    .AddRawSegment("AK4*1**1*7")
    .AddRawSegment("AK5*R*5")
    .AddRawSegment("AK9*R*1*1*0")
    .AddRawSegment("SE*7*0001")
    .EndFunctionalGroup()
    .Build();
```

---

## Hierarchical DOM — X12Interchange

`X12Interchange.Parse` builds the full ISA → GS → ST tree in one call. Segments are
read-only; use `X12Document` when you need to edit values.

```csharp
using X12Net.DOM;

var interchange = X12Interchange.Parse(edi);

// Envelope fields (1-based element access)
Console.WriteLine($"Sender:   {interchange.ISA[6].Trim()}");
Console.WriteLine($"Receiver: {interchange.ISA[8].Trim()}");

foreach (var group in interchange.FunctionalGroups)
{
    Console.WriteLine($"  Group {group.GS[6]} — function {group.GS[1]}");

    foreach (var tx in group.Transactions)
    {
        Console.WriteLine($"    Transaction {tx.ST[1]} / control {tx.ST[2]}");

        // Group-level acknowledgment
        var ak9 = tx.Segments.FirstOrDefault(s => s.SegmentId == "AK9");
        if (ak9 is not null)
        {
            string groupStatus = ak9[1] switch
            {
                "A" => "Accepted",
                "E" => "Accepted with errors",
                "R" => "Rejected",
                _   => ak9[1]
            };
            Console.WriteLine(
                $"      Group ack: {groupStatus}  " +
                $"({ak9[4]} of {ak9[3]} transactions accepted)");
        }

        // Per-transaction acknowledgments
        foreach (var ak5 in tx.Segments.Where(s => s.SegmentId == "AK5"))
        {
            bool ok = ak5[1] is "A" or "E";
            Console.WriteLine($"      Transaction ack: {(ok ? "OK" : "REJECTED")}");

            // Segment errors
            foreach (var ak3 in tx.Segments.Where(s => s.SegmentId == "AK3"))
                Console.WriteLine(
                    $"        Bad segment: {ak3[1]} at position {ak3[2]}, " +
                    $"loop {ak3[3]}, error code {ak3[4]}");
        }
    }
}
```

---

## Mutable DOM — X12Document

`X12Document` parses an interchange into editable segments and round-trips back to EDI text
via `ToString()`.

```csharp
using X12Net.DOM;

// Parse
var doc = X12Document.Parse(edi);

// Read a value — doc["SegmentId", elementIndex] (1-based)
string groupAckCode = doc["AK9", 1];  // "A" or "R"

// Edit in place
doc["AK1", 2] = "100";   // fix group control number

// Serialize back
string updated = doc.ToString();
```

**Iterating all AK5 segments** in a multi-transaction 999:

```csharp
foreach (var ak5 in doc.AllSegments("AK5"))
{
    Console.WriteLine($"Transaction result: {ak5[1]}");
    if (ak5.ElementCount >= 2)
        Console.WriteLine($"  Error code: {ak5[2]}");
}
```

**Flipping an acceptance to a rejection** and re-serializing:

```csharp
var doc = X12Document.Parse(accepted999Edi);

// Transaction level
var ak5 = doc.AllSegments("AK5").First();
ak5[1] = "R";
ak5[2] = "5";   // error code 5: "Transmission (ISA) Control Number Mismatch"

// Group level
doc["AK9", 1] = "R";   // group rejected
doc["AK9", 4] = "0";   // zero accepted

string rejected = doc.ToString();
```

---

## Structural validation

`X12Validator` checks six structural rules with no external dependencies.

```csharp
using X12Net.Validation;

var result = X12Validator.Validate(edi);

if (result.IsValid)
{
    Console.WriteLine("Interchange is structurally valid.");
}
else
{
    foreach (var error in result.Errors)
        Console.WriteLine($"[{error.Code}] {error.Message}");
}
```

Built-in rules:

| Error code | What is checked |
|------------|----------------|
| `MissingRequiredSegment` | ISA and IEA must both be present |
| `IsaSenderIdTooLong` | ISA06 sender ID must be 15 characters or fewer |
| `ControlNumberMismatch` | ISA13 must equal IEA02 |
| `GroupControlNumberMismatch` | GS06 must equal GE02 for each GS/GE pair |
| `IeaGroupCountMismatch` | IEA01 must equal the number of GS segments present |
| `SeSegmentCountMismatch` | SE01 must equal the segment count from ST through SE inclusive |

---

## Schema-driven access

Define a schema once, then access elements by name instead of by index.

```csharp
using X12Net.Schema;

var schema = new X12TransactionSchema("999", "Functional Acknowledgment",
    new X12SegmentSchema("AK1", new[] { "FunctionalIdentifierCode", "GroupControlNumber", "Version" }),
    new X12SegmentSchema("AK5", new[] { "TransactionSetAckCode", "SyntaxErrorCode" }),
    new X12SegmentSchema("AK9", new[] { "AckCode", "NumberIncluded", "NumberReceived", "NumberAccepted" }));

var tx = X12DynamicTransaction.Parse(edi, schema);

Console.WriteLine(tx["AK1", "GroupControlNumber"]);  // "100"
Console.WriteLine(tx["AK9", "AckCode"]);             // "A" or "R"
Console.WriteLine(tx["AK9", "NumberAccepted"]);      // "1" or "0"
```

### Schema inheritance

Derive a specialised schema from a base to avoid repeating shared segments:

```csharp
var base999 = new X12TransactionSchema("999", "Base 999",
    new X12SegmentSchema("AK1", new[] { "FunctionalIdentifierCode", "GroupControlNumber", "Version" }),
    new X12SegmentSchema("AK9", new[] { "AckCode", "NumberIncluded", "NumberReceived", "NumberAccepted" }));

// Derived schema adds AK3/AK5 error detail
var extended999 = base999.Extend("999ext", "999 with error detail",
    new X12SegmentSchema("AK3", new[] { "SegmentIdCode", "SegmentPosition", "LoopId", "SyntaxErrorCode" }),
    new X12SegmentSchema("AK5", new[] { "TransactionSetAckCode", "SyntaxErrorCode" }));
```

### Schema registry

```csharp
var registry = new X12SchemaRegistry();
registry.Register(base999);

var schema = registry.Get("999");
```

---

## Source-generated transaction types

The included Roslyn incremental source generator emits compile-time typed wrappers for all
built-in transaction sets. Types appear automatically when the source generator package is
referenced — no configuration needed.

```csharp
using X12Net;

var ts = new Ts999(edi);
Console.WriteLine(ts.AK1.FunctionalIdentifierCode);  // "HC"
Console.WriteLine(ts.AK1.GroupControlNumber);         // "100"
```

Generated transaction types: `Ts999`, `Ts835`, `Ts837P`, `Ts837I`, `Ts837D`,
`Ts270`, `Ts271`, `Ts834`, `Ts276`, `Ts277`.

---

## X12Tool CLI

A reference command-line tool ships in `tools/X12Tool`:

```
dotnet run --project tools/X12Tool -- parse    <file>
dotnet run --project tools/X12Tool -- validate <file>
dotnet run --project tools/X12Tool -- edit     <file> <segmentId> <elementIndex> <newValue>
```

---

## Running the benchmarks

```
dotnet run --project benchmarks/X12Net.Benchmarks -c Release -- --filter *
```

---

## Running the tests

```
dotnet test
```

---

## License

MIT — Copyright © 2026 Bill Oliver
