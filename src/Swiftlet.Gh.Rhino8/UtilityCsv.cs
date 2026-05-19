using System.Text;

namespace Swiftlet.Gh.Rhino8;

internal static class UtilityCsv
{
    public static string CreateLine(IEnumerable<string> cells, string delimiter)
    {
        string actualDelimiter = string.IsNullOrEmpty(delimiter) ? "," : delimiter;
        return string.Join(actualDelimiter, (cells ?? Array.Empty<string>()).Select(cell => Escape(cell ?? string.Empty, actualDelimiter)));
    }

    public static IReadOnlyList<string> ParseLine(string line, string delimiter)
    {
        string actualDelimiter = string.IsNullOrEmpty(delimiter) ? "," : delimiter;
        if (string.IsNullOrEmpty(line))
        {
            return Array.Empty<string>();
        }

        var cells = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char character = line[i];
            if (character == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (!inQuotes && MatchesDelimiter(line, actualDelimiter, i))
            {
                cells.Add(current.ToString());
                current.Clear();
                i += actualDelimiter.Length - 1;
                continue;
            }

            current.Append(character);
        }

        cells.Add(current.ToString());
        return cells;
    }

    private static string Escape(string cell, string delimiter)
    {
        bool mustQuote = cell.Contains(delimiter, StringComparison.Ordinal) ||
                         cell.Contains('"') ||
                         cell.Contains('\n') ||
                         cell.Contains('\r');

        if (!mustQuote)
        {
            return cell;
        }

        return "\"" + cell.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }

    private static bool MatchesDelimiter(string text, string delimiter, int startIndex)
    {
        if (startIndex + delimiter.Length > text.Length)
        {
            return false;
        }

        for (int i = 0; i < delimiter.Length; i++)
        {
            if (text[startIndex + i] != delimiter[i])
            {
                return false;
            }
        }

        return true;
    }
}
