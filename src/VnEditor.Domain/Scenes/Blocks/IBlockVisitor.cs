using System;
using System.Collections.Generic;
using System.Text;

namespace VnEditor.Domain.Scenes.Blocks;
/// <summary>
/// Chi sa fare qualcosa con i blocchi: generare uno script, calcolare
/// cosa si vede a schermo, validare.
/// Il dominio dichiara solo quali tipi di blocco esistono; cosa farne
/// è deciso da chi implementa questa interfaccia.
/// </summary>
/// 
public interface IBlockVisitor
{
    void Visit(BackgroundBlock block);
    void Visit(ShowCharacterBlock block);
    void Visit(DialogueBlock block);
    void Visit(MusicBlock block);
}

