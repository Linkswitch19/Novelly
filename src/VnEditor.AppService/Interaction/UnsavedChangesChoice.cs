using System;
using System.Collections.Generic;
using System.Text;

namespace VnEditor.AppService.Interaction;

/// <summary>
/// Rappresenta la decisione dell'utente sulle modifiche non salvate
/// prima di sostituire il progetto corrente o chiudere la finestra.
/// </summary>
public enum UnsavedChangesChoice
{
    Cancel = 0,
    Save,
    Discard
}
