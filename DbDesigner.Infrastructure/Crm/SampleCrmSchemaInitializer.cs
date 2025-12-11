using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace DbDesigner.Infrastructure.Crm;

public class SampleCrmSchemaInitializer
{
    public async Task InitializeAsync(string connectionString)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        var accountExists = await AccountExistsAsync(connection);
        if (accountExists)
        {
            return;
        }

        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = BuildCreateScript();

        await command.ExecuteNonQueryAsync();
        await transaction.CommitAsync();
    }

    private static async Task<bool> AccountExistsAsync(SqliteConnection connection)
    {
        await using var checkCommand = connection.CreateCommand();
        checkCommand.CommandText = "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = 'Account' LIMIT 1;";
        var result = await checkCommand.ExecuteScalarAsync();
        return result != null;
    }

    private static string BuildCreateScript() =>
        @"
PRAGMA foreign_keys = ON;

CREATE TABLE Account (
    Id INTEGER PRIMARY KEY,
    Name TEXT NOT NULL,
    Nip TEXT NULL,
    Industry TEXT NULL,
    CreatedAt TEXT NOT NULL
);

CREATE TABLE Contact (
    Id INTEGER PRIMARY KEY,
    AccountId INTEGER NOT NULL,
    FirstName TEXT NOT NULL,
    LastName TEXT NOT NULL,
    Email TEXT NULL,
    Phone TEXT NULL,
    FOREIGN KEY (AccountId) REFERENCES Account(Id)
);

CREATE TABLE Opportunity (
    Id INTEGER PRIMARY KEY,
    AccountId INTEGER NOT NULL,
    Name TEXT NOT NULL,
    Stage TEXT NOT NULL,
    EstimatedValue REAL NOT NULL,
    CloseDate TEXT NULL,
    FOREIGN KEY (AccountId) REFERENCES Account(Id)
);

CREATE TABLE Activity (
    Id INTEGER PRIMARY KEY,
    AccountId INTEGER NOT NULL,
    ContactId INTEGER NULL,
    Type TEXT NOT NULL,
    Subject TEXT NOT NULL,
    DueDate TEXT NULL,
    Completed INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (AccountId) REFERENCES Account(Id),
    FOREIGN KEY (ContactId) REFERENCES Contact(Id)
);

CREATE TABLE User (
    Id INTEGER PRIMARY KEY,
    Login TEXT NOT NULL,
    FullName TEXT NOT NULL,
    Role TEXT NOT NULL
);

CREATE VIEW IF NOT EXISTS vwAccountSummary AS
SELECT 
    A.Id,
    A.Name,
    A.Industry,
    (SELECT COUNT(*) FROM Opportunity O WHERE O.AccountId = A.Id) AS OpportunitiesCount,
    (SELECT COUNT(*) FROM Activity Ac WHERE Ac.AccountId = A.Id AND Ac.Completed = 0) AS OpenActivities
FROM Account A;

CREATE VIEW IF NOT EXISTS vwOpportunityPipeline AS
SELECT
    O.Id,
    O.Name,
    O.Stage,
    O.EstimatedValue,
    O.CloseDate,
    A.Name AS AccountName
FROM Opportunity O
JOIN Account A ON A.Id = O.AccountId;

CREATE VIEW IF NOT EXISTS vwActivityDetails AS
SELECT
    Ac.Id,
    Ac.Type,
    Ac.Subject,
    Ac.DueDate,
    Ac.Completed,
    A.Name AS AccountName,
    C.FirstName || ' ' || C.LastName AS ContactName
FROM Activity Ac
JOIN Account A ON A.Id = Ac.AccountId
LEFT JOIN Contact C ON C.Id = Ac.ContactId;
";
}
