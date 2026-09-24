using VnEditor.Domain;
using VnEditor.Domain.Variables;
using Xunit;

namespace VnEditor.Tests.Domain;
/// <summary>
/// Verifica i tipi di variabile, i valori iniziali, la generazione degli ID,
/// la ricerca nel progetto, le operazioni disponibili e gli operatori di confronto.
/// </summary>
public class VariableTests
{
    [Fact]
    public void UnProgettoNuovoNonHaVariabili()
    {
        Project project = new() { Title = "Prova" };

        Assert.Empty(project.Variables);
    }

    [Fact]
    public void AggiungeIValoriInizialiDeiTreTipi()
    {
        Project project = new() { Title = "Prova" };

        NumberVariable number = project.AddNumberVariable("Punti", 10.5m);
        BooleanVariable boolean = project.AddBooleanVariable("Porta aperta", true);
        TextVariable text = project.AddTextVariable("Nome", "Anna");

        Assert.Equal(10.5m, number.InitialValue);
        Assert.True(boolean.InitialValue);
        Assert.Equal("Anna", text.InitialValue);
        Assert.Equal(3, project.Variables.Count);
    }

    [Fact]
    public void TipiDiversiCondividonoLaSequenzaDegliId()
    {
        Project project = new() { Title = "Prova" };

        NumberVariable number = project.AddNumberVariable("Punti", 0m);
        BooleanVariable boolean = project.AddBooleanVariable("Porta aperta", false);
        TextVariable text = project.AddTextVariable("Nome", string.Empty);

        Assert.Equal("var_1", number.Id);
        Assert.Equal("var_2", boolean.Id);
        Assert.Equal("var_3", text.Id);
    }

    [Fact]
    public void TrovaUnaVariabileEsistente()
    {
        Project project = new() { Title = "Prova" };
        NumberVariable number = project.AddNumberVariable("Punti", 0m);

        Assert.Same(number, project.FindVariable("var_1"));
        Assert.Null(project.FindVariable("var_99"));
    }

    [Fact]
    public void DefinisceLeOperazioniDelleVariabili()
    {
        VariableOperation[] expected =
        [
            VariableOperation.Set,
            VariableOperation.Add,
            VariableOperation.Subtract,
            VariableOperation.Toggle
        ];

        Assert.Equal(expected, Enum.GetValues<VariableOperation>());
    }

    [Fact]
    public void DefinisceGliOperatoriDiConfronto()
    {
        ComparisonOperator[] expected =
        [
            ComparisonOperator.Equal,
            ComparisonOperator.NotEqual,
            ComparisonOperator.GreaterThan,
            ComparisonOperator.LessThan,
            ComparisonOperator.GreaterThanOrEqual,
            ComparisonOperator.LessThanOrEqual
        ];

        Assert.Equal(expected, Enum.GetValues<ComparisonOperator>());
    }
}