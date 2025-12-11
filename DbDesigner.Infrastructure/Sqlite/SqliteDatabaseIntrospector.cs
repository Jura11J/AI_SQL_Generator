using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using DbDesigner.Core.Introspection;
using DbDesigner.Core.Schema;
using Microsoft.Data.Sqlite;
using SchemaIndex = DbDesigner.Core.Schema.Index;

namespace DbDesigner.Infrastructure.Sqlite;

public class SqliteDatabaseIntrospector : IDatabaseIntrospector
{
    public async Task<DatabaseSchema> LoadSchemaAsync(DbConnection connection)
    {
        if (connection is not SqliteConnection sqliteConnection)
        {
            throw new ArgumentException("SQLite introspector wymaga polaczenia SqliteConnection.", nameof(connection));
        }

        if (sqliteConnection.State != ConnectionState.Open)
        {
            await sqliteConnection.OpenAsync();
        }

        var schema = new DatabaseSchema();

        var tableNames = await LoadNamesAsync(sqliteConnection, "table");
        foreach (var tableName in tableNames)
        {
            var table = new Table { Name = tableName };
            await LoadColumnsAsync(sqliteConnection, table);
            await LoadForeignKeysAsync(sqliteConnection, table);
            await LoadIndexesAsync(sqliteConnection, table);
            schema.Tables.Add(table);
        }

        var views = await LoadViewsAsync(sqliteConnection);
        schema.Views.AddRange(views);

        return schema;
    }

    private static async Task<List<string>> LoadNamesAsync(SqliteConnection connection, string objectType)
    {
        var names = new List<string>();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = @"SELECT name FROM sqlite_master WHERE type = $type AND name NOT LIKE 'sqlite_%' ORDER BY name;";
        cmd.Parameters.AddWithValue("$type", objectType);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            names.Add(reader.GetString(0));
        }

        return names;
    }

    private static async Task LoadColumnsAsync(SqliteConnection connection, Table table)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info({EscapeIdentifier(table.Name)});";

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var column = new Column
            {
                Name = reader.GetString(reader.GetOrdinal("name")),
                DataType = reader.GetString(reader.GetOrdinal("type")),
                IsNullable = reader.GetInt32(reader.GetOrdinal("notnull")) == 0,
                IsPrimaryKey = reader.GetInt32(reader.GetOrdinal("pk")) > 0
            };

            table.Columns.Add(column);
        }
    }

    private static async Task LoadForeignKeysAsync(SqliteConnection connection, Table table)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"PRAGMA foreign_key_list({EscapeIdentifier(table.Name)});";

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var fk = new ForeignKey
            {
                Name = $"fk_{table.Name}_{reader.GetInt32(reader.GetOrdinal("id"))}",
                FromTable = table.Name,
                FromColumn = reader.GetString(reader.GetOrdinal("from")),
                ToTable = reader.GetString(reader.GetOrdinal("table")),
                ToColumn = reader.GetString(reader.GetOrdinal("to"))
            };

            table.ForeignKeys.Add(fk);
        }
    }

    private static async Task LoadIndexesAsync(SqliteConnection connection, Table table)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"PRAGMA index_list({EscapeIdentifier(table.Name)});";

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var indexName = reader.GetString(reader.GetOrdinal("name"));
            var origin = reader.GetString(reader.GetOrdinal("origin"));

            // Pomijamy indeks PK, bo informacje mamy na poziomie kolumn.
            if (origin.Equals("pk", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var index = new SchemaIndex
            {
                Name = indexName,
                IsUnique = reader.GetInt32(reader.GetOrdinal("unique")) == 1
            };

            var columns = await LoadIndexColumnsAsync(connection, indexName);
            index.Columns.AddRange(columns);

            table.Indexes.Add(index);
        }
    }

    private static async Task<List<string>> LoadIndexColumnsAsync(SqliteConnection connection, string indexName)
    {
        var columns = new List<string>();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"PRAGMA index_info({EscapeIdentifier(indexName)});";

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            columns.Add(reader.GetString(reader.GetOrdinal("name")));
        }

        return columns;
    }

    private static async Task<List<View>> LoadViewsAsync(SqliteConnection connection)
    {
        var views = new List<View>();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = @"SELECT name, sql FROM sqlite_master WHERE type = 'view' AND name NOT LIKE 'sqlite_%' ORDER BY name;";

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            views.Add(new View
            {
                Name = reader.GetString(0),
                Definition = reader.IsDBNull(1) ? string.Empty : reader.GetString(1)
            });
        }

        return views;
    }

    private static string EscapeIdentifier(string name) => $"\"{name.Replace("\"", "\"\"")}\"";
}
