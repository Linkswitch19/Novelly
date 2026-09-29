using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using VnEditor.Domain.Scenes.Blocks;
using Xunit;

namespace VnEditor.Tests.Generation;

/// <summary>
/// Verifica che ogni sottoclasse concreta di Block abbia un test dedicato
/// e una copertura dichiarata nella suite del generatore.
/// </summary>
public class RenpyBlockCoverageTests
{
    [Fact]
    public void OgniBloccoConcretoHaUnTestDedicatoDichiarato()
    {
        Dictionary<Type, string> declaredTests = new()
        {
            [typeof(BackgroundBlock)] =
                nameof(RenpyBlockGenerationTests.BackgroundBlock_GeneraLaScenaConFade),
            [typeof(ShowCharacterBlock)] =
                nameof(RenpyBlockGenerationTests.ShowCharacterBlock_GeneraIlPersonaggioConLaSuaEspressione),
            [typeof(DialogueBlock)] =
                nameof(RenpyBlockGenerationTests.DialogueBlock_GeneraIlDialogoConEscaping),
            [typeof(MusicBlock)] =
                nameof(RenpyBlockGenerationTests.MusicBlock_GeneraIlPercorsoAudio)
        };

        Type[] concreteBlocks = typeof(Block).Assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(Block)))
            .ToArray();

        Type[] uncoveredBlocks = concreteBlocks.Except(declaredTests.Keys).ToArray();
        Type[] obsoleteDeclarations = declaredTests.Keys.Except(concreteBlocks).ToArray();

        Assert.Empty(uncoveredBlocks);
        Assert.Empty(obsoleteDeclarations);
        Assert.Equal(declaredTests.Count, declaredTests.Values.Distinct(StringComparer.Ordinal).Count());

        foreach (KeyValuePair<Type, string> declaration in declaredTests)
        {
            MethodInfo testMethod = typeof(RenpyBlockGenerationTests).GetMethod(
                declaration.Value,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                ?? throw new InvalidOperationException(
                    $"Manca il test dichiarato per {declaration.Key.Name}: {declaration.Value}.");

            Assert.True(
                testMethod.IsDefined(typeof(FactAttribute), false),
                $"{declaration.Value} deve avere [Fact].");
            Assert.Empty(testMethod.GetParameters());
            Assert.Equal(typeof(void), testMethod.ReturnType);
        }
    }
}