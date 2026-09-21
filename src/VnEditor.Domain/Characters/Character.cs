using System;
using System.Collections.Generic;
using System.Text;

namespace VnEditor.Domain.Characters;

public sealed class Character : Entity
{

    private readonly IdGenerator _ids= new();
    /// <summary>Colore del nome nel box del dialogo, es. "#e05a8c".</summary>
    public string Color { get; set; } = "#ffffff";

    public List<Expression> Expressions { get; } = [];

    /// <summary>
    /// Crea un'espressione assegnandole un Id interno. È il personaggio
    /// a generarlo, perché è lui a conoscere quelle già esistenti.
    /// </summary>
    public Expression AddExpression(string name, string fileName)
    {
        var expression = new Expression
        {
            Id = _ids.Next<Expression>(),
            Name = name,
            FileName = fileName

        };
        Expressions.Add(expression);
        return expression;
    }

    public Expression? FindExpression(string id) =>
        Expressions.FirstOrDefault(e=> e.Id == id);

}


