using Microsoft.EntityFrameworkCore;

namespace CampusEstateLiving.Data;

/// <summary>
/// Keeps campusestate.db aligned with the current model without SQL Server migrations.
/// Safe to run on every start.
/// </summary>
public static class SchemaEnsure
{
    public static async Task ApplyAsync(ApplicationDbContext db)
    {
        await db.Database.OpenConnectionAsync();
        try
        {
            await db.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = ON;");
            await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode = WAL;");

            await AddColumnIfMissing(db, "Payments", "ProcessedAt", "TEXT NULL");
            await AddColumnIfMissing(db, "Payments", "ProcessingNotes", "TEXT NULL");
            await AddColumnIfMissing(db, "Persons", "DateOfBirth", "TEXT NULL");
            await AddColumnIfMissing(db, "Persons", "Gender", "TEXT NULL");
            await AddColumnIfMissing(db, "Persons", "HasDisability", "INTEGER NOT NULL DEFAULT 0");
            await AddColumnIfMissing(db, "Persons", "DisabilityDetails", "TEXT NULL");
            await AddColumnIfMissing(db, "Users", "LastLoginAt", "TEXT NULL");
            await AddColumnIfMissing(db, "Rooms", "SharingGender", "TEXT NULL");

            await db.Database.ExecuteSqlRawAsync("""
                CREATE TABLE IF NOT EXISTS ResidentVehicles (
                    VehicleId INTEGER NOT NULL CONSTRAINT PK_ResidentVehicles PRIMARY KEY AUTOINCREMENT,
                    UserId INTEGER NOT NULL,
                    RegistrationNumber TEXT NOT NULL,
                    MakeAndModel TEXT NULL,
                    RegisteredAt TEXT NOT NULL,
                    CONSTRAINT FK_ResidentVehicles_Users_UserId FOREIGN KEY (UserId) REFERENCES Users (Id) ON DELETE CASCADE,
                    CONSTRAINT CK_ResidentVehicles_Registration CHECK(length(RegistrationNumber) BETWEEN 2 AND 12)
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_ResidentVehicles_UserId_RegistrationNumber ON ResidentVehicles(UserId, RegistrationNumber);
                CREATE INDEX IF NOT EXISTS IX_ResidentVehicles_UserId ON ResidentVehicles(UserId);
                """);

            await db.Database.ExecuteSqlRawAsync("""
                CREATE TABLE IF NOT EXISTS Announcements (
                    AnnouncementId INTEGER NOT NULL CONSTRAINT PK_Announcements PRIMARY KEY AUTOINCREMENT,
                    Title TEXT NOT NULL,
                    Body TEXT NOT NULL,
                    Audience TEXT NOT NULL,
                    IsPinned INTEGER NOT NULL,
                    PublishedAt TEXT NOT NULL,
                    ExpiresAt TEXT NULL,
                    CreatedByUserId INTEGER NOT NULL,
                    CONSTRAINT FK_Announcements_Users_CreatedByUserId
                        FOREIGN KEY (CreatedByUserId) REFERENCES Users (Id) ON DELETE CASCADE
                );
                """);

            await db.Database.ExecuteSqlRawAsync("""
                CREATE TABLE IF NOT EXISTS MaintenanceRequests (
                    RequestId INTEGER NOT NULL CONSTRAINT PK_MaintenanceRequests PRIMARY KEY AUTOINCREMENT,
                    UserId INTEGER NOT NULL,
                    RoomId INTEGER NULL,
                    Category TEXT NOT NULL,
                    Title TEXT NOT NULL,
                    Description TEXT NOT NULL,
                    Status TEXT NOT NULL,
                    Priority TEXT NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL,
                    ResolvedAt TEXT NULL,
                    StaffNotes TEXT NULL,
                    CONSTRAINT FK_MaintenanceRequests_Users_UserId
                        FOREIGN KEY (UserId) REFERENCES Users (Id) ON DELETE CASCADE,
                    CONSTRAINT FK_MaintenanceRequests_Rooms_RoomId
                        FOREIGN KEY (RoomId) REFERENCES Rooms (RoomId) ON DELETE SET NULL
                );
                """);

            await db.Database.ExecuteSqlRawAsync(
                "CREATE INDEX IF NOT EXISTS IX_Announcements_PublishedAt ON Announcements (PublishedAt);");
            await db.Database.ExecuteSqlRawAsync(
                "CREATE INDEX IF NOT EXISTS IX_MaintenanceRequests_UserId ON MaintenanceRequests (UserId);");
            await db.Database.ExecuteSqlRawAsync(
                "CREATE INDEX IF NOT EXISTS IX_MaintenanceRequests_Status ON MaintenanceRequests (Status);");
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }

    private static async Task AddColumnIfMissing(
        ApplicationDbContext db, string table, string column, string definition)
    {
        var exists = false;
        await using var cmd = db.Database.GetDbConnection().CreateCommand();
        cmd.CommandText = $"PRAGMA table_info({table})";
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase))
            {
                exists = true;
                break;
            }
        }

        if (!exists)
        {
            if (table == "Payments" && column == "ProcessedAt")
                await db.Database.ExecuteSqlRawAsync("ALTER TABLE Payments ADD COLUMN ProcessedAt TEXT NULL;");
            else if (table == "Payments" && column == "ProcessingNotes")
                await db.Database.ExecuteSqlRawAsync("ALTER TABLE Payments ADD COLUMN ProcessingNotes TEXT NULL;");
            else
            {
                // These identifiers/definitions come only from the fixed schema-upgrade calls above.
                // SQLite does not support parameters in ALTER TABLE column definitions.
                var allowed = (table, column, definition) switch
                {
                    ("Persons", "DateOfBirth", "TEXT NULL") => true,
                    ("Persons", "Gender", "TEXT NULL") => true,
                    ("Persons", "HasDisability", "INTEGER NOT NULL DEFAULT 0") => true,
                    ("Persons", "DisabilityDetails", "TEXT NULL") => true,
                    ("Users", "LastLoginAt", "TEXT NULL") => true,
                    ("Rooms", "SharingGender", "TEXT NULL") => true,
                    _ => false
                };
                if (!allowed) throw new InvalidOperationException($"Unsupported schema upgrade for {table}.{column}.");
#pragma warning disable EF1002 // SQLite ALTER TABLE column definitions cannot be parameterized; values are allow-listed above.
                await db.Database.ExecuteSqlRawAsync($"ALTER TABLE {table} ADD COLUMN {column} {definition};");
#pragma warning restore EF1002
            }
        }
    }
}
