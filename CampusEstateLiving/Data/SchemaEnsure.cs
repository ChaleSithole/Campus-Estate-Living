using Microsoft.EntityFrameworkCore;

namespace CampusEstateLiving.Data;

/// <summary>
/// Ensures Azure SQL schema matches the EF model on startup.
/// </summary>
public static class SchemaEnsure
{
    public static async Task ApplyAsync(ApplicationDbContext db)
    {
        // Creates the database tables from your models if they do not exist yet.
        // Safe for first deploy to Azure SQL.
        await db.Database.EnsureCreatedAsync();
        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.StaffMessages', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.StaffMessages (
                    MessageId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_StaffMessages PRIMARY KEY,
                    SenderUserId int NOT NULL,
                    RecipientUserId int NOT NULL,
                    Body nvarchar(4000) NOT NULL,
                    SentAt datetime2 NOT NULL,
                    ReadAt datetime2 NULL
                );
                CREATE INDEX IX_StaffMessages_SenderUserId_RecipientUserId_SentAt
                    ON dbo.StaffMessages(SenderUserId, RecipientUserId, SentAt);
                CREATE INDEX IX_StaffMessages_RecipientUserId_ReadAt
                    ON dbo.StaffMessages(RecipientUserId, ReadAt);
            END
            """);
    }
}
