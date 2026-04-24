using woliver13.HL7Net.Core;
using woliver13.HL7Net.DOM;
using Shouldly;
using Xunit;

namespace HL7Net.Tests.Core;

public class Hl7RepetitionTests
{
    // Standard HL7 delimiters: field=|, component=^, repetition=~, escape=\, sub=&
    private static readonly char RepSep = '~';

    // ── Cycle 1 / 2 ──────────────────────────────────────────────────────

    [Fact]
    public void Hl7Segment_GetRepetitions_splits_field_on_repetition_separator()
    {
        // PID-3 contains two patient identifiers separated by ~
        var segment = new Hl7Segment("PID",
            new[] { "", "", "12345^^^MRN~98765^^^EPI", "" });

        var reps = segment.GetRepetitions(3, RepSep);

        reps.Count.ShouldBe(2);
        reps[0].ShouldBe("12345^^^MRN");
        reps[1].ShouldBe("98765^^^EPI");
    }

    [Fact]
    public void Hl7Segment_GetRepetitions_returns_single_entry_when_no_repetition_present()
    {
        var segment = new Hl7Segment("NK1",
            new[] { "1", "SMITH^JOHN", "SPO" });

        var reps = segment.GetRepetitions(3, RepSep);

        reps.Count.ShouldBe(1);
        reps[0].ShouldBe("SPO");
    }

    // ── Cycle 3 ───────────────────────────────────────────────────────────

    [Fact]
    public void Hl7MutableSegment_GetRepetitions_splits_field_on_repetition_separator()
    {
        var segment = new Hl7MutableSegment("PID",
            new[] { "", "", "12345^^^MRN~98765^^^EPI" });

        var reps = segment.GetRepetitions(3, RepSep);

        reps.Count.ShouldBe(2);
        reps[0].ShouldBe("12345^^^MRN");
        reps[1].ShouldBe("98765^^^EPI");
    }
}
