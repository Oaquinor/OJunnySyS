using System.Globalization;
using OJunnySyS.Domain.Sales;

namespace OJunnySyS.Web.Presentation;

public static class DisplayFormat
{
    private static readonly CultureInfo DominicanSpanish = CultureInfo.GetCultureInfo("es-DO");

    public static string Money(decimal amount, bool showCents = false) =>
        amount.ToString(showCents ? "C2" : "C0", DominicanSpanish);

    public static string Initials(string value) =>
        string.Join("", value.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(2).Select(word => char.ToUpperInvariant(word[0])));

    public static string PaymentLabel(PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => "Efectivo",
        PaymentMethod.Card => "Tarjeta",
        PaymentMethod.Transfer => "Transferencia",
        PaymentMethod.Credit => "Crédito",
        _ => "Otro"
    };
}
