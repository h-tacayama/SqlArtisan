using System;
using System.Data;
using Microsoft.Data.Sqlite;
using Dapper;
using SqlArtisan;
using static SqlArtisan.Sql;

var connectionString = "Data Source=:memory:";
using var connection = new SqliteConnection(connectionString);
connection.Open();

// Get SQLite version first
string version = (string)connection.ExecuteScalar("SELECT sqlite_version()");
Console.WriteLine($"SQLite version: {version}");

// Parse version
var parts = version.Split('.');
int major = int.Parse(parts[0]);
int minor = int.Parse(parts[1]);
int patch = parts.Length > 2 ? int.Parse(parts[2]) : 0;

Console.WriteLine($"Parsed: {major}.{minor}.{patch}");
Console.WriteLine($"Supports CONCAT (3.35+): {(major > 3 || (major == 3 && minor >= 35))}");

// Try to use CONCAT directly
try
{
    var result = connection.ExecuteScalar("SELECT CONCAT('hello', 'world')");
    Console.WriteLine($"Direct CONCAT result: {result}");
}
catch (SqliteException ex)
{
    Console.WriteLine($"Direct CONCAT failed: {ex.Message}");
}

// Try with SqlArtisan
try
{
    var sql = Select(Concat("hello", "world")).Build(Dbms.Sqlite);
    Console.WriteLine($"SqlArtisan generated: {sql.Text}");
    var result = connection.ExecuteScalar(sql);
    Console.WriteLine($"SqlArtisan CONCAT result: {result}");
}
catch (Exception ex)
{
    Console.WriteLine($"SqlArtisan CONCAT failed: {ex.GetType().Name}: {ex.Message}");
}
