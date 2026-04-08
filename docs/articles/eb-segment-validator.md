# EB Segment Validator — Implementation Plan

## Overview

Adds semantic validation for all elements of the X12 EB segment as it appears in a 271
(Health Care Eligibility/Benefit Response) transaction. Structural interchange validation
already exists in `X12Validator`; this plan adds a complementary `EbSegmentValidator`
for transaction-level element rules.

---

## What Gets Added

No existing files are modified except `X12ErrorCode.cs`. Two new files, 17 TDD cycles.

| File | Change |
|------|--------|
| `src/X12Net/Validation/X12ErrorCode.cs` | 15 new enum members |
| `src/X12Net/Validation/EbSegmentValidator.cs` | new `public static` class |
| `test/X12Net.Tests/Validation/EbSegmentValidatorTests.cs` | new test class, ~50 tests |

---

## EB Segment Element Reference (271 context)

| Element | Name | Required | Notes |
|---------|------|----------|-------|
| EB01 | Eligibility or Benefit Information Code | Required | Code set 235 |
| EB02 | Coverage Level Code | Situational | Code set 1205 |
| EB03 | Service Type Code | Situational | Code set 1365; composite — one or more codes joined by `:` |
| EB04 | Insurance Type Code | Situational | Subset of code set 1205 |
| EB05 | Plan Coverage Description | Situational | Free text, max 50 chars |
| EB06 | Time Period Qualifier | Situational | Code set 615 |
| EB07 | Monetary Amount | Situational | Decimal, non-negative |
| EB08 | Percent | Situational | Decimal, 0.00–100.00 |
| EB09 | Quantity Qualifier | Situational | Code set 673; must pair with EB10 |
| EB10 | Quantity | Situational | Decimal, non-negative; must pair with EB09 |
| EB11 | Authorization/Certification Required | Situational | `N`, `Y`, or `U` |
| EB12 | In-Plan Network Indicator | Situational | `N`, `U`, `W`, or `Y` |
| EB13 | Procedure Identifier | Situational | Composite; presence check only |

---

## New `X12ErrorCode` Members

```csharp
// EB01
EbMissingEligibilityCode,           // EB01 absent or blank (required)
EbInvalidEligibilityCode,           // EB01 not in code set 235

// EB02–EB04
EbInvalidCoverageLevelCode,         // EB02 not in code set 1205
EbInvalidServiceTypeCode,           // EB03 component not in code set 1365
EbInvalidInsuranceTypeCode,         // EB04 not in insurance type subset

// EB05–EB06
EbPlanDescriptionTooLong,           // EB05 > 50 chars
EbInvalidTimePeriodQualifier,       // EB06 not in code set 615

// EB07–EB10
EbNegativeMonetaryAmount,           // EB07 < 0
EbPercentOutOfRange,                // EB08 < 0 or > 100.00
EbInvalidQuantityQualifier,         // EB09 not in code set 673
EbNegativeQuantity,                 // EB10 < 0

// EB11–EB12
EbInvalidAuthorizationIndicator,    // EB11 not in { N, Y, U }
EbInvalidInPlanNetworkIndicator,    // EB12 not in { N, U, W, Y }

// Relational rules
EbQuantityQualifierWithoutQuantity, // EB09 present ↔ EB10 must be present (and vice versa)
EbTimePeriodRequiresAmount,         // EB06 present → at least one of EB07/EB08/EB10 required
```

---

## `EbSegmentValidator` Class Structure

```csharp
public static class EbSegmentValidator
{
    // ── Code sets (static readonly HashSet<string>) ───────────────────────
    // Eb01Codes   — code set 235 (~36 values)
    // Eb02Codes   — code set 1205 coverage levels (9 values)
    // Eb03Codes   — code set 1365 service types (~80 values)
    // Eb04Codes   — insurance type subset (~17 values)
    // Eb06Codes   — code set 615 time period qualifiers (14 values)
    // Eb09Codes   — code set 673 quantity qualifiers (5 values)

    // ── Public API ────────────────────────────────────────────────────────
    public static X12ValidationResult Validate(X12Segment ebSegment)
    public static X12ValidationResult ValidateRaw(string ebSegmentText)

    // ── Private rules (one method per element / relational rule) ─────────
    private static void CheckEb01Required(...)
    private static void CheckEb02CoverageLevelCode(...)
    private static void CheckEb03ServiceTypeCode(...)   // splits on ':' for composite
    private static void CheckEb04InsuranceTypeCode(...)
    private static void CheckEb05PlanDescription(...)
    private static void CheckEb06TimePeriodQualifier(...)
    private static void CheckEb07MonetaryAmount(...)
    private static void CheckEb08Percent(...)
    private static void CheckEb09QuantityQualifier(...)
    private static void CheckEb10Quantity(...)
    private static void CheckEb11AuthorizationRequired(...)
    private static void CheckEb12InPlanNetwork(...)
    private static void CheckEb13ProcedureIdentifier(...)  // limited: non-empty check only
    private static void CheckEb09Eb10Pairing(...)
    private static void CheckEb06RequiresNumericElement(...)

    // ── Helpers ───────────────────────────────────────────────────────────
    private static string GetElement(X12Segment seg, int oneBasedIndex)
    private static bool TryParseDecimal(string value, out decimal result)
}
```

**Key design decisions:**

- `Validate(X12Segment)` accepts a pre-parsed segment — callers use `X12Reader` first, then pass
  matching EB segments in. Keeps the validator composable with the existing pipeline.
- `ValidateRaw(string)` is a convenience overload: parses one segment string with `X12Reader`
  and delegates to `Validate(X12Segment)`. Used heavily in tests.
- `GetElement(seg, n)` returns `string.Empty` when `n > seg.Elements.Count` — critical for
  sparse segments where senders omit trailing empty elements.
- EB03 composite elements arrive from `X12Reader` as a single string like `"30:35:48"` (`:` is
  the standard component separator). `CheckEb03` splits on `':'` and validates each component.
- Numeric parsing always uses `CultureInfo.InvariantCulture` to avoid locale-sensitive issues.

---

## Embedded Code Sets

### EB01 — Code Set 235 (Eligibility/Benefit Information)
`"1"`, `"2"`, `"3"`, `"4"`, `"5"`, `"6"`, `"7"`, `"8"`, `"9"`, `"A"`, `"B"`, `"C"`, `"CB"`,
`"D"`, `"E"`, `"F"`, `"G"`, `"H"`, `"I"`, `"J"`, `"K"`, `"L"`, `"M"`, `"MC"`, `"N"`, `"O"`,
`"P"`, `"Q"`, `"R"`, `"S"`, `"T"`, `"U"`, `"V"`, `"W"`, `"X"`.

### EB02 — Code Set 1205 (Coverage Level)
`"CHD"`, `"DEP"`, `"ECH"`, `"EMP"`, `"ESP"`, `"FAM"`, `"IND"`, `"SPC"`, `"TWO"`.

### EB03 — Code Set 1365 (Service Type, composite components)
Representative 271 values: `"1"` through `"98"` (most numeric values), plus `"A0"`–`"AK"`.
Full list embedded as a `HashSet<string>` in source.

### EB04 — Insurance Type Subset
`"AP"`, `"C1"`, `"CO"`, `"D"`, `"GP"`, `"HM"`, `"MA"`, `"MB"`, `"MC"`, `"MH"`, `"MP"`,
`"OT"`, `"PR"`, `"PS"`, `"SP"`, `"TF"`, `"WC"`.

### EB06 — Code Set 615 (Time Period Qualifier)
`"6"`, `"7"`, `"13"`, `"21"`, `"24"`, `"25"`, `"26"`, `"27"`, `"28"`, `"29"`, `"30"`,
`"33"`, `"34"`, `"35"`.

### EB09 — Code Set 673 (Quantity Qualifier)
`"CA"`, `"LA"`, `"LE"`, `"NE"`, `"VS"`.

### EB11 / EB12
Checked inline (small fixed sets); no HashSet needed.

---

## 17 TDD Cycles

| Cycle | Test(s) | What it drives |
|-------|---------|----------------|
| 1 | `Validator_returns_valid_for_minimal_EB_with_known_code` | Class skeleton, `ValidateRaw`, `GetElement` helper |
| 2 | EB01 empty / absent → `EbMissingEligibilityCode` | Required field check |
| 3 | EB01 invalid code → `EbInvalidEligibilityCode`; theory over all valid codes | EB01 HashSet |
| 4 | EB02 invalid → `EbInvalidCoverageLevelCode`; absent → valid | EB02 HashSet |
| 5 | EB03 single valid; multi-component `"30:35:48"`; one bad component → error | Composite split logic |
| 6 | EB04 invalid → `EbInvalidInsuranceTypeCode`; valid `"HM"` → valid | EB04 HashSet |
| 7 | EB05 51 chars → `EbPlanDescriptionTooLong`; 50 chars → valid | Length check |
| 8 | EB06 invalid → `EbInvalidTimePeriodQualifier`; theory over valid codes | EB06 HashSet |
| 9 | EB07 negative → `EbNegativeMonetaryAmount`; zero and positive → valid | `TryParseDecimal` helper |
| 10 | EB08 negative and >100 → `EbPercentOutOfRange`; boundary 0 and 100 → valid | Percent range check |
| 11 | EB09 invalid → `EbInvalidQuantityQualifier`; theory over valid codes | EB09 HashSet |
| 12 | EB10 negative → `EbNegativeQuantity`; zero → valid | Reuses `TryParseDecimal` |
| 13 | EB11 invalid → `EbInvalidAuthorizationIndicator`; theory over `N`, `Y`, `U` | Inline literal check |
| 14 | EB12 invalid → `EbInvalidInPlanNetworkIndicator`; theory over `N`, `U`, `W`, `Y` | Inline literal check |
| 15 | EB09 without EB10 → error; EB10 without EB09 → error; both/neither → valid | Pairing relational rule |
| 16 | EB06 alone → `EbTimePeriodRequiresAmount`; EB06 + EB07/08/10 → valid | Amount relational rule |
| 17 | Fully populated valid segment — all 12 elements | Integration / all rules together |

---

## Integration with Existing Code

`X12Validator` is **not modified**. It validates interchange-level structure. `EbSegmentValidator`
validates transaction-level element semantics. Callers compose them:

```csharp
// 1. Structural check
var structResult = X12Validator.Validate(rawEdi);

// 2. EB semantic check
using var reader = new X12Reader(rawEdi);
var ebErrors = reader.ReadAllSegments()
    .Where(s => s.SegmentId == "EB")
    .SelectMany(eb => EbSegmentValidator.Validate(eb).Errors)
    .ToList();
```

A future `Ts271Validator` can aggregate both without touching either existing class.

---

## Key Risks

1. **Sparse segments** — any `CheckXxx` that bypasses `GetElement` and uses `seg[n]` directly
   will throw on a valid sparse segment with trailing elements omitted. Every method must use
   the helper.
2. **EB03 composite delimiter assumption** — the validator assumes `:` is the component
   separator, consistent with `X12Reader` normalisation. Documents this assumption prominently.
3. **Numeric parsing culture** — always use `CultureInfo.InvariantCulture`.
4. **EB13 scope limit** — full procedure code validation (ICD-10, CPT, HCPCS) is explicitly
   out of scope; EB13 gets a presence/non-empty-component check only.
