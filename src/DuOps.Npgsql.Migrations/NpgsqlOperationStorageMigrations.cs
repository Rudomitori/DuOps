using System.Reflection;

namespace DuOps.Npgsql.Migrations;

public static class NpgsqlOperationStorageMigrations
{
    private static string GetResourceName(string migrationName) =>
        $"DuOps.Npgsql.Migrations.Migrations.{migrationName}.sql";

    private static readonly string[] MigrationNames = ["001_Init"];

    public static IEnumerable<string> GetMigrations()
    {
        foreach (var migrationName in MigrationNames)
        {
            var resourceName = GetResourceName(migrationName);
            using var resourceStream =
                Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
                ?? throw new NullReferenceException($"Resource {resourceName} as not found");

            var streamReader = new StreamReader(resourceStream);
            var migration = streamReader.ReadToEnd();

            yield return migration;
        }
    }
}
