namespace woliver13.X12Net.Tests.Fixtures;

/// <summary>
/// Shared EDI sample interchanges referenced across multiple test classes.
/// </summary>
internal static class Edi
{
    /// <summary>ISA header + GS segment only (no transactions, no closing envelope).</summary>
    internal const string IsaGs =
        "ISA*00*          *00*          *ZZ*SENDER         *ZZ*RECEIVER       *201909*1200*^*00501*000000001*0*P*:~" +
        "GS*FA*SENDER*RECEIVER*20190901*1200*1*X*005010X231A1~";

    /// <summary>Complete single-transaction 999 functional acknowledgement interchange.</summary>
    internal const string Valid999 =
        "ISA*00*          *00*          *ZZ*SENDER         *ZZ*RECEIVER       *201909*1200*^*00501*000000001*0*P*:~" +
        "GS*FA*SENDER*RECEIVER*20190901*1200*1*X*005010X231A1~" +
        "ST*999*0001~" +
        "AK1*FA*1*005010X231A1~" +
        "AK9*A*1*1*1~" +
        "SE*4*0001~" +
        "GE*1*1~" +
        "IEA*1*000000001~";
}
