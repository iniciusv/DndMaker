using System.Globalization;
using DndMaker.Domain.Combat;

namespace DndMaker.Desktop.Formatting;

/// <summary>Números como a página original mostrava: pt-BR, uma casa decimal, porcentagem inteira e sinal tipográfico.</summary>
internal static class Format
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    private static readonly Dictionary<string, string> AbilityNames = new(StringComparer.Ordinal)
    {
        ["FOR"] = "Força",
        ["DES"] = "Destreza",
        ["CON"] = "Constituição",
        ["INT"] = "Inteligência",
        ["SAB"] = "Sabedoria",
        ["CAR"] = "Carisma",
    };

    /// <summary>Até uma casa decimal, com vírgula: <c>12,4</c>, <c>18</c>.</summary>
    public static string Decimal(double value) => value.ToString("0.#", PtBr);

    /// <summary>Porcentagem inteira, arredondando 0,5 para cima como o JavaScript: <c>55%</c>.</summary>
    public static string Percent(double probability) =>
        Math.Round(probability * 100, MidpointRounding.AwayFromZero).ToString("0", PtBr) + "%";

    /// <summary>Com sinal: <c>+3</c>, <c>−1</c>.</summary>
    public static string Signed(int value) => SignedNumber.Format(value);

    public static string Number(int value) => value.ToString(PtBr);

    /// <summary>Nome do atributo pela sigla: SAB → Sabedoria.</summary>
    public static string AbilityName(string abbreviation) =>
        AbilityNames.TryGetValue(abbreviation, out var name) ? name : abbreviation;

    /// <summary>Numeral romano das cartas.</summary>
    public static string Roman(int index) => index switch
    {
        0 => "I",
        1 => "II",
        2 => "III",
        _ => Number(index + 1),
    };
}
