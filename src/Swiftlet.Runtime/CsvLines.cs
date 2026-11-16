using Microsoft.VisualBasic.FileIO;

namespace Swiftlet.Runtime;

/// <summary>
/// The CSV behaviour of the "Create CSV Line" and "Read CSV Line" components, shared by both shells.
/// Kept exactly as the Grasshopper 1 components have always behaved (cells are quoted only when they
/// contain the delimiter; reading uses the .NET TextFieldParser).
/// </summary>
internal static class CsvLines
{
    public static string Create(IEnumerable<string> cells, string delimiter)
    {
        string line = string.Empty;
        foreach (string cell in cells)
        {
            string cleanCell = cell;
            if (cell.Contains(delimiter))
            {
                cleanCell = $"\"{cell}\"";
            }

            line += $"{cleanCell}{delimiter}";
        }

        // Drop the trailing delimiter. (Grasshopper 1 used to remove a single character, which left part
        // of a multi-character delimiter behind and threw on an empty cell list.)
        return line.Length >= delimiter.Length ? line[..^delimiter.Length] : line;
    }

    public static List<string> Read(string line, string delimiter)
    {
        using TextFieldParser parser = new(new StringReader(line));
        if (line.Contains('"'))
        {
            parser.HasFieldsEnclosedInQuotes = true;
        }

        parser.SetDelimiters(delimiter);
        List<string> cells = new();
        while (!parser.EndOfData)
        {
            cells.AddRange(parser.ReadFields()!);
        }

        return cells;
    }
}
