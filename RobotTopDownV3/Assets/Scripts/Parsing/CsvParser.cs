using System.Collections.Generic;
using System.IO;

public static class CsvParser
{
    public static List<Dictionary<string, string>> Parse ( string path )
    {
        List<Dictionary<string, string>> rows = new();

        string[] lines = File.ReadAllLines(path);

        string[] headers = lines[0].Split(',');

        for (int i = 1; i < lines.Length; i++)
        {
            string[] values = lines[i].Split(',');

            Dictionary<string, string> row = new();

            for (int j = 0; j < headers.Length; j++)
            {
                row[headers[j]] = values[j];
            }

            rows.Add(row);
        }

        return rows;
    }

}