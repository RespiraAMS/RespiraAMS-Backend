namespace Respira.ServiceDefaults.Utils.FullTextSearch
{
    public static class FullTextSearchUtil
    {
        public const string ConfigurationName = "respira_vi";

        /// <summary>
        /// Turn a free text search term into a PostgreSQL tsquery string that
        /// matches by prefix, e.g. "nguyen va" becomes "nguyen:* &amp; va:*".
        /// Words are stripped of tsquery operators so the input can never break
        /// the query, and the <c>respira_vi</c> configuration folds accents on
        /// both sides (stored vector and query).
        /// </summary>
        public static string BuildPrefixQuery(string searchTerm)
        {
            // tsquery treats & | ! ( ) : * < > ' \ as operators. Replace everything that
            // is not a letter, a digit or a hyphen with a separator so raw user input can
            // never inject an operator. Hyphens are kept because the text search parser
            // indexes hyphenated words as a whole lexeme plus its parts
            // (e.g. "beta-lactams" -> 'beta', 'beta-lactams', 'lactams'), which is what
            // makes prefix matching on "beta-lactam" work.
            var sanitized = string.Concat(
                searchTerm.Select(c => char.IsLetterOrDigit(c) || c == '-' ? c : ' '));

            var lexemes = sanitized
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                // At least 2 alphanumeric characters, to keep the existing "avoid one
                // single character -> noise" rule and to drop leftovers like "--"
                .Where(word => word.Count(char.IsLetterOrDigit) > 1)
                .Select(word => $"{word}:*");

            return string.Join(" & ", lexemes);
        }
    }
}
