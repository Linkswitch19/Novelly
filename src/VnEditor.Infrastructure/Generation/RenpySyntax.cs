

using VnEditor.Domain.Scenes.Blocks;

namespace VnEditor.Infrastructure.Generation;
public static class RenpySyntax
{
    public static string Label(string id)
        => $"label {id}:";
    public static string Jump(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException(
                "L'ID della scena non può essere nullo o vuoto.",
                nameof(id));
        return $"jump {id}";
    }
        

    public static string Scene(string backgroundId)
        => $"scene {backgroundId} with fade";
    
    public static string Show (string characterId , string expressionId)
        => $"show {characterId} {expressionId}";
    
    public static string Dialogue(string characterId, string text)
        => $"{characterId} \"{RenpyEscape.Text(text)}\"";
    public static string PlayMusic(string? fileName)
        => $"play music \"audio/{fileName}\"";

    public static string DefineCharacter(
        string id,
        string name,
        string color)
        => $"define {id} = Character(\"{name}\", color=\"{color}\")";

    public const string Return = "return";


}