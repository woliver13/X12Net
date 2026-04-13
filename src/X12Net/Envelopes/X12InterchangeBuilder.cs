using System.Text;
using woliver13.X12Net.Core;

namespace woliver13.X12Net.Envelopes;

/// <summary>
/// Fluent builder that constructs a complete, standards-compliant EDI X12 interchange
/// including the ISA/IEA envelope, GS/GE functional group pairs, and ST/SE transaction sets.
/// </summary>
public sealed class X12InterchangeBuilder
{
    // ── Configuration ─────────────────────────────────────────────────────

    private readonly string _senderId;
    private readonly string _receiverId;
    private readonly string _date;
    private readonly string _time;
    private readonly int    _icn;

    private readonly char   _elementSep;
    private readonly char   _componentSep;
    private readonly char   _segmentTerm;
    private readonly char   _repetitionSep;
    private readonly string _isaVersion;

    // ── Build state ───────────────────────────────────────────────────────

    // Raw body lines added via AddRawSegment / group tracking
    private readonly List<string> _bodySegments = new();

    private sealed class GroupState
    {
        public string FunctionCode { get; init; } = "";
        public string SenderId     { get; init; } = "";
        public string ReceiverId   { get; init; } = "";
        public string Date         { get; init; } = "";
        public string Time         { get; init; } = "";
        public string Version      { get; init; } = "";
        public int    GroupControlNumber { get; init; }
        public int TransactionCount { get; private set; }

        private readonly List<string> _segments = new();
        public IReadOnlyList<string> Segments => _segments;

        public void AddSegment(string segmentText, char elementSep)
        {
            _segments.Add(segmentText);
            if (segmentText.StartsWith($"ST{elementSep}", StringComparison.Ordinal))
                TransactionCount++;
        }
    }

    private readonly List<GroupState> _groups = new();
    private GroupState? _currentGroup;

    // ── Constructor ───────────────────────────────────────────────────────

    /// <summary>
    /// Initializes the builder with envelope identifiers and optional delimiter overrides.
    /// </summary>
    /// <param name="senderId">ISA06 sender ID (padded to 15 chars).</param>
    /// <param name="receiverId">ISA08 receiver ID (padded to 15 chars).</param>
    /// <param name="date">ISA09 date (YYMMDD, 6 chars).</param>
    /// <param name="time">ISA10 time (HHMM, 4 chars).</param>
    /// <param name="interchangeControlNumber">ISA13 / IEA02 control number (default 1).</param>
    /// <param name="senderQualifier">ISA05 (default "ZZ").</param>
    /// <param name="receiverQualifier">ISA07 (default "ZZ").</param>
    /// <param name="elementSeparator">ISA element delimiter (default <c>*</c>).</param>
    /// <param name="componentSeparator">ISA16 component delimiter (default <c>:</c>).</param>
    /// <param name="segmentTerminator">Segment terminator written after every segment (default <c>~</c>).</param>
    /// <param name="isaVersion">ISA12 version/release number (default <c>"00501"</c>).</param>
    /// <param name="repetitionSeparator">ISA11 repetition separator (default <c>^</c>).</param>
    public X12InterchangeBuilder(
        string senderId,
        string receiverId,
        string date,
        string time,
        int    interchangeControlNumber = 1,
        string senderQualifier         = "ZZ",
        string receiverQualifier       = "ZZ",
        char   elementSeparator        = '*',
        char   componentSeparator      = ':',
        char   segmentTerminator       = '~',
        string isaVersion              = "00501",
        char   repetitionSeparator     = '^')
    {
        _senderId    = senderId;
        _receiverId  = receiverId;
        _date        = date;
        _time        = time;
        _icn         = interchangeControlNumber;
        SenderQualifier   = senderQualifier;
        ReceiverQualifier = receiverQualifier;
        _elementSep    = elementSeparator;
        _componentSep  = componentSeparator;
        _segmentTerm   = segmentTerminator;
        _isaVersion    = isaVersion;
        _repetitionSep = repetitionSeparator;
    }

    /// <summary>ISA05 sender qualifier.</summary>
    public string SenderQualifier   { get; }

    /// <summary>ISA07 receiver qualifier.</summary>
    public string ReceiverQualifier { get; }

    // ── Fluent API ────────────────────────────────────────────────────────

    /// <summary>Begins a GS/GE functional group.</summary>
    /// <param name="functionCode">GS01 functional identifier code (e.g. "FA", "HB").</param>
    /// <param name="senderId">GS02 application sender ID.</param>
    /// <param name="receiverId">GS03 application receiver ID.</param>
    /// <param name="date">GS04 date (YYYYMMDD, 8 chars).</param>
    /// <param name="version">GS08 implementation convention reference.</param>
    /// <param name="groupControlNumber">GS06 / GE02 group control number (default 1).</param>
    /// <param name="time">GS05 time (HHMM, 4 chars). Defaults to the interchange-level time when omitted.</param>
    public X12InterchangeBuilder BeginFunctionalGroup(
        string  functionCode,
        string  senderId,
        string  receiverId,
        string  date,
        string  version,
        int     groupControlNumber = 1,
        string? time               = null)
    {
        if (_currentGroup is not null)
            throw new InvalidOperationException("Call EndFunctionalGroup before beginning another.");

        _currentGroup = new GroupState
        {
            FunctionCode       = functionCode,
            SenderId           = senderId,
            ReceiverId         = receiverId,
            Date               = date,
            Time               = time ?? _time,
            Version            = version,
            GroupControlNumber = groupControlNumber,
        };
        return this;
    }

    /// <summary>
    /// Adds a raw segment string (without terminator) to the current functional group.
    /// </summary>
    public X12InterchangeBuilder AddRawSegment(string segmentText)
    {
        if (_currentGroup is null)
            throw new InvalidOperationException("Call BeginFunctionalGroup first.");
        _currentGroup.AddSegment(segmentText, _elementSep);
        return this;
    }

    /// <summary>Closes the current GS/GE functional group.</summary>
    public X12InterchangeBuilder EndFunctionalGroup()
    {
        if (_currentGroup is null)
            throw new InvalidOperationException("No open functional group.");
        _groups.Add(_currentGroup);
        _currentGroup = null;
        return this;
    }

    // ── Build ─────────────────────────────────────────────────────────────

    /// <summary>Assembles and returns the complete EDI X12 interchange text.</summary>
    public string Build()
    {
        if (_currentGroup is not null)
            throw new InvalidOperationException("EndFunctionalGroup was not called.");

        var sb = new StringBuilder();

        // ── ISA ──
        sb.Append(BuildISA());

        // ── GS / body / GE ──
        foreach (var group in _groups)
        {
            sb.Append(BuildGS(group));
            foreach (var seg in group.Segments)
            {
                sb.Append(seg);
                sb.Append(_segmentTerm);
            }
            sb.Append(BuildGE(group));
        }

        // ── IEA ──
        sb.Append(BuildIEA());

        return sb.ToString();
    }

    // ── Segment construction helpers ──────────────────────────────────────

    private string BuildISA()
    {
        // ISA is fixed-width: 106 characters total.
        // Fields: ISA01-ISA16, delimited by element separator, closed by segment terminator at [105].
        string icnPadded      = _icn.ToString().PadLeft(9, '0');
        string senderPadded   = _senderId.PadRight(15).Substring(0, 15);
        string receiverPadded = _receiverId.PadRight(15).Substring(0, 15);

        // The ISA segment, without terminator:
        // ISA*00*          *00*          *ZZ*<sender15>*ZZ*<receiver15>*<date>*<time>*<ISA11>*<ISA12>*<icn9>*0*P*<ISA16>
        string body =
            $"ISA{_elementSep}" +
            $"00{_elementSep}" +
            $"          {_elementSep}" +
            $"00{_elementSep}" +
            $"          {_elementSep}" +
            $"{SenderQualifier}{_elementSep}" +
            $"{senderPadded}{_elementSep}" +
            $"{ReceiverQualifier}{_elementSep}" +
            $"{receiverPadded}{_elementSep}" +
            $"{_date}{_elementSep}" +
            $"{_time}{_elementSep}" +
            $"{_repetitionSep}{_elementSep}" +
            $"{_isaVersion}{_elementSep}" +
            $"{icnPadded}{_elementSep}" +
            $"0{_elementSep}" +
            $"P{_elementSep}" +
            $"{_componentSep}";

        // body must be exactly 105 chars before the terminator
        if (body.Length != 105)
            throw new InvalidOperationException(
                $"ISA body is {body.Length} chars; expected 105. Check field widths.");

        return body + _segmentTerm;
    }

    private string BuildGS(GroupState g)
    {
        // GS*<functionCode>*<senderId>*<receiverId>*<date>*<time>*<gcn>*X*<version>~
        // We need an 8-digit date for GS04 — caller supplies it.
        string gcn = g.GroupControlNumber.ToString();
        return
            $"GS{_elementSep}" +
            $"{g.FunctionCode}{_elementSep}" +
            $"{g.SenderId}{_elementSep}" +
            $"{g.ReceiverId}{_elementSep}" +
            $"{g.Date}{_elementSep}" +
            $"{g.Time}{_elementSep}" +
            $"{gcn}{_elementSep}" +
            $"X{_elementSep}" +
            $"{g.Version}{_segmentTerm}";
    }

    private string BuildGE(GroupState g) =>
        $"GE{_elementSep}{g.TransactionCount}{_elementSep}{g.GroupControlNumber}{_segmentTerm}";

    private string BuildIEA() =>
        $"IEA{_elementSep}{_groups.Count}{_elementSep}{_icn.ToString().PadLeft(9, '0')}{_segmentTerm}";
}
