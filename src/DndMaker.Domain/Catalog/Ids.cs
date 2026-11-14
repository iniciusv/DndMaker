namespace DndMaker.Domain.Catalog;

/// <summary>Identifica uma build (ex.: "fwl").</summary>
public readonly record struct ArchetypeId
{
    public ArchetypeId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public string Value { get; }

    public override string ToString() => Value ?? string.Empty;
}

/// <summary>Identifica uma variante de build (ex.: "dueling").</summary>
public readonly record struct VariantId
{
    public VariantId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public string Value { get; }

    public override string ToString() => Value ?? string.Empty;
}

/// <summary>Identifica uma espécie (ex.: "gnomo").</summary>
public readonly record struct SpeciesId
{
    public SpeciesId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public string Value { get; }

    public override string ToString() => Value ?? string.Empty;
}

/// <summary>Identifica um antecedente (ex.: "haunted").</summary>
public readonly record struct BackgroundId
{
    public BackgroundId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public string Value { get; }

    public override string ToString() => Value ?? string.Empty;
}

/// <summary>Identifica um talento (ex.: "alert").</summary>
public readonly record struct FeatId
{
    public FeatId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public string Value { get; }

    public override string ToString() => Value ?? string.Empty;
}

/// <summary>Identifica uma classe (ex.: "fighter").</summary>
public readonly record struct ClassId
{
    public ClassId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public string Value { get; }

    public override string ToString() => Value ?? string.Empty;
}
