using System;
using System.Collections.Generic;
using System.Text;
using VnEditor.Domain;

namespace VnEditor.AppService.Serialization;
/// <summary>
/// Converte un progetto nella sua rappresentazione persistente e lo ricostruisce.
/// Il contratto non dipende dalla tecnologia di serializzazione utilizzata.
/// </summary>
public interface IProjectSerializer
{
    string Serialize(Project project);

    Project Deserialize(string content);
}
