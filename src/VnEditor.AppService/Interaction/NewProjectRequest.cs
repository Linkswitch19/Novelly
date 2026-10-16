using System;
using System.Collections.Generic;
using System.Text;

namespace VnEditor.AppService.Interaction;

/// <summary>
/// Contiene il nome e la cartella scelti dall'utente
/// come dati di ingresso del caso d'uso di creazione del progetto.
/// </summary>
public sealed record NewProjectRequest(string ProjectName, string ProjectFolder);

