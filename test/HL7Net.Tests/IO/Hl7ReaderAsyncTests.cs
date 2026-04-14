using woliver13.HL7Net.IO;
using Shouldly;
using Xunit;

namespace HL7Net.Tests.IO;

public class Hl7ReaderAsyncTests
{
    private const string SimpleMessage =
        "MSH|^~\\&|SendApp|SendFac|RecvApp|RecvFac|20230101120000||ADT^A01|MSG001|P|2.5\r" +
        "EVN|A01|20230101120000\r" +
        "PID|1||12345^^^MRN||Doe^John^A||19800101|M\r";

    [Fact]
    public async Task ReadAllSegmentsAsync_YieldsThreeSegments()
    {
        var reader = new Hl7Reader(SimpleMessage);
        var segments = new List<woliver13.HL7Net.Core.Hl7Segment>();
        await foreach (var seg in reader.ReadAllSegmentsAsync())
            segments.Add(seg);
        segments.Count.ShouldBe(3);
    }

    [Fact]
    public async Task ReadAllSegmentsAsync_SegmentIds_MatchSyncVersion()
    {
        var reader = new Hl7Reader(SimpleMessage);
        var asyncIds = new List<string>();
        await foreach (var seg in reader.ReadAllSegmentsAsync())
            asyncIds.Add(seg.SegmentId);

        var syncIds = new Hl7Reader(SimpleMessage).ReadAllSegments().Select(s => s.SegmentId).ToList();
        asyncIds.ShouldBe(syncIds);
    }

    [Fact]
    public async Task ReadAllSegmentsAsync_ThrowsWhenCapExceeded()
    {
        var reader = new Hl7Reader(SimpleMessage, maxSegments: 2);
        await Should.ThrowAsync<Hl7MemoryCapException>(async () =>
        {
            await foreach (var seg in reader.ReadAllSegmentsAsync())
            {
                // consume
            }
        });
    }
}
