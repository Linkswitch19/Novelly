using System;
using System.Collections.Generic;
using System.Text;

namespace VnEditor.AppService.Interaction;

/// <summary>
/// Contiene il nome del progetto e la cartella genitore scelta dall'utente.
/// Il caso d'uso New determina la destinazione creando una sottocartella
/// con il nome del progetto all'interno della posizione indicata.
/// </summary>
public sealed record NewProjectRequest(string ProjectName, string ParentFolder);

