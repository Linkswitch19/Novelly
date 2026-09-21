using VnEditor.Domain;
using Xunit;

namespace VnEditor.Tests.Domain;

public class ProjectTests
{
    private static Project CreaProgetto()
    {
        var progetto = new Project { Title = "Prova" };

        var anna = progetto.AddCharacter("Anna");
        anna.AddExpression("felice", "char_1 espr_1.png");

        progetto.AddBackground("Aula", "bg_1.jpg");
        progetto.AddMusic("Tema", "mus_1.ogg");

        return progetto;
    }

    [Fact]
    public void UnProgettoNuovoEVuoto()
    {
        var progetto = new Project { Title = "Vuoto" };

        Assert.Empty(progetto.Scenes);
        Assert.Empty(progetto.Characters);
        Assert.Null(progetto.StartSceneId);
        Assert.Equal(1, progetto.FormatVersion);
    }

    [Fact]
    public void AggiungereUnPersonaggioGliAssegnaUnId()
    {
        var progetto = new Project { Title = "Prova" };
        var anna = progetto.AddCharacter("Anna");

        Assert.Equal("char_1", anna.Id);
        Assert.Equal("Anna", anna.Name);
        Assert.Single(progetto.Characters);
    }

    [Fact]
    public void DuePersonaggiHannoIdDiversi()
    {
        var progetto = new Project { Title = "Prova" };

        Assert.NotEqual(progetto.AddCharacter("Anna").Id,
                        progetto.AddCharacter("Bruno").Id);
    }

    [Fact]
    public void LaPrimaScenaDiventaQuellaIniziale()
    {
        var progetto = new Project { Title = "Prova" };
        var prima = progetto.AddScene("Inizio");

        Assert.Equal(prima.Id, progetto.StartSceneId);
    }

    [Fact]
    public void LeSceneSuccessiveNonCambianoQuellaIniziale()
    {
        var progetto = new Project { Title = "Prova" };
        var prima = progetto.AddScene("Inizio");
        progetto.AddScene("Seconda");

        Assert.Equal(prima.Id, progetto.StartSceneId);
    }

    [Fact]
    public void TrovaUnPersonaggioEsistente()
    {
        Assert.Equal("Anna", CreaProgetto().FindCharacter("char_1")?.Name);
    }

    [Fact]
    public void RestituisceNullSeIlPersonaggioNonEsiste()
    {
        Assert.Null(CreaProgetto().FindCharacter("char_99"));
    }

    [Fact]
    public void TrovaUnoSfondoEUnaMusica()
    {
        var progetto = CreaProgetto();

        Assert.Equal("Aula", progetto.FindBackground("bg_1")?.Name);
        Assert.Equal("Tema", progetto.FindMusic("mus_1")?.Name);
    }
}