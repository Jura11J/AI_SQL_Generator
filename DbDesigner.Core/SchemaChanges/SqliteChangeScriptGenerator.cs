using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DbDesigner.Core.SchemaChanges;

public class SqliteChangeScriptGenerator : ISqlChangeScriptGenerator
{
    public string GenerateScript(IEnumerable<SchemaChange> changes)
    {
        var builder = new StringBuilder();
        foreach (var change in changes)
        {
            switch (change)
            {
                case CreateTableChange createTable:
                    builder.AppendLine(GenerateCreateTable(createTable));
                    break;
                case AddColumnChange addColumn:
                    builder.AppendLine(GenerateAddColumn(addColumn));
                    break;
                case CreateViewChange createView:
                    builder.AppendLine(GenerateCreateView(createView));
                    break;
                default:
                    throw new NotSupportedException($"Unsupported change type: {change.GetType().Name}");
            }

            builder.AppendLine();
        }

        return builder.ToString().Trim();
    }

    private static string GenerateCreateTable(CreateTableChange change)
    {
        var primaryKeys = change.Columns.Where(c => c.IsPrimaryKey).ToList();
        var columnDefinitions = new List<string>();

        foreach (var column in change.Columns)
        {
            var definition = BuildColumnDefinition(column, inlinePrimaryKey: primaryKeys.Count == 1);
            columnDefinitions.Add(definition);
        }

        if (primaryKeys.Count > 1)
        {
            // Przy wielu PK budujemy osobny constraint PRIMARY KEY.
            var pkColumns = string.Join(", ", primaryKeys.Select(c => EscapeIdentifier(c.Name)));
            columnDefinitions.Add($"PRIMARY KEY ({pkColumns})");
        }

        var joined = string.Join(",\n    ", columnDefinitions);
        return $"CREATE TABLE IF NOT EXISTS {EscapeIdentifier(change.TableName)} (\n    {joined}\n);";
    }

    private static string GenerateAddColumn(AddColumnChange change)
    {
        var definition = BuildColumnDefinition(change.Column, inlinePrimaryKey: false);
        return $"ALTER TABLE {EscapeIdentifier(change.TableName)} ADD COLUMN {definition};";
    }

    private static string GenerateCreateView(CreateViewChange change)
    {
        return $"CREATE VIEW IF NOT EXISTS {EscapeIdentifier(change.ViewName)} AS\n{change.SqlDefinition.Trim().TrimEnd(';')};";
    }

    private static string BuildColumnDefinition(ColumnDefinition column, bool inlinePrimaryKey)
    {
        var parts = new List<string>
        {
            EscapeIdentifier(column.Name),
            column.DataType
        };

        if (column.IsPrimaryKey && inlinePrimaryKey)
        {
            parts.Add("PRIMARY KEY");
        }

        parts.Add(column.IsNullable ? "NULL" : "NOT NULL");

        return string.Join(" ", parts);
    }

    private static string EscapeIdentifier(string name) => $"\"{name.Replace("\"", "\"\"")}\"";
}
