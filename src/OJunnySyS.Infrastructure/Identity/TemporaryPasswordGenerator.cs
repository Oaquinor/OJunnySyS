using System.Security.Cryptography;

namespace OJunnySyS.Infrastructure.Identity;

public static class TemporaryPasswordGenerator
{
    private const string Upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Lower = "abcdefghijkmnopqrstuvwxyz";
    private const string Digits = "23456789";
    private const string Symbols = "!@#$%*-_+";
    private const string All = Upper + Lower + Digits + Symbols;

    public static string Generate(int length = 18)
    {
        if (length < 12) throw new ArgumentOutOfRangeException(nameof(length), "La contraseña temporal debe tener al menos 12 caracteres.");
        var chars = new char[length];
        chars[0] = Pick(Upper); chars[1] = Pick(Lower); chars[2] = Pick(Digits); chars[3] = Pick(Symbols);
        for (var i = 4; i < chars.Length; i++) chars[i] = Pick(All);
        for (var i = chars.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }
        return new string(chars);
    }

    private static char Pick(string source) => source[RandomNumberGenerator.GetInt32(source.Length)];
}
