using VnEditor.Domain;
using Xunit;

namespace VnEditor.Tests.Domain;

public class CharacterTests
{
    [Fact]
    public void UnPersonaggioNuovoNonHaEspressioni()
    {
        var anna = new Project { Title = "Prova" }.AddCharacter("Anna");

        Assert.Empty(anna.Expressions);
        Assert.Equal("#ffffff", anna.Color);
    }

    [Fact]
    public void AggiungereUnEspressioneLeAssegnaUnId()
    {
        var anna = new Project { Title = "Prova" }.AddCharacter("Anna");
        var felice = anna.AddExpression("felice", "char_1 espr_1.png");

        Assert.Equal("espr_1", felice.Id);
        Assert.Equal("felice", felice.Name);
    }

    [Fact]
    public void OgniPersonaggioContaLeSueEspressioniDaCapo()
    {
        // Gli Id delle espressioni sono unici dentro il personaggio,
        // non in tutto il progetto.
        var progetto = new Project { Title = "Prova" };

        var anna = progetto.AddCharacter("Anna");
        anna.AddExpression("felice", "a.png");

        var bruno = progetto.AddCharacter("Bruno");
        var espressioneDiBruno = bruno.AddExpression("triste", "b.png");

        Assert.Equal("espr_1", espressioneDiBruno.Id);
    }

    [Fact]
    public void TrovaUnEspressioneEsistente()
    {
        var anna = new Project { Title = "Prova" }.AddCharacter("Anna");
        anna.AddExpression("felice", "a.png");

        Assert.Equal("felice", anna.FindExpression("espr_1")?.Name);
    }

    [Fact]
    public void RestituisceNullSeLEspressioneNonEsiste()
    {
        var anna = new Project { Title = "Prova" }.AddCharacter("Anna");

        Assert.Null(anna.FindExpression("espr_99"));
    }
}