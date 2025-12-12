using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DbDesigner.Core.Schema;
using DbDesigner.Core.SchemaChanges;

namespace DbDesigner.AI;

public class StubChangeProposalService : IChangeProposalService
{
    public async Task<IReadOnlyList<SchemaChange>> ProposeChangesAsync(string specification, DatabaseSchema? currentSchema)
    {
        var proposals = new List<SchemaChange>();
        var schema = currentSchema ?? new DatabaseSchema();

        var hasAccount = HasTable(schema, "Account");
        var hasOpportunity = HasTable(schema, "Opportunity");

        if (!hasOpportunity)
        {
            proposals.Add(BuildOpportunityTableChange(hasAccount));
        }
        else if (!HasColumn(schema, "Opportunity", "Probability"))
        {
            proposals.Add(BuildProbabilityColumnChange());
        }

        proposals.Add(BuildPipelineViewChange(HasView(schema, "vwOpportunityPipeline")));

        if (proposals.Count == 0)
        {
            proposals.Add(BuildDefaultTable());
        }

        await AiCallLogger.LogAsync(new AiCallLogEntry
        {
            Backend = "LocalStub",
            Specification = specification,
            TableCount = schema.Tables.Count,
            ViewCount = schema.Views.Count,
            Changes = proposals
        });

        return proposals;
    }

    private static bool HasTable(DatabaseSchema schema, string tableName) =>
        schema.Tables.Any(t => string.Equals(t.Name, tableName, StringComparison.OrdinalIgnoreCase));

    private static bool HasColumn(DatabaseSchema schema, string tableName, string columnName) =>
        schema.Tables.Any(t =>
            string.Equals(t.Name, tableName, StringComparison.OrdinalIgnoreCase) &&
            t.Columns.Any(c => string.Equals(c.Name, columnName, StringComparison.OrdinalIgnoreCase)));

    private static bool HasView(DatabaseSchema schema, string viewName) =>
        schema.Views.Any(v => string.Equals(v.Name, viewName, StringComparison.OrdinalIgnoreCase));

    private static SchemaChange BuildOpportunityTableChange(bool hasAccount) =>
        new CreateTableChange
        {
            Description = hasAccount
                ? "Dodaj tabele Opportunity powiazana z Account."
                : "Dodaj tabele Opportunity (wymaga relacji do Account).",
            TableName = "Opportunity",
            Columns =
            {
                new ColumnDefinition { Name = "Id", DataType = "INTEGER", IsPrimaryKey = true, IsNullable = false },
                new ColumnDefinition { Name = "Name", DataType = "TEXT", IsNullable = false },
                new ColumnDefinition { Name = "Stage", DataType = "TEXT", IsNullable = false },
                new ColumnDefinition { Name = "EstimatedValue", DataType = "REAL", IsNullable = false },
                new ColumnDefinition { Name = "CloseDate", DataType = "TEXT", IsNullable = true },
                new ColumnDefinition { Name = "AccountId", DataType = "INTEGER", IsNullable = false }
            }
        };

    private static SchemaChange BuildProbabilityColumnChange() =>
        new AddColumnChange
        {
            Description = "Dodaj kolumne Probability do tabeli Opportunity.",
            TableName = "Opportunity",
            Column = new ColumnDefinition
            {
                Name = "Probability",
                DataType = "REAL",
                IsNullable = false
            }
        };

    private static SchemaChange BuildPipelineViewChange(bool viewExists)
    {
        var description = viewExists
            ? "Odswiez widok vwOpportunityPipeline (propozycja aktualizacji)."
            : "Dodaj widok vwOpportunityPipeline z podstawowymi danymi.";

        return new CreateViewChange
        {
            Description = description,
            ViewName = "vwOpportunityPipeline",
            SqlDefinition = @"
SELECT
    O.Id,
    O.Name,
    O.Stage,
    O.EstimatedValue,
    O.CloseDate,
    A.Name AS AccountName
FROM Opportunity O
JOIN Account A ON A.Id = O.AccountId"
        };
    }

    private static SchemaChange BuildDefaultTable() =>
        new CreateTableChange
        {
            Description = "Dodaj tabele ExampleEntity z typowymi kolumnami.",
            TableName = "ExampleEntity",
            Columns =
            {
                new ColumnDefinition { Name = "Id", DataType = "INTEGER", IsPrimaryKey = true, IsNullable = false },
                new ColumnDefinition { Name = "Name", DataType = "TEXT", IsNullable = false },
                new ColumnDefinition { Name = "CreatedAt", DataType = "TEXT", IsNullable = false }
            }
        };
}
