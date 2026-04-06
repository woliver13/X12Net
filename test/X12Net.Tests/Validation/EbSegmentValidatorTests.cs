using X12Net.IO;
using X12Net.Validation;

namespace X12Net.Tests.Validation;

public class EbSegmentValidatorTests
{
    // ── Helper ────────────────────────────────────────────────────────────

    /// <summary>Builds an EB segment from the supplied element values and validates it.</summary>
    private static X12ValidationResult ValidateEb(params string[] elements)
    {
        var w = new X12Writer();
        w.WriteSegment("EB", elements);
        return EbSegmentValidator.ValidateRaw(w.ToString());
    }

    // ── Cycle 1 — valid minimal EB ────────────────────────────────────────

    [Fact]
    public void Validator_returns_valid_for_minimal_EB_with_known_code()
    {
        var result = EbSegmentValidator.ValidateRaw("EB*1~");

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    // ── Cycle 2 — EB01 required ───────────────────────────────────────────

    [Fact]
    public void Validator_reports_error_when_EB01_is_empty()
    {
        var result = EbSegmentValidator.ValidateRaw("EB*~");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == X12ErrorCode.EbMissingEligibilityCode);
    }

    [Fact]
    public void Validator_reports_error_when_EB01_is_absent()
    {
        var result = EbSegmentValidator.ValidateRaw("EB~");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == X12ErrorCode.EbMissingEligibilityCode);
    }

    // ── Cycle 3 — EB01 code set ───────────────────────────────────────────

    [Fact]
    public void Validator_reports_error_when_EB01_has_invalid_code()
    {
        var result = EbSegmentValidator.ValidateRaw("EB*ZZ~");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == X12ErrorCode.EbInvalidEligibilityCode);
    }

    [Theory]
    [InlineData("1")] [InlineData("2")] [InlineData("3")] [InlineData("4")] [InlineData("5")]
    [InlineData("6")] [InlineData("7")] [InlineData("8")] [InlineData("9")]
    [InlineData("A")] [InlineData("B")] [InlineData("C")] [InlineData("CB")]
    [InlineData("D")] [InlineData("E")] [InlineData("F")] [InlineData("G")] [InlineData("H")]
    [InlineData("I")] [InlineData("J")] [InlineData("K")] [InlineData("L")] [InlineData("M")]
    [InlineData("MC")] [InlineData("N")] [InlineData("O")] [InlineData("P")] [InlineData("Q")]
    [InlineData("R")] [InlineData("S")] [InlineData("T")] [InlineData("U")] [InlineData("V")]
    [InlineData("W")] [InlineData("X")]
    public void Validator_accepts_all_valid_EB01_codes(string code)
    {
        var result = EbSegmentValidator.ValidateRaw($"EB*{code}~");

        Assert.True(result.IsValid);
    }

    // ── Cycle 4 — EB02 coverage level code ───────────────────────────────

    [Fact]
    public void Validator_reports_error_when_EB02_has_invalid_coverage_level()
    {
        var result = EbSegmentValidator.ValidateRaw("EB*1*ZZZ~");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == X12ErrorCode.EbInvalidCoverageLevelCode);
    }

    [Theory]
    [InlineData("CHD")] [InlineData("DEP")] [InlineData("ECH")] [InlineData("EMP")]
    [InlineData("ESP")] [InlineData("FAM")] [InlineData("IND")] [InlineData("SPC")]
    [InlineData("TWO")]
    public void Validator_accepts_valid_EB02_coverage_level_codes(string code)
    {
        var result = EbSegmentValidator.ValidateRaw($"EB*1*{code}~");

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validator_accepts_EB02_when_absent()
    {
        var result = EbSegmentValidator.ValidateRaw("EB*1~");

        Assert.True(result.IsValid);
    }

    // ── Cycle 5 — EB03 composite service type code ────────────────────────

    [Fact]
    public void Validator_accepts_valid_single_component_EB03()
    {
        var result = EbSegmentValidator.ValidateRaw("EB*1**30~");

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validator_accepts_valid_multi_component_EB03()
    {
        // X12Reader stores composite "30:35:48" as a single element string joined by ':'
        var result = ValidateEb("1", "", "30:35:48");

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validator_reports_error_when_EB03_single_component_is_invalid()
    {
        var result = EbSegmentValidator.ValidateRaw("EB*1**ZZ~");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == X12ErrorCode.EbInvalidServiceTypeCode);
    }

    [Fact]
    public void Validator_reports_error_when_one_component_of_multi_EB03_is_invalid()
    {
        var result = ValidateEb("1", "", "30:ZZ:48");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == X12ErrorCode.EbInvalidServiceTypeCode);
    }

    // ── Cycle 6 — EB04 insurance type code ───────────────────────────────

    [Fact]
    public void Validator_reports_error_when_EB04_has_invalid_insurance_type()
    {
        var result = EbSegmentValidator.ValidateRaw("EB*1***ZZ~");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == X12ErrorCode.EbInvalidInsuranceTypeCode);
    }

    [Theory]
    [InlineData("AP")] [InlineData("C1")] [InlineData("CO")] [InlineData("HM")]
    [InlineData("MP")] [InlineData("OT")] [InlineData("PR")] [InlineData("WC")]
    public void Validator_accepts_valid_EB04_insurance_type_codes(string code)
    {
        var result = EbSegmentValidator.ValidateRaw($"EB*1***{code}~");

        Assert.True(result.IsValid);
    }

    // ── Cycle 7 — EB05 plan description length ────────────────────────────

    [Fact]
    public void Validator_reports_error_when_EB05_exceeds_50_chars()
    {
        var result = ValidateEb("1", "", "", "", new string('A', 51));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == X12ErrorCode.EbPlanDescriptionTooLong);
    }

    [Fact]
    public void Validator_accepts_EB05_at_exactly_50_chars()
    {
        var result = ValidateEb("1", "", "", "", new string('A', 50));

        Assert.True(result.IsValid);
    }

    // ── Cycle 8 — EB06 time period qualifier ─────────────────────────────

    [Fact]
    public void Validator_reports_error_when_EB06_has_invalid_time_period_qualifier()
    {
        // EB07 supplied so the EB06 relational rule is satisfied
        var result = ValidateEb("1", "", "", "", "", "ZZ", "100");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == X12ErrorCode.EbInvalidTimePeriodQualifier);
    }

    [Theory]
    [InlineData("6")]  [InlineData("7")]  [InlineData("13")] [InlineData("21")]
    [InlineData("27")] [InlineData("29")] [InlineData("30")] [InlineData("33")]
    [InlineData("34")] [InlineData("35")]
    public void Validator_accepts_valid_EB06_time_period_qualifiers(string code)
    {
        // EB07 = "100" so the relational rule is satisfied
        var result = ValidateEb("1", "", "", "", "", code, "100");

        Assert.True(result.IsValid);
    }

    // ── Cycle 9 — EB07 monetary amount ───────────────────────────────────

    [Fact]
    public void Validator_reports_error_when_EB07_is_negative()
    {
        var result = ValidateEb("1", "", "", "", "", "", "-100");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == X12ErrorCode.EbNegativeMonetaryAmount);
    }

    [Theory]
    [InlineData("0")] [InlineData("150.75")] [InlineData("0.01")]
    public void Validator_accepts_non_negative_EB07(string amount)
    {
        var result = ValidateEb("1", "", "", "", "", "", amount);

        Assert.True(result.IsValid);
    }

    // ── Cycle 10 — EB08 percent ───────────────────────────────────────────

    [Theory]
    [InlineData("-1")] [InlineData("100.01")] [InlineData("-0.01")]
    public void Validator_reports_error_when_EB08_is_out_of_range(string pct)
    {
        var result = ValidateEb("1", "", "", "", "", "", "", pct);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == X12ErrorCode.EbPercentOutOfRange);
    }

    [Theory]
    [InlineData("0")] [InlineData("100")] [InlineData("50.00")] [InlineData("100.00")]
    public void Validator_accepts_EB08_within_valid_range(string pct)
    {
        var result = ValidateEb("1", "", "", "", "", "", "", pct);

        Assert.True(result.IsValid);
    }

    // ── Cycle 11 — EB09 quantity qualifier ───────────────────────────────

    [Fact]
    public void Validator_reports_error_when_EB09_has_invalid_qualifier()
    {
        // EB09 + EB10 both present so the pairing rule is satisfied
        var result = ValidateEb("1", "", "", "", "", "", "", "", "ZZ", "5");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == X12ErrorCode.EbInvalidQuantityQualifier);
    }

    [Theory]
    [InlineData("CA")] [InlineData("LA")] [InlineData("LE")] [InlineData("NE")] [InlineData("VS")]
    public void Validator_accepts_valid_EB09_quantity_qualifiers(string code)
    {
        var result = ValidateEb("1", "", "", "", "", "", "", "", code, "5");

        Assert.True(result.IsValid);
    }

    // ── Cycle 12 — EB10 quantity ──────────────────────────────────────────

    [Fact]
    public void Validator_reports_error_when_EB10_is_negative()
    {
        // EB09 + EB10 both present to satisfy pairing rule
        var result = ValidateEb("1", "", "", "", "", "", "", "", "VS", "-5");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == X12ErrorCode.EbNegativeQuantity);
    }

    [Fact]
    public void Validator_accepts_EB10_of_zero()
    {
        var result = ValidateEb("1", "", "", "", "", "", "", "", "VS", "0");

        Assert.True(result.IsValid);
    }

    // ── Cycle 13 — EB11 authorization indicator ───────────────────────────

    [Fact]
    public void Validator_reports_error_when_EB11_is_invalid()
    {
        var result = ValidateEb("1", "", "", "", "", "", "", "", "", "", "X");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == X12ErrorCode.EbInvalidAuthorizationIndicator);
    }

    [Theory]
    [InlineData("N")] [InlineData("Y")] [InlineData("U")]
    public void Validator_accepts_valid_EB11_values(string code)
    {
        var result = ValidateEb("1", "", "", "", "", "", "", "", "", "", code);

        Assert.True(result.IsValid);
    }

    // ── Cycle 14 — EB12 in-plan network indicator ─────────────────────────

    [Fact]
    public void Validator_reports_error_when_EB12_is_invalid()
    {
        var result = ValidateEb("1", "", "", "", "", "", "", "", "", "", "", "X");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == X12ErrorCode.EbInvalidInPlanNetworkIndicator);
    }

    [Theory]
    [InlineData("N")] [InlineData("U")] [InlineData("W")] [InlineData("Y")]
    public void Validator_accepts_valid_EB12_values(string code)
    {
        var result = ValidateEb("1", "", "", "", "", "", "", "", "", "", "", code);

        Assert.True(result.IsValid);
    }

    // ── Cycle 15 — EB09/EB10 pairing rule ────────────────────────────────

    [Fact]
    public void Validator_reports_error_when_EB09_present_but_EB10_absent()
    {
        var result = ValidateEb("1", "", "", "", "", "", "", "", "VS", "");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == X12ErrorCode.EbQuantityQualifierWithoutQuantity);
    }

    [Fact]
    public void Validator_reports_error_when_EB10_present_but_EB09_absent()
    {
        var result = ValidateEb("1", "", "", "", "", "", "", "", "", "5");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == X12ErrorCode.EbQuantityQualifierWithoutQuantity);
    }

    [Fact]
    public void Validator_accepts_EB09_and_EB10_both_present()
    {
        var result = ValidateEb("1", "", "", "", "", "", "", "", "VS", "5");

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validator_accepts_neither_EB09_nor_EB10()
    {
        var result = EbSegmentValidator.ValidateRaw("EB*1~");

        Assert.True(result.IsValid);
    }

    // ── Cycle 16 — EB06 relational rule ──────────────────────────────────

    [Fact]
    public void Validator_reports_error_when_EB06_present_without_any_amount_or_quantity()
    {
        var result = ValidateEb("1", "", "", "", "", "29");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == X12ErrorCode.EbTimePeriodRequiresAmount);
    }

    [Fact]
    public void Validator_accepts_EB06_when_EB07_monetary_amount_is_present()
    {
        var result = ValidateEb("1", "", "", "", "", "29", "500");

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validator_accepts_EB06_when_EB08_percent_is_present()
    {
        var result = ValidateEb("1", "", "", "", "", "29", "", "20");

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validator_accepts_EB06_when_EB10_quantity_is_present()
    {
        // EB09 + EB10 both supplied to satisfy pairing rule
        var result = ValidateEb("1", "", "", "", "", "29", "", "", "VS", "3");

        Assert.True(result.IsValid);
    }

    // ── Cycle 17 — fully populated valid segment ──────────────────────────

    [Fact]
    public void Validator_returns_valid_for_fully_populated_EB_segment()
    {
        // EB01=1 (Active Coverage)  EB02=FAM (Family)   EB03=30:35 (composite)
        // EB04=HM (HMO)             EB05=BLUE PLUS PPO  EB06=29 (Year)
        // EB07=500.00               EB08=20.00          EB09=VS (Visits)
        // EB10=10                   EB11=Y (Yes)        EB12=Y (In-network)
        var result = ValidateEb(
            "1", "FAM", "30:35", "HM", "BLUE PLUS PPO",
            "29", "500.00", "20.00", "VS", "10", "Y", "Y");

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }
}
