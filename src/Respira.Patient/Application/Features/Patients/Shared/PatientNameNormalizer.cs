namespace Application.Features.Patients.Shared;

public static class PatientNameNormalizer
{
    public static string Normalize(string fullname)
    {
        fullname = fullname.Trim();

        return string.Join(" ", fullname
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Select(word => char.ToUpper(word[0]) + word[1..].ToLower())
        );
    }
}