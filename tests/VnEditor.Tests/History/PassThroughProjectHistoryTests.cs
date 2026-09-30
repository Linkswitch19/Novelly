using VnEditor.Domain;
using VnEditor.Infrastructure.History;
using Xunit;

namespace VnEditor.Tests.Infrastructure.History;

// Verifica che la history temporanea esegua le modifiche sul progetto corrente
// senza sostituirlo e propaghi gli errori della modifica.
public sealed class PassThroughProjectHistoryTests
{
    [Fact]
    public void EditExecutesOnceOnTheCurrentProject()
    {
        Project project = new() { Title = "Test" };
        PassThroughProjectHistory history = new(project);
        int executionCount = 0;

        history.Edit(
            "Aggiungi scena",
            currentProject =>
            {
                Assert.Same(project, currentProject);
                currentProject.AddScene("Inizio");
                executionCount++;
            },
            mergeKey: "scene-edit");

        Assert.Equal(1, executionCount);
        Assert.Same(project, history.CurrentProject);
        Assert.Single(project.Scenes);
    }

    [Fact]
    public void EditPropagatesTheModificationException()
    {
        Project project = new() { Title = "Test" };
        PassThroughProjectHistory history = new(project);
        InvalidOperationException expected = new("Modifica non riuscita");

        InvalidOperationException actual = Assert.Throws<InvalidOperationException>(
            () => history.Edit("Modifica", _ => throw expected));

        Assert.Same(expected, actual);
    }
}