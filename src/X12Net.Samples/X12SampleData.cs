using System.IO;
using System.Reflection;

namespace X12Net.Samples;

/// <summary>
/// Provides anonymized sample EDI X12 interchanges as strings, loaded from
/// embedded resources. Suitable for unit tests, demos, and documentation snippets.
/// </summary>
public static class X12SampleData
{
    // ── 999 Functional Acknowledgment ────────────────────────────────────────

    /// <summary>
    /// A 999 Functional Acknowledgment that fully accepts one 837P functional group
    /// (<c>AK9*A</c>).
    /// </summary>
    public static string Edi999Accepted => LoadSample("999_accepted.edi");

    /// <summary>
    /// A 999 Functional Acknowledgment that rejects one 837P transaction
    /// (<c>AK5*R*5</c> — missing required segment).
    /// </summary>
    public static string Edi999Rejected => LoadSample("999_rejected.edi");

    // ── 835 Health Care Claim Payment / Remittance Advice ────────────────────

    /// <summary>
    /// An 835 Health Care Claim Payment/Remittance Advice with one CLP loop and
    /// one SVC line.
    /// </summary>
    public static string Edi835 => LoadSample("835.edi");

    // ── 837 Health Care Claims ────────────────────────────────────────────────

    /// <summary>
    /// An 837P (Professional) Health Care Claim with one CLM and one SV1 line
    /// (version <c>005010X222A2</c>).
    /// </summary>
    public static string Edi837P => LoadSample("837P.edi");

    /// <summary>
    /// An 837I (Institutional) Health Care Claim with one CLM and CL1 segment
    /// (version <c>005010X223A2</c>).
    /// </summary>
    public static string Edi837I => LoadSample("837I.edi");

    /// <summary>
    /// An 837D (Dental) Health Care Claim with one CLM and one SV3 line
    /// (version <c>005010X224A2</c>).
    /// </summary>
    public static string Edi837D => LoadSample("837D.edi");

    // ── 270 / 271 Eligibility ─────────────────────────────────────────────────

    /// <summary>
    /// A 270 Health Care Eligibility/Benefit Inquiry for one subscriber.
    /// </summary>
    public static string Edi270 => LoadSample("270.edi");

    /// <summary>
    /// A 271 Health Care Eligibility/Benefit Information response with two EB
    /// segments (family and individual benefit levels).
    /// </summary>
    public static string Edi271 => LoadSample("271.edi");

    // ── 834 Benefit Enrollment ────────────────────────────────────────────────

    /// <summary>
    /// An 834 Benefit Enrollment and Maintenance transaction with one INS loop.
    /// </summary>
    public static string Edi834 => LoadSample("834.edi");

    // ── 276 / 277 Claim Status ────────────────────────────────────────────────

    /// <summary>
    /// A 276 Health Care Claim Status Request for one claim.
    /// </summary>
    public static string Edi276 => LoadSample("276.edi");

    /// <summary>
    /// A 277 Health Care Claim Status Response with one STC status segment.
    /// </summary>
    public static string Edi277 => LoadSample("277.edi");

    // ── Private helpers ───────────────────────────────────────────────────────

    private static string LoadSample(string fileName)
    {
        var assembly    = typeof(X12SampleData).Assembly;
        var resourceName = $"X12Net.Samples.Data.{fileName}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{resourceName}' was not found in assembly '{assembly.FullName}'. " +
                $"Ensure the file exists under Data\\ and the project's EmbeddedResource glob is intact.");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
