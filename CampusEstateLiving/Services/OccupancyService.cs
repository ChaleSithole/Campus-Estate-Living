using CampusEstateLiving.Data;
using Microsoft.EntityFrameworkCore;
using CampusEstateLiving.Models;

namespace CampusEstateLiving.Services;

public class OccupancyService
{
    private readonly ApplicationDbContext _db;

    public OccupancyService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<(bool Ok, string? Error)> CanAssignAsync(
        int userId,
        int roomId,
        DateTime moveIn,
        DateTime? moveOut)
    {
        if (moveOut.HasValue && moveOut.Value < moveIn)
        {
            return (
                false,
                "Move-out date cannot be earlier than move-in date."
            );
        }

        var hasActiveAssignment = await _db.RoomAssignments
            .AnyAsync(a =>
                a.UserId == userId &&
                a.MoveOutDate == null);

        if (hasActiveAssignment)
        {
            return (
                false,
                "This user already has an active room assignment."
            );
        }

        var room = await _db.Rooms
            .Include(r => r.RoomType)
            .Include(r => r.Property)
            .FirstOrDefaultAsync(r => r.RoomId == roomId);

        if (room == null)
        {
            return (false, "Room was not found.");
        }

        if (room.RoomType == null || room.RoomType.Capacity <= 0)
        {
            return (
                false,
                "This room does not have a valid capacity."
            );
        }

        var activeOccupants = await _db.RoomAssignments
            .CountAsync(a =>
                a.RoomId == roomId &&
                a.MoveOutDate == null);

        if (activeOccupants >= room.RoomType.Capacity)
        {
            return (
                false,
                $"This {room.RoomType.RoomTypeName.ToLower()} room is already at capacity ({room.RoomType.Capacity})."
            );
        }

        if (string.Equals(room.RoomType.RoomTypeName, RoomTypeNames.Sharing, StringComparison.OrdinalIgnoreCase))
        {
            var gender = await _db.Users.Where(u => u.Id == userId).Select(u => u.Person.Gender).FirstOrDefaultAsync();
            if (string.IsNullOrWhiteSpace(gender))
                return (false, "Set the resident's gender in their profile before assigning a sharing room.");

            var currentGender = await _db.RoomAssignments
                .Where(a => a.RoomId == roomId && a.MoveOutDate == null)
                .Select(a => a.User.Person.Gender)
                .FirstOrDefaultAsync();
            var roomGender = room.SharingGender ?? currentGender;
            if (!string.IsNullOrWhiteSpace(roomGender) && !string.Equals(roomGender, gender, StringComparison.OrdinalIgnoreCase))
                return (false, "This room already has a resident of a different gender and cannot be shared.");
        }

        return (true, null);
    }

    public async Task<bool> EstablishSharingGenderAsync(int roomId, string? gender)
    {
        var room = await _db.Rooms.Include(r => r.RoomType).FirstOrDefaultAsync(r => r.RoomId == roomId);
        if (room == null || !string.Equals(room.RoomType.RoomTypeName, RoomTypeNames.Sharing, StringComparison.OrdinalIgnoreCase)) return true;
        var existingGender = room.SharingGender ?? await _db.RoomAssignments
            .Where(a => a.RoomId == roomId && a.MoveOutDate == null)
            .Select(a => a.User.Person.Gender).FirstOrDefaultAsync();
        if (!string.IsNullOrWhiteSpace(existingGender) && !string.Equals(existingGender, gender, StringComparison.OrdinalIgnoreCase)) return false;
        room.SharingGender ??= gender;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> HasActiveAssignmentAsync(int userId)
    {
        return await _db.RoomAssignments
            .AnyAsync(a =>
                a.UserId == userId &&
                a.MoveOutDate == null);
    }

    public async Task<int> GetActiveOccupantCountAsync(int roomId)
    {
        return await _db.RoomAssignments
            .CountAsync(a =>
                a.RoomId == roomId &&
                a.MoveOutDate == null);
    }

    public async Task<int?> GetRoomCapacityAsync(int roomId)
    {
        return await _db.Rooms
            .Where(r => r.RoomId == roomId)
            .Select(r => (int?)r.RoomType.Capacity)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> IsRoomAvailableAsync(int roomId)
    {
        var room = await _db.Rooms
            .Include(r => r.RoomType)
            .FirstOrDefaultAsync(r => r.RoomId == roomId);

        if (room == null || room.RoomType == null)
        {
            return false;
        }

        var activeOccupants = await GetActiveOccupantCountAsync(roomId);

        return activeOccupants < room.RoomType.Capacity;
    }
}
