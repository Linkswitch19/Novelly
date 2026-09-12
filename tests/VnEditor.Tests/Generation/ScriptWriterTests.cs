using VnEditor.Infrastructure.Generation;
using Xunit;
namespace VnEditor.Tests.Generation;

public class ScriptWriterTests
{
    [Fact]
    public void UnoScrittoreNuovoProduceTestoVuoto()
    {
        var writer = new ScriptWriter();
        Assert.Equal("", writer.Build());
    }

    [Fact]
    public void LeRigheDiPrimoLivelloNonSonoIndentate()
    {
        var writer = new ScriptWriter();
        writer.Line("label start:");
        Assert.Equal("label start:\n", writer.Build());

    }

    [Fact]
    public void UnBloccoIndentaDiQuattroSpazi()
    {
        var writer = new ScriptWriter();
        using (writer.Block("label start:"))
            writer.Line("jump scene_001");
        Assert.Equal("label start:\n    jump scene_001\n", writer.Build());
    }
    [Fact]
    public void IBlocchiAnnidatiSommanoLIndentazione()
    {
        var writer= new ScriptWriter();
        using (writer.Block("menu:"))
        using (writer.Block("\"Si\":"))
            writer.Line("jump scena_002");
        Assert.Equal("menu:\n    \"Si\":\n        jump scena_002\n", writer.Build());
    }

    [Fact]
    public void AllaChiusuraDelBloccoLIndentazioneTorna()
    {
        var writer = new ScriptWriter();
        using (writer.Block("menu:"))
            writer.Line("dentro");
        writer.Line("fuori");
        Assert.Equal("menu:\n    dentro\nfuori\n", writer.Build());
    }
    [Fact]
    public void LeRigheFinisconoConLfMaiConCrlf()
    {
        var writer = new ScriptWriter();
        writer.Line("prima");
        writer.Line("seconda");

        Assert.DoesNotContain("\r", writer.Build());
    }

    [Fact]
    public void LaRigaVuotaNonEIndentata()
    {
        var writer = new ScriptWriter();
        using (writer.Block("label start:"))
        {
            writer.Line("dentro");
            writer.BlankLIne();
        }

        Assert.Equal("label start:\n    dentro\n\n", writer.Build());
    }

    [Fact]
    public void NonSiPuoAprireUnBloccoSenzaIntestazione()
    {
        var writer = new ScriptWriter();
        Assert.Throws<ArgumentException>(() => writer.Block("   "));
    }
}