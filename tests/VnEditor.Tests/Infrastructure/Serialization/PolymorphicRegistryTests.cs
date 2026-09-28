using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using VnEditor.Domain.Scenes.Blocks;
using VnEditor.Domain.Variables;
using VnEditor.Infrastructure.Serialization;
using Xunit;

namespace VnEditor.Tests.Infrastructure.Serialization;

public sealed class PolymorphicRegistryTests
{
    [Fact]
    public void EveryConcreteBlockIsRegistered()
    {
        Type[] concreteBlocks = typeof(Block).Assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract && typeof(Block).IsAssignableFrom(type))
            .ToArray();

        JsonSerializerOptions options = ProjectJsonOptions.Create();
        JsonPolymorphismOptions polymorphism = options.GetTypeInfo(typeof(Block)).PolymorphismOptions!;

        Type[] registeredBlocks = polymorphism.DerivedTypes
            .Select(entry => entry.DerivedType)
            .ToArray();

        Assert.Equal(
            concreteBlocks.OrderBy(type => type.FullName),
            registeredBlocks.OrderBy(type => type.FullName));
    }

    [Fact]
    public void EveryConcreteVariableIsRegistered()
    {
        Type[] concreteVariables = typeof(Variable).Assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract && typeof(Variable).IsAssignableFrom(type))
            .ToArray();

        JsonSerializerOptions options = ProjectJsonOptions.Create();
        JsonPolymorphismOptions polymorphism = options.GetTypeInfo(typeof(Variable)).PolymorphismOptions!;

        Type[] registeredVariables = polymorphism.DerivedTypes
            .Select(entry => entry.DerivedType)
            .ToArray();

        Assert.Equal(
            concreteVariables.OrderBy(type => type.FullName),
            registeredVariables.OrderBy(type => type.FullName));
    }

    [Fact]
    public void BlockDiscriminatorsRemainStable()
    {
        JsonSerializerOptions options = ProjectJsonOptions.Create();
        JsonPolymorphismOptions polymorphism = options.GetTypeInfo(typeof(Block)).PolymorphismOptions!;

        Assert.Equal("background", polymorphism.DerivedTypes
            .Single(entry => entry.DerivedType == typeof(BackgroundBlock)).TypeDiscriminator);

        Assert.Equal("dialogue", polymorphism.DerivedTypes
            .Single(entry => entry.DerivedType == typeof(DialogueBlock)).TypeDiscriminator);

        Assert.Equal("music", polymorphism.DerivedTypes
            .Single(entry => entry.DerivedType == typeof(MusicBlock)).TypeDiscriminator);

        Assert.Equal("show_character", polymorphism.DerivedTypes
            .Single(entry => entry.DerivedType == typeof(ShowCharacterBlock)).TypeDiscriminator);
    }

    [Fact]
    public void VariableDiscriminatorsRemainStable()
    {
        JsonSerializerOptions options = ProjectJsonOptions.Create();
        JsonPolymorphismOptions polymorphism = options.GetTypeInfo(typeof(Variable)).PolymorphismOptions!;

        string[] discriminators = polymorphism.DerivedTypes
            .Select(entry => Assert.IsType<string>(entry.TypeDiscriminator))
            .OrderBy(discriminator => discriminator, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(["boolean", "number", "text"], discriminators);
    }
}