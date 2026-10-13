namespace Respira.Patient.Domain.Utils
{
    public static class NameNormalizer
    {
        public static string NameNormalize(string name)
        {
            name = name.Trim();

            return string.Join(" ", name
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                .Select(word => char.ToUpper(word[0]) + word[1..].ToLower())
            );
        }
    }
}
