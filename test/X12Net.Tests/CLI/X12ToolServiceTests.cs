using Microsoft.Extensions.DependencyInjection;
using woliver13.X12Net.CLI;

namespace woliver13.X12Net.Tests.CLI;

public class X12ToolServiceTests
{
    private const string ValidInterchange = Fixtures.Edi.Valid999;

    private static IX12ToolService BuildService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IX12ToolService>();
    }

    [Fact]
    public void AddApplication_registers_IX12ToolService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();

        using var provider = services.BuildServiceProvider();

        provider.GetService<IX12ToolService>().ShouldNotBeNull();
    }

    [Fact]
    public void X12ToolService_Parse_returns_segment_ids()
    {
        var svc = BuildService();

        var result = svc.Parse(ValidInterchange);

        result.Success.ShouldBeTrue();
        result.SegmentIds.ShouldBe(new[] { "ISA", "GS", "ST", "AK1", "AK9", "SE", "GE", "IEA" });
    }

    [Fact]
    public void X12ToolService_Validate_returns_valid_for_valid_interchange()
    {
        var svc = BuildService();

        var result = svc.Validate(ValidInterchange);

        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void X12ToolService_Edit_modifies_element()
    {
        var svc = BuildService();

        var result = svc.Edit(ValidInterchange, "GS", 2, "NEWSENDER");

        result.Success.ShouldBeTrue();
        result.Output.ShouldContain("GS*FA*NEWSENDER*");
    }
}
