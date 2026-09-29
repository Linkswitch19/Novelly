using System;
using System.Collections.Generic;
using System.Text;
using VnEditor.Domain.Characters;
using VnEditor.Domain.Scenes;
using VnEditor.Domain.Scenes.Blocks;

namespace VnEditor.Domain.Validation;

/// <summary>
/// Controlla i riferimenti contenuti in un blocco, compresi quelli a variabili,
/// senza modificare il progetto.
/// </summary>
internal class BlockValidationVisitor : IBlockVisitor
{
    private readonly Project _project;
    private readonly Scene _scene;
    private readonly int _blockPosition;
    private readonly List<ValidationIssue> _issues;

    /// <summary>
    /// Inizializza una nuova istanza del visitatore configurando il contesto dell'ispezione.
    /// </summary>
    /// <param name="project">Il progetto globale, utilizzato per cercare le entità referenziate.</param>
    /// <param name="scene">La scena a cui appartiene il blocco (utile per formulare messaggi di errore chiari).</param>
    /// <param name="blockPosition">L'indice numerico del blocco all'interno della scena, per indicare l'esatta posizione dell'errore.</param>
    /// <param name="issues">La lista condivisa in cui accumulare tutte le anomalie riscontrate durante la validazione.</param>
    public BlockValidationVisitor(Project project, Scene scene, int blockPosition, List<ValidationIssue> issues)
    {
        this._project = project;
        this._scene = scene;
        this._blockPosition = blockPosition;
        this._issues = issues;
    }

    /// <summary>
    /// Controlla il riferimento a una variabile, se presente, e poi visita il tipo concreto del blocco.
    /// </summary>
    public void Validate(Block block)
    {
        if (block is IVariableReferencingBlock variableBlock)
            ValidateVariableReference(block, variableBlock);

        block.Accept(this);
    }



    /// <summary>
    /// Valida un blocco di cambio sfondo, verificando che l'asset grafico indicato sia registrato nel progetto.
    /// </summary>
    public void Visit(BackgroundBlock block) 
    {
        if (this._project.FindBackground(block.BackgroundId) is null)
            AddError(ValidationCode.MissingBackground, block, $"Sfondo con ID '{block.BackgroundId}' non trovato.");
    }

    /// <summary>
    /// Valida un blocco di apparizione a schermo, controllando non solo l'esistenza del personaggio, 
    /// ma anche che la specifica espressione (sprite) richiesta gli appartenga realmente.
    /// </summary>
    public void Visit(ShowCharacterBlock block)
    {
        Character? character = this._project.FindCharacter(block.CharacterId);

        if (character is null)
        {
            AddError(ValidationCode.MissingCharacter, block, $"Personaggio con ID '{block.CharacterId}' non trovato.");
            return;
        }

        if (character.FindExpression(block.ExpressionId) is null)
        {
            AddError(
                ValidationCode.MissingExpression,
                block,
                $"Espressione con ID '{block.ExpressionId}' non trovata per il personaggio '{character.Name}' ({character.Id}).");
        }
    }

    /// <summary>
    /// Valida un blocco di testo, assicurandosi che l'oratore associato alla battuta sia un personaggio valido.
    /// </summary>
    public void Visit(DialogueBlock block)
    {
        if (this._project.FindCharacter(block.CharacterId) is null)
            AddError(ValidationCode.MissingCharacter, block, $"Personaggio con ID '{block.CharacterId}' non trovato.");
    }

    /// <summary>
    /// Valida un blocco audio, verificando che la traccia musicale da riprodurre sia presente negli asset.
    /// </summary>
    public void Visit(MusicBlock block)
    {
        if (this._project.FindMusic(block.MusicId) is null)
            AddError(ValidationCode.MissingMusic, block, $"Musica con ID '{block.MusicId}' non trovata.");
    }


    /// <summary>Segnala un ID di variabile che non esiste nel progetto.</summary>
    private void ValidateVariableReference(Block block, IVariableReferencingBlock variableBlock)
    {
        if (_project.FindVariable(variableBlock.VariableId) is null)
            AddError(ValidationCode.MissingVariable, block, $"Variabile con ID '{variableBlock.VariableId}' non trovata.");
    }

    /// <summary>
    /// Metodo di supporto per generare e registrare un problema di validazione.
    /// Arricchisce il messaggio di base con le coordinate esatte (scena, indice e ID del blocco) 
    /// per facilitare l'individuazione e la correzione dell'errore da parte dell'utente.
    /// </summary>
    /// <param name="code">Il codice univoco che identifica il tipo di errore.</param>
    /// <param name="block">Il blocco specifico che ha causato il problema.</param>
    /// <param name="description">La spiegazione dettagliata dell'anomalia riscontrata.</param>
    private void AddError(ValidationCode code, Block block, string description)
    {
        string message = $"Scena '{this._scene.Name}' ({this._scene.Id}), blocco {this._blockPosition} '{block.Label}' ({block.Id}): {description}";
        this._issues.Add(new ValidationIssue(code, ValidationSeverity.Error, message, this._scene.Id, block.Id));
    }


}
