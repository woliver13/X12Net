using BenchmarkDotNet.Attributes;
using woliver13.X12Net.DOM;
using woliver13.X12Net.Envelopes;
using woliver13.X12Net.IO;
using woliver13.X12Net.Schema;
using woliver13.X12Net.Transactions;

namespace woliver13.X12Net.Benchmarks;

/// <summary>
/// Measures the throughput and allocation profile of each major parsing and
/// generation path in X12Net. Run with:
///   dotnet run -c Release --project benchmarks/X12Net.Benchmarks
/// </summary>
[MemoryDiagnoser]
public class X12ReaderBenchmarks
{
    // ── Fixtures ──────────────────────────────────────────────────────────

    private string _interchange999 = null!;
    private string _interchange271 = null!;
    private X12TransactionSchema _schema271 = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Small 999 — baseline streaming fixture (8 segments)
        _interchange999 =
            "ISA*00*          *00*          *ZZ*SENDER         *ZZ*RECEIVER       *201909*1200*^*00501*000000001*0*P*:~" +
            "GS*FA*SENDER*RECEIVER*20190901*1200*1*X*005010X231A1~" +
            "ST*999*0001~" +
            "AK1*FA*1*005010X231A1~" +
            "AK9*A*1*1*1~" +
            "SE*4*0001~" +
            "GE*1*1~" +
            "IEA*1*000000001~";

        // 271 with two EB segments — exercised by typed + schema-driven benchmarks
        _interchange271 =
            "ISA*00*          *00*          *ZZ*RECEIVER       *ZZ*SUBMITTER      *260407*1200*^*00501*000000007*0*P*:~" +
            "GS*HB*RECEIVER*SUBMITTER*20260407*1200*7*X*005010X279A1~" +
            "ST*271*0001~" +
            "BHT*0022*11*10001234*20260407*1200~" +
            "EB*1*FAM*30*MC**1~" +
            "EB*C*IND*30*MC**1~" +
            "SE*6*0001~" +
            "GE*1*7~" +
            "IEA*1*000000007~";

        // Schema for schema-driven benchmark
        _schema271 = new X12TransactionSchema("271", "Eligibility Response",
            new X12SegmentSchema("BHT", new[] { "PurposeCode", "TransactionTypeCode", "ReferenceId", "Date", "Time" }),
            new X12SegmentSchema("EB",  new[] { "BenefitInfoCode", "CoverageLevelCode", "ServiceTypeCode", "InsuranceTypeCode" }));
    }

    // ── 1. Raw streaming ─────────────────────────────────────────────────

    /// <summary>Baseline: stream all segments from a 999 — exercises tokenizer only.</summary>
    [Benchmark(Baseline = true)]
    public int ReadAllSegments()
    {
        using var reader = new X12Reader(_interchange999);
        int count = 0;
        foreach (var _ in reader.ReadAllSegments())
            count++;
        return count;
    }

    // ── 2. DOM parse ──────────────────────────────────────────────────────

    /// <summary>Parse a 999 into a full <see cref="X12Interchange"/> hierarchy.</summary>
    [Benchmark]
    public int DomParse_Interchange()
    {
        var interchange = X12Interchange.Parse(_interchange999);
        return interchange.FunctionalGroups.Count;
    }

    /// <summary>Parse a 271 into a mutable <see cref="X12Document"/>.</summary>
    [Benchmark]
    public int DomParse_Document()
    {
        var doc = X12Document.Parse(_interchange271);
        return doc.AllSegments("EB").Count();
    }

    // ── 3. Source-generated typed parse ──────────────────────────────────

    /// <summary>Parse a 999 via the source-generated <c>Ts999</c> typed wrapper.</summary>
    [Benchmark]
    public bool TypedParse_Ts999()
    {
        var ts = Ts999.Parse(_interchange999);
        return ts.AK1 is not null;
    }

    /// <summary>Parse a 271 via the source-generated <c>Ts271</c> typed wrapper.</summary>
    [Benchmark]
    public int TypedParse_Ts271()
    {
        var ts = Ts271.Parse(_interchange271);
        return ts.AllEB().Count();
    }

    // ── 4. Schema-driven access ───────────────────────────────────────────

    /// <summary>Parse a 271 using <see cref="X12DynamicTransaction"/> and read EB03 by name.</summary>
    [Benchmark]
    public string? SchemaDriven_271()
    {
        var tx = X12DynamicTransaction.Parse(_interchange271, _schema271);
        return tx["EB", "ServiceTypeCode"];
    }

    // ── 5. Writer / builder ───────────────────────────────────────────────

    /// <summary>Build a minimal 999-acceptance interchange via <see cref="X12InterchangeBuilder"/>.</summary>
    [Benchmark]
    public int Builder_Build999()
    {
        var edi = new X12InterchangeBuilder("SENDER", "RECEIVER", "260407", "1200")
            .BeginFunctionalGroup("FA", "SENDER", "RECEIVER", "20260407", "005010X231A1")
            .AddRawSegment("ST*999*0001")
            .AddRawSegment("AK1*HC*1*005010X222A2")
            .AddRawSegment("AK9*A*1*1*1")
            .AddRawSegment("SE*4*0001")
            .EndFunctionalGroup()
            .Build();
        return edi.Length;
    }
}
