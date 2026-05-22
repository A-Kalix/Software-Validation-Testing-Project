// Temporary schema fix file - can be deleted after running once
// This is called during startup to patch any missing columns
using Microsoft.Data.SqlClient;

namespace Backend.Data;

public static class SchemaFixer
{
    public static async Task EnsureColumnsExistAsync(string connectionString)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        var fixes = new[]
        {
            // Add IsPublished to Sections if missing
            """
            IF NOT EXISTS (
                SELECT 1 FROM sys.columns 
                WHERE object_id = OBJECT_ID(N'[dbo].[Sections]') AND name = 'IsPublished'
            )
            BEGIN
                ALTER TABLE [dbo].[Sections] ADD [IsPublished] BIT NOT NULL DEFAULT 0;
            END
            """,
        };

        foreach (var sql in fixes)
        {
            await using var cmd = new SqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        Console.WriteLine("[SchemaFixer] Schema verified/patched successfully.");
    }
}
