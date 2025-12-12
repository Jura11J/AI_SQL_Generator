using System.Text.RegularExpressions;

namespace DbDesigner.AI;

public static class SqlFormatter
{
    public static string Format(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return sql;
        }

        var formatted = sql.Trim();

        formatted = Regex.Replace(formatted, @"\s+from\s+", "\nFROM ", RegexOptions.IgnoreCase);
        formatted = Regex.Replace(formatted, @"\s+where\s+", "\nWHERE ", RegexOptions.IgnoreCase);
        formatted = Regex.Replace(formatted, @"\s+group\s+by\s+", "\nGROUP BY ", RegexOptions.IgnoreCase);
        formatted = Regex.Replace(formatted, @"\s+order\s+by\s+", "\nORDER BY ", RegexOptions.IgnoreCase);
        formatted = Regex.Replace(formatted, @"\bunion all\b\s*select", "UNION ALL\nSELECT", RegexOptions.IgnoreCase);

        return formatted;
    }
}
