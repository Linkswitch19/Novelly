using VnEditor.Infrastructure.Generation;


namespace VnEditor.Tests.Generation;

public class RenpyEscapeTests
{
    [Fact]
    public void IlTestoNormaleNonVieneModificato()
    {
        Assert.Equal("Buongiorno!", RenpyEscape.Text("Buongiorno!"));
    }

    [Fact]
    public void IlTestoVuotoRestaVuoto()
    {
        Assert.Equal("", RenpyEscape.Text(""));
    }

    [Fact]
    public void LaGraffaVieneRaddoppiata()
    {
        Assert.Equal("Ciao {{amico}", RenpyEscape.Text("Ciao {amico}"));
    }

    [Fact]
    public void LaQuadraVieneRaddoppiata()
    {
        Assert.Equal("Ciao [[nome]", RenpyEscape.Text("Ciao [nome]"));
    }

    [Fact]
    public void LeVirgoletteVengonoProtette()
    {
        Assert.Equal("Ha detto \\\"ciao\\\"", RenpyEscape.Text("Ha detto \"ciao\""));
    }

    [Fact]
    public void IlBackslashVieneRaddoppiato()
    {
        Assert.Equal("C:\\\\temp", RenpyEscape.Text("C:\\temp"));
    }

    [Fact]
    public void IlBackslashPrimaDelleVirgoletteNonSiConfonde()
    {
        // Se il backslash non fosse trattato per primo, il risultato
        // sarebbe ambiguo e lo script si romperebbe.
        Assert.Equal("\\\\\\\"", RenpyEscape.Text("\\\""));
    }

    [Fact]
    public void IlRitornoACapoDiventaUnoSpazio()
    {
        Assert.Equal("prima seconda", RenpyEscape.Text("prima\nseconda"));
    }

    [Fact]
    public void GliAccentiELEmojiRestanoIntatti()
    {
        Assert.Equal("Perché? 🎮", RenpyEscape.Text("Perché? 🎮"));
    }

    [Fact]
    public void UnTestoNullNonEAccettato()
    {
        Assert.Throws<ArgumentNullException>(() => RenpyEscape.Text(null!));
    }
}