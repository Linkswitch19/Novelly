
using System.Xml.Linq;
using VnEditor.Domain.Assets;
using VnEditor.Domain.Characters;
using VnEditor.Domain.Scenes;

namespace VnEditor.Domain;
/// <summary>
/// Radice del modello: tutto ciò che l'utente ha creato.
/// È l'unica fonte di verità; lo script Ren'Py si rigenera sempre da qui.
/// </summary>
public sealed class Project
{

    private IdGenerator _ids = new();
    /// <summary>Versione del formato del file salvato.</summary>
    public int FormatVersion { get; init; } = 1;
    /// <summary>Titolo del gioco.</summary>
    public required string Title { get; set; }

    /// <summary>Id della scena da cui parte il gioco.</summary>
    public string? StartSceneId { get; set; }

    //Assets
    public List<Character> Characters { get; } = [];
    public List<Background> Backgrounds { get; } = [];
    public List<MusicTrack> Music { get; } = [];
    public List<Scene> Scenes { get; } = [];

    public Character AddCharacter(string name)
    {
        Character character = new(){Id = this._ids.Next<Character>(), Name = name};
        Characters.Add(character);
        return character;
    }
    public Background AddBackground (string name, string filename)
    {
        Background Background = new() 
        { 
            Id= this._ids.Next<Background>(),Name   = name,FileName = filename
        };
        Backgrounds.Add(Background);
        return Background;
    }
    public MusicTrack AddMusic(string name, string fileName)
    {
        var track = new MusicTrack
        {
            Id = _ids.Next<MusicTrack>(),
            Name = name,
            FileName = fileName
        };
        Music.Add(track);
        return track;
    }

    /// <summary>
    /// Crea una scena. La prima diventa automaticamente quella iniziale,
    /// così un progetto nuovo è subito avviabile.
    /// </summary>
    public Scene AddScene(string name)
    {
        var scene = new Scene { Id = _ids.Next<Scene>(), Name = name };
        Scenes.Add(scene);
        StartSceneId ??= scene.Id;
        return scene;
    }




    public Character? FindCharacter(string id) =>
        Characters.FirstOrDefault(c => c.Id == id);

    public Background? FindBackground(string id) =>
        Backgrounds.FirstOrDefault(b => b.Id == id);

    public MusicTrack? FindMusic(string id) =>
        Music.FirstOrDefault(m => m.Id == id);

    public Scene? FindScene(string id) =>
        Scenes.FirstOrDefault(s => s.Id == id);
}
