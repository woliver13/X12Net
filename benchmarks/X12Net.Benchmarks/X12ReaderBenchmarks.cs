using BenchmarkDotNet.Attributes;
using X12Net.IO;

namespace X12Net.Benchmarks;

[MemoryDiagnoser]
public class X12ReaderBenchmarks
{
    private string _interchange = null!;

    [GlobalSetup]
    public void Setup()
    {
        _interchange =
            "ISA*00*          *00*          *ZZ*SENDER         *ZZ*RECEIVER       *201909*1200*^*00501*000000001*0*P*:~" +
            "GS*FA*SENDER*RECEIVER*20190901*1200*1*X*005010X231A1~" +
            "ST*999*0001~" +
            "AK1*FA*1*005010X231A1~" +
            "AK9*A*1*1*1~" +
            "SE*4*0001~" +
            "GE*1*1~" +
            "IEA*1*000000001~";
    }

    [Benchmark]
    public int ReadAllSegments()
    {
        using var reader = new X12Reader(_interchange);
        int count = 0;
        foreach (var _ in reader.ReadAllSegments())
            count++;
        return count;
    }
}
