using Microsoft.EntityFrameworkCore;

namespace CampusEstateLiving.Api.Data;

/// <summary>
/// Ensures Azure SQL has the tables the API model needs.
/// Same database as the web app. Safe if tables already exist.
/// </summary>
public static class SchemaEnsure
{
    public static async Task ApplyAsync(CampusEstateDbContext db)
    {
        // Creates missing tables from the EF model. Does not drop existing data.
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
