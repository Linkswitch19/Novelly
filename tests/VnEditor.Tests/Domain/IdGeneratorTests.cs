using VnEditor.Domain;
using VnEditor.Domain.Assets;
using VnEditor.Domain.Characters;
using Xunit;

namespace VnEditor.Tests.Domain;

public class IdGeneratorTests
{
    [Fact]
    public void IlPrimoIdPartaDaUno()
    {
        Assert.Equal("char_1", new IdGenerator().Next<Character>());
    }

    [Fact]
    public void GliIdSuccessiviIncrementano()
    {
        var ids = new IdGenerator();
        ids.Next<Character>();

        Assert.Equal("char_2", ids.Next<Character>());
    }

    [Fact]
    public void OgniTipoHaIlSuoContatore()
    {
        var ids = new IdGenerator();
        ids.Next<Character>();
        ids.Next<Character>();

        Assert.Equal("bg_1", ids.Next<Background>());
    }

    [Fact]
    public void IContatoriSiPossonoRipristinare()
    {
        var ids = new IdGenerator();
        ids.Restore(new Dictionary<string, int> { ["char_"] = 7 });

        Assert.Equal("char_8", ids.Next<Character>());
    }

    [Fact]
    public void OgniEntitaHaUnPrefissoDefinito()
    {
        var tipi = typeof(Entity).Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(Entity)) && !t.IsAbstract);

        foreach (var tipo in tipi)
            IdGenerator.PrefixOf(tipo);   // lancia se manca
    }
}