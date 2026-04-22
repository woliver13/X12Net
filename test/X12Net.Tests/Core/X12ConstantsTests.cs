using woliver13.X12Net.Core;

namespace woliver13.X12Net.Tests.Core;

public class X12ConstantsTests
{
    [Theory]
    [InlineData(nameof(X12Constants.IsaMinLength),    106)]
    [InlineData(nameof(X12Constants.IsaBodyLength),   105)]
    [InlineData(nameof(X12Constants.IsaElementCount),  16)]
    [InlineData(nameof(X12Constants.IsaIdFieldWidth),  15)]
    public void X12Constants_defines_ISA_field_widths(string name, int expected)
    {
        var actual = name switch
        {
            nameof(X12Constants.IsaMinLength)    => X12Constants.IsaMinLength,
            nameof(X12Constants.IsaBodyLength)   => X12Constants.IsaBodyLength,
            nameof(X12Constants.IsaElementCount) => X12Constants.IsaElementCount,
            nameof(X12Constants.IsaIdFieldWidth) => X12Constants.IsaIdFieldWidth,
            _ => throw new ArgumentOutOfRangeException(nameof(name))
        };
        actual.ShouldBe(expected);
    }
}
