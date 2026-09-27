using CampusEstateLiving.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CampusEstateLiving.Data;

public class ApplicationDbContext
    : IdentityDbContext<ApplicationUser, ApplicationRole, int,
        IdentityUserClaim<int>, ApplicationUserRole, IdentityUserLogin<int>,
        IdentityRoleClaim<int>, IdentityUserToken<int>>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Person> Persons => Set<Person>();
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<RoomType> RoomTypes => Set<RoomType>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<RoomAssignment> RoomAssignments => Set<RoomAssignment>();
    public DbSet<EstateAccount> EstateAccounts => Set<EstateAccount>();
    public DbSet<PaymentCategory> PaymentCategories => Set<PaymentCategory>();
    public DbSet<PaymentStatus> PaymentStatuses => Set<PaymentStatus>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Tariff> Tariffs => Set<Tariff>();
    public DbSet<SavedCard> SavedCards => Set<SavedCard>();
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<MaintenanceRequest> MaintenanceRequests => Set<MaintenanceRequest>();
    public DbSet<ResidentVehicle> ResidentVehicles => Set<ResidentVehicle>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(b =>
        {
            b.ToTable("Users");
            b.HasIndex(u => u.UserNumber);
            b.HasIndex(u => u.Email).IsUnique();
            b.HasOne(u => u.Person)
                .WithMany(p => p.Users)
                .HasForeignKey(u => u.PersonId)
                .OnDelete(DeleteBehavior.Restrict);
            b.HasMany(u => u.UserRoles)
                .WithOne(ur => ur.User)
                .HasForeignKey(ur => ur.UserId)
                .IsRequired();
        });

        builder.Entity<ApplicationRole>(b =>
        {
            b.ToTable("Roles");
            b.HasIndex(r => r.Name).IsUnique();
            b.HasMany(r => r.UserRoles)
                .WithOne(ur => ur.Role)
                .HasForeignKey(ur => ur.RoleId)
                .IsRequired();
        });

        builder.Entity<ApplicationUserRole>(b =>
        {
            b.ToTable("UserRoles");
            b.HasKey(r => new { r.UserId, r.RoleId });
            b.Property(r => r.AssignedAt).IsRequired();
        });

        builder.Entity<IdentityUserClaim<int>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<int>>().ToTable("UserLogins");
        builder.Entity<IdentityUserToken<int>>().ToTable("UserTokens");
        builder.Entity<IdentityRoleClaim<int>>().ToTable("RoleClaims");

        builder.Entity<Person>(b =>
        {
            b.ToTable("Persons");
            b.HasIndex(p => new { p.LastName, p.FirstName });
        });

        builder.Entity<Property>(b =>
        {
            b.ToTable(t => t.HasCheckConstraint(
                "CK_Properties_Status",
                "PropertyStatus IN ('Active','Inactive')"));
            b.HasIndex(p => p.BuildingCode).IsUnique();
        });

        builder.Entity<RoomType>(b =>
        {
            b.ToTable(t => t.HasCheckConstraint("CK_RoomTypes_Capacity", "Capacity > 0"));
            b.HasIndex(rt => rt.RoomTypeName).IsUnique();
        });

        builder.Entity<Room>(b =>
        {
            b.HasIndex(r => new { r.PropertyId, r.RoomNumber }).IsUnique();
            b.HasOne(r => r.Property)
                .WithMany(p => p.Rooms)
                .HasForeignKey(r => r.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);
            b.HasOne(r => r.RoomType)
                .WithMany(rt => rt.Rooms)
                .HasForeignKey(r => r.RoomTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ResidentVehicle>(b =>
        {
            b.HasIndex(v => new { v.UserId, v.RegistrationNumber }).IsUnique();
            b.HasIndex(v => v.UserId);
            b.ToTable(t => t.HasCheckConstraint("CK_ResidentVehicles_Registration", "length(RegistrationNumber) BETWEEN 2 AND 12"));
            b.HasOne(v => v.User).WithMany(u => u.Vehicles).HasForeignKey(v => v.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<RoomAssignment>(b =>
        {
            b.ToTable(t => t.HasCheckConstraint(
                "CK_RoomAssignments_Dates",
                "MoveOutDate IS NULL OR MoveOutDate >= MoveInDate"));
            b.HasIndex(a => a.UserId);
            b.HasIndex(a => a.RoomId);
            b.HasOne(a => a.User)
                .WithMany(u => u.RoomAssignments)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            b.HasOne(a => a.Room)
                .WithMany(r => r.Assignments)
                .HasForeignKey(a => a.RoomId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<EstateAccount>(b =>
        {
            b.ToTable(t => t.HasCheckConstraint(
                "CK_EstateAccounts_Status",
                "AccountStatus IN ('Active','Inactive','Suspended')"));
            b.HasIndex(a => a.AccountNumber).IsUnique();
            b.HasIndex(a => a.UserId).IsUnique();
            b.HasOne(a => a.User)
                .WithOne(u => u.EstateAccount)
                .HasForeignKey<EstateAccount>(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PaymentCategory>(b =>
        {
            b.HasIndex(c => c.CategoryName).IsUnique();
        });

        builder.Entity<PaymentStatus>(b =>
        {
            b.HasIndex(s => s.StatusName).IsUnique();
        });

        builder.Entity<Payment>(b =>
        {
            b.ToTable(t => t.HasCheckConstraint("CK_Payments_Amount", "Amount > 0"));
            b.HasIndex(p => p.ReferenceNumber).IsUnique();
            b.HasIndex(p => p.PaymentDate);
            b.HasOne(p => p.Account)
                .WithMany(a => a.Payments)
                .HasForeignKey(p => p.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
            b.HasOne(p => p.Category)
                .WithMany(c => c.Payments)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            b.HasOne(p => p.PaymentStatus)
                .WithMany(s => s.Payments)
                .HasForeignKey(p => p.PaymentStatusId)
                .OnDelete(DeleteBehavior.Restrict);
            b.HasOne(p => p.ProcessedByUser)
                .WithMany(u => u.ProcessedPayments)
                .HasForeignKey(p => p.ProcessedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Notification>(b =>
        {
            b.HasIndex(n => new { n.UserId, n.IsRead });
            b.HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(n => n.Payment)
                .WithMany(p => p.Notifications)
                .HasForeignKey(n => n.PaymentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Tariff>(b =>
        {
            b.ToTable(t => t.HasCheckConstraint("CK_Tariffs_Amount", "Amount > 0"));
        });

        builder.Entity<SavedCard>(b =>
        {
            b.HasIndex(c => c.UserId);
        });

        builder.Entity<BankAccount>(b =>
        {
            b.HasIndex(a => a.UserId);
        });

        builder.Entity<Review>(b =>
        {
            b.ToTable(t => t.HasCheckConstraint("CK_Reviews_Rating", "Rating >= 1 AND Rating <= 5"));
        });

        builder.Entity<Announcement>(b =>
        {
            b.HasIndex(a => a.PublishedAt);
            b.HasOne(a => a.CreatedByUser)
                .WithMany()
                .HasForeignKey(a => a.CreatedByUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<MaintenanceRequest>(b =>
        {
            b.HasIndex(m => m.UserId);
            b.HasIndex(m => m.Status);
            b.HasOne(m => m.User)
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(m => m.Room)
                .WithMany()
                .HasForeignKey(m => m.RoomId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
