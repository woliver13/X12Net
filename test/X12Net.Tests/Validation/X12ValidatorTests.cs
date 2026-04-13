using woliver13.X12Net.Validation;

namespace woliver13.X12Net.Tests.Validation;

public class X12ValidatorTests
{
    private const string GE01MismatchInterchange =
        "ISA*00*          *00*          *ZZ*SENDER         *ZZ*RECEIVER       *201909*1200*^*00501*000000001*0*P*:~" +
        "GS*FA*SENDER*RECEIVER*20190901*1200*1*X*005010X231A1~" +
        "ST*999*0001~" +
        "AK1*FA*1*005010X231A1~" +
        "AK9*A*1*1*1~" +
        "SE*4*0001~" +
        "GE*2*1~" +   // GE01 says 2 transactions but there's only 1
        "IEA*1*000000001~";

    // ── Cycle 2 (Phase 6) ─────────────────────────────────────────────────

    [Fact]
    public void Validator_reports_ge01_transaction_count_mismatch()
    {
        var result = X12Validator.Validate(GE01MismatchInterchange);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == X12ErrorCode.GeTransactionCountMismatch);
    }


    private const string ValidInterchange =
        "ISA*00*          *00*          *ZZ*SENDER         *ZZ*RECEIVER       *201909*1200*^*00501*000000001*0*P*:~" +
        "GS*FA*SENDER*RECEIVER*20190901*1200*1*X*005010X231A1~" +
        "ST*999*0001~" +
        "AK1*FA*1*005010X231A1~" +
        "AK9*A*1*1*1~" +
        "SE*4*0001~" +
        "GE*1*1~" +
        "IEA*1*000000001~";

    // ── Cycle 1 ───────────────────────────────────────────────────────────

    [Fact]
    public void Validator_reports_no_errors_for_valid_interchange()
    {
        var result = X12Validator.Validate(ValidInterchange);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    // ── Cycle 2 ───────────────────────────────────────────────────────────

    [Fact]
    public void Validator_reports_error_when_ISA_sender_id_exceeds_15_chars()
    {
        // Build a non-standard ISA with a 16-char sender ID using X12Writer.
        // The new element-counting auto-detection handles non-106-char ISAs correctly.
        var w = new X12Net.IO.X12Writer();
        w.WriteSegment("ISA", "00", "          ", "00", "          ",
            "ZZ", "SENDER_TOO_LONG_",   // 16 chars — ISA06
            "ZZ", "RECEIVER       ",
            "201909", "1200", "^", "00501", "000000001", "0", "P", ":");
        w.WriteSegment("GS",  "FA", "SENDER", "RECEIVER", "20190901", "1200", "1", "X", "005010X231A1");
        w.WriteSegment("ST",  "999", "0001");
        w.WriteSegment("AK1", "FA", "1", "005010X231A1");
        w.WriteSegment("AK9", "A", "1", "1", "1");
        w.WriteSegment("SE",  "4", "0001");
        w.WriteSegment("GE",  "1", "1");
        w.WriteSegment("IEA", "1", "000000001");
        string bad = w.ToString();

        var result = X12Validator.Validate(bad);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == X12ErrorCode.IsaSenderIdTooLong);
    }

    // ── Cycle 3 ───────────────────────────────────────────────────────────

    [Fact]
    public void Validator_reports_error_when_ISA13_does_not_match_IEA02()
    {
        string bad = ValidInterchange.Replace("IEA*1*000000001~", "IEA*1*000000099~");

        var result = X12Validator.Validate(bad);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == X12ErrorCode.ControlNumberMismatch);
    }

    // ── Cycle 4 ───────────────────────────────────────────────────────────

    [Fact]
    public void Validator_reports_error_when_SE01_count_does_not_match_body_segment_count()
    {
        // SE01 = 3 (correct) — corrupt it to 99
        string bad = ValidInterchange.Replace("SE*4*0001~", "SE*99*0001~");

        var result = X12Validator.Validate(bad);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == X12ErrorCode.SeSegmentCountMismatch);
    }

    // ── Cycle 4 (Phase 4) ─────────────────────────────────────────────────

    [Fact]
    public void Validator_reports_error_for_GS_GE_control_number_mismatch()
    {
        // GS06 = "1", GE02 should also be "1"; corrupt GE02 to "99"
        string bad = ValidInterchange.Replace("GE*1*1~", "GE*1*99~");

        var result = X12Validator.Validate(bad);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == X12ErrorCode.GroupControlNumberMismatch);
    }

    // ── Cycle 5 ───────────────────────────────────────────────────────────

    [Fact]
    public void Validator_reports_error_when_required_segment_is_missing()
    {
        // Remove the IEA segment entirely
        string bad = ValidInterchange.Replace("IEA*1*000000001~", "");

        var result = X12Validator.Validate(bad);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == X12ErrorCode.MissingRequiredSegment);
    }

    // ── Cycle 1 (Issue 3) ─────────────────────────────────────────────────

    [Fact]
    public void Validate_with_extra_rules_runs_custom_rule_alongside_defaults()
    {
        X12ValidationRule noAk1 = (segs, errs) =>
        {
            if (segs.Any(s => s.SegmentId == "AK1"))
                errs.Add(new X12ValidationError(X12ErrorCode.MissingRequiredSegment, "AK1 is forbidden"));
        };

        var result = X12Validator.Validate(ValidInterchange, new[] { noAk1 });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Message.Contains("AK1 is forbidden"));
    }

    // ── Issue 6 — non-numeric control fields ─────────────────────────────

    [Fact]
    public void Validate_returns_MalformedControlField_when_IEA01_is_non_numeric()
    {
        string bad = ValidInterchange.Replace("IEA*1*", "IEA*ABC*");

        var result = X12Validator.Validate(bad);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == X12ErrorCode.MalformedControlField);
    }

    [Fact]
    public void Validate_returns_MalformedControlField_when_GE01_is_non_numeric()
    {
        string bad = ValidInterchange.Replace("GE*1*1~", "GE*ABC*1~");

        var result = X12Validator.Validate(bad);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == X12ErrorCode.MalformedControlField);
    }

    [Fact]
    public void Validate_returns_MalformedControlField_when_SE01_is_non_numeric()
    {
        string bad = ValidInterchange.Replace("SE*4*0001~", "SE*ABC*0001~");

        var result = X12Validator.Validate(bad);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == X12ErrorCode.MalformedControlField);
    }

    // ── Cycle 2 (Issue 3) ─────────────────────────────────────────────────

    [Fact]
    public void DefaultRules_contains_all_built_in_rules()
    {
        Assert.Equal(7, X12Validator.DefaultRules.Count);
    }

    [Fact]
    public void Validate_with_only_custom_rules_skips_built_in_checks()
    {
        // An interchange missing IEA — normally fails built-in check
        string noIea = ValidInterchange.Replace("IEA*1*000000001~", "");

        // Running with no rules at all should return valid (nothing checked)
        var result = X12Validator.Validate(noIea, builtInRules: false);

        Assert.True(result.IsValid);
    }
}
