namespace OrdoWiki.Web.Helpers;

using System.Globalization;
using System.Text;

public static class StringExtensions
{
    extension(string str)
    {
        public string CreateSlug()
        {
            string normalized = str.Normalize(NormalizationForm.FormKD);
            string ascii = new(normalized
                .Where(c => char.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                .ToArray());

            StringBuilder sb = new(ascii.Length);
            foreach (char c in ascii.ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(c)) sb.Append(c);
                else if(sb.Length > 0 && sb[^1] != '-') sb.Append('-');
            }

            return sb.ToString().Trim('-');
        }
    }
}