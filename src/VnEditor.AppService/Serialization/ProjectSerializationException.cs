using System;
using System.Collections.Generic;
using System.Text;

namespace VnEditor.AppService.Serialization;

/// <summary>Errore durante la conversione del formato persistente di un progetto.</summary>
public sealed class ProjectSerializationException : Exception
{
    public enum FailureKind
    {
        InvalidContent,
        UnsupportedVersion,
        UnknownType
    }

    public FailureKind Kind { get;}

    public ProjectSerializationException(
        FailureKind kind, string  message , 
        Exception? innerException = null ) : base( message, innerException)
    {
        this.Kind = kind;
    }
}
