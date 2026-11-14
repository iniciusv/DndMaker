namespace DndMaker.Domain.Common;

/// <summary>
/// Uma regra do domínio foi violada. A interface só oferece escolhas válidas,
/// então esta exceção indica um bug, não um erro do usuário.
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException()
    {
    }

    public DomainException(string message)
        : base(message)
    {
    }

    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
