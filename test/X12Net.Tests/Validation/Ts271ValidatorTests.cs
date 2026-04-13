using woliver13.X12Net.Validation;

namespace woliver13.X12Net.Tests.Validation;

public class Ts271ValidatorTests
{
    // ── Shared fixtures ───────────────────────────────────────────────────

    /// <summary>
    /// A structurally valid 271 with one valid EB segment.
    /// Segments: ST BHT EB SE → SE*4.
    /// </summary>
    private const string Valid271 =
        "ISA*00*          *00*          *ZZ*RECEIVER       *ZZ*SUBMITTER      *260407*1200*^*00501*000000007*0*P*:~" +
        "GS*HB*RECEIVER*SUBMITTER*20260407*1200*7*X*005010X279A1~" +
        "ST*271*0001~" +
        "BHT*0022*11*10001234*20260407*1200~" +
        "EB*1*FAM*30~" +
        "SE*4*0001~" +
        "GE*1*7~" +
        "IEA*1*000000007~";

    /// <summary>
    /// Structurally valid 271 with no EB segments.
    /// Segments: ST BHT SE → SE*3.
    /// </summary>
    private const string Valid271NoEb =
        "ISA*00*          *00*          *ZZ*RECEIVER       *ZZ*SUBMITTER      *260407*1200*^*00501*000000007*0*P*:~" +
        "GS*HB*RECEIVER*SUBMITTER*20260407*1200*7*X*005010X279A1~" +
        "ST*271*0001~" +
        "BHT*0022*11*10001234*20260407*1200~" +
        "SE*3*0001~" +
        "GE*1*7~" +
        "IEA*1*000000007~";

    /// <summary>
    /// Structurally valid envelope but EB01 contains an unrecognised code.
    /// </summary>
    private const string Valid271WithBadEb01 =
        "ISA*00*          *00*          *ZZ*RECEIVER       *ZZ*SUBMITTER      *260407*1200*^*00501*000000007*0*P*:~" +
        "GS*HB*RECEIVER*SUBMITTER*20260407*1200*7*X*005010X279A1~" +
        "ST*271*0001~" +
        "BHT*0022*11*10001234*20260407*1200~" +
        "EB*BADCODE*FAM*30~" +
        "SE*4*0001~" +
        "GE*1*7~" +
        "IEA*1*000000007~";

    /// <summary>
    /// ISA13 / IEA02 mismatch (structural, non-fatal) AND bad EB01 (semantic).
    /// Produces two distinct errors — one from each phase.
    /// </summary>
    private const string Mismatched271WithBadEb =
        "ISA*00*          *00*          *ZZ*RECEIVER       *ZZ*SUBMITTER      *260407*1200*^*00501*000000007*0*P*:~" +
        "GS*HB*RECEIVER*SUBMITTER*20260407*1200*7*X*005010X279A1~" +
        "ST*271*0001~" +
        "BHT*0022*11*10001234*20260407*1200~" +
        "EB*BADCODE*FAM*30~" +
        "SE*4*0001~" +
        "GE*1*7~" +
        "IEA*1*000000099~";   // IEA02 ≠ ISA13 → ControlNumberMismatch

    /// <summary>
    /// Two EB segments: first is valid, second has a bad EB01.
    /// Both should be validated independently.
    /// </summary>
    private const string Valid271TwoEbsOneInvalid =
        "ISA*00*          *00*          *ZZ*RECEIVER       *ZZ*SUBMITTER      *260407*1200*^*00501*000000007*0*P*:~" +
        "GS*HB*RECEIVER*SUBMITTER*20260407*1200*7*X*005010X279A1~" +
        "ST*271*0001~" +
        "BHT*0022*11*10001234*20260407*1200~" +
        "EB*1*FAM*30~" +
        "EB*BADCODE*IND*30~" +
        "SE*5*0001~" +
        "GE*1*7~" +
        "IEA*1*000000007~";

    // ── Cycle 1 — happy path ──────────────────────────────────────────────

    [Fact]
    public void Validate_returns_valid_for_structurally_correct_271_with_valid_EB()
    {
        var result = Ts271Validator.Validate(Valid271);

        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    // ── Cycle 2 — structural early-exit ───────────────────────────────────

    [Fact]
    public void Validate_returns_structural_error_and_skips_EB_check_when_ISA_is_missing()
    {
        // No envelope at all — X12Validator will fire MissingRequiredSegment.
        const string noEnvelope = "ST*271*0001~SE*2*0001~";

        var result = Ts271Validator.Validate(noEnvelope);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Code == X12ErrorCode.MissingRequiredSegment);
        // EB-specific codes must NOT appear — phase 2 must not run.
        result.Errors.ShouldNotContain(e => e.Code == X12ErrorCode.EbInvalidEligibilityCode);
    }

    // ── Cycle 3 — EB semantic errors surface ─────────────────────────────

    [Fact]
    public void Validate_returns_EB_error_when_EB01_is_invalid_and_structure_is_correct()
    {
        var result = Ts271Validator.Validate(Valid271WithBadEb01);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Code == X12ErrorCode.EbInvalidEligibilityCode);
        // No structural errors — only the EB problem.
        result.Errors.ShouldNotContain(e => e.Code == X12ErrorCode.ControlNumberMismatch);
    }

    // ── Cycle 4 — combined structural + EB errors ─────────────────────────

    [Fact]
    public void Validate_returns_both_structural_and_EB_errors_when_both_phases_fail()
    {
        var result = Ts271Validator.Validate(Mismatched271WithBadEb);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Code == X12ErrorCode.ControlNumberMismatch);
        result.Errors.ShouldContain(e => e.Code == X12ErrorCode.EbInvalidEligibilityCode);
    }

    // ── Cycle 5 — multiple EB segments each validated independently ───────

    [Fact]
    public void Validate_accumulates_errors_across_multiple_EB_segments()
    {
        var result = Ts271Validator.Validate(Valid271TwoEbsOneInvalid);

        result.IsValid.ShouldBeFalse();
        // Exactly one EB error — from the second EB, not the first.
        result.Errors.Where(e => e.Code == X12ErrorCode.EbInvalidEligibilityCode).ShouldHaveSingleItem();
    }

    // ── Cycle 6 — valid 271 with no EB segments ───────────────────────────

    [Fact]
    public void Validate_returns_valid_when_271_has_no_EB_segments()
    {
        var result = Ts271Validator.Validate(Valid271NoEb);

        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }
}
