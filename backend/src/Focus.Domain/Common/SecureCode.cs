using System.Security.Cryptography;

namespace Focus.Domain.Common;

public static class SecureCode
{
    // Karisabilecek karakterler (0/O, 1/I/L) alfabede yer almaz
    private const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    public static string Generate(string prefix, int length = 8)
    {
        return prefix + RandomNumberGenerator.GetString(Alphabet, length);
    }
}
