using ItAssetManagement.Constants;
using ItAssetManagement.Data;
using ItAssetManagement.Domain;
using ItAssetManagement.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ItAssetManagement.Services;

public class UserService(
    AppDbContext db,
    IPasswordHasher hasher,
    ILogger<UserService> logger) : IUserService
{
    private const string UniqueViolation = "23505";

    /// <summary>Postgres SQLSTATE for foreign_key_violation.</summary>
    private const string ForeignKeyViolation = "23503";

    public async Task<IReadOnlyList<UserListItemViewModel>> GetAllAsync(CancellationToken ct = default) =>
        await db.Users
            .AsNoTracking()
            .OrderByDescending(u => u.Role == Roles.Admin)
            .ThenBy(u => u.FullName)
            .Select(u => new UserListItemViewModel
            {
                Id = u.Id,
                Username = u.Username,
                FullName = u.FullName,
                Role = u.Role,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                AssignedAssetCount = u.AssignedAssets.Count
            })
            .ToListAsync(ct);

    public async Task<UserFormViewModel?> GetForEditAsync(int id, CancellationToken ct = default) =>
        await db.Users
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UserFormViewModel
            {
                Id = u.Id,
                Username = u.Username,
                FullName = u.FullName,
                Role = u.Role,
                IsActive = u.IsActive
                // Password is deliberately not populated: the hash is never sent to a browser,
                // and a blank field on edit means "leave the password alone".
            })
            .SingleOrDefaultAsync(ct);

    public async Task<ServiceResult<int>> CreateAsync(UserFormViewModel model, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(model.Password))
        {
            return ServiceResult<int>.Fail("A password is required when creating a user.");
        }

        if (!Roles.All.Contains(model.Role))
        {
            return ServiceResult<int>.Fail("That role is not recognised.");
        }

        var user = new User
        {
            Username = model.Username.Trim(),
            FullName = model.FullName.Trim(),
            Role = model.Role,
            IsActive = model.IsActive,
            PasswordHash = hasher.Hash(model.Password),
            CreatedAt = DateTime.UtcNow
        };

        db.Users.Add(user);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsSqlState(ex, UniqueViolation))
        {
            db.Entry(user).State = EntityState.Detached;
            return ServiceResult<int>.Fail($"Username '{model.Username}' is already taken.");
        }

        logger.LogInformation("User {Username} created with role {Role}.", user.Username, user.Role);
        return ServiceResult<int>.Ok(user.Id);
    }

    public async Task<ServiceResult> UpdateAsync(
        UserFormViewModel model, int actingUserId, CancellationToken ct = default)
    {
        if (!Roles.All.Contains(model.Role))
        {
            return ServiceResult.Fail("That role is not recognised.");
        }

        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == model.Id, ct);

        if (user is null)
        {
            return ServiceResult.Fail("That user no longer exists.");
        }

        // Guard against an administrator locking themselves out of the admin pages by
        // demoting or deactivating their own account mid-session.
        if (user.Id == actingUserId)
        {
            if (model.Role != Roles.Admin)
            {
                return ServiceResult.Fail("You cannot remove your own administrator role.");
            }

            if (!model.IsActive)
            {
                return ServiceResult.Fail("You cannot deactivate your own account.");
            }
        }

        if (user.Role == Roles.Admin && model.Role != Roles.Admin && !await HasAnotherActiveAdminAsync(user.Id, ct))
        {
            return ServiceResult.Fail("This is the last active administrator, so the role cannot be removed.");
        }

        user.Username = model.Username.Trim();
        user.FullName = model.FullName.Trim();
        user.Role = model.Role;
        user.IsActive = model.IsActive;

        // Blank means "leave it alone". Only a non-empty box replaces the stored hash.
        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            user.PasswordHash = hasher.Hash(model.Password);
            logger.LogInformation("Password reset for user {Username}.", user.Username);
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsSqlState(ex, UniqueViolation))
        {
            return ServiceResult.Fail($"Username '{model.Username}' is already taken.");
        }

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DeleteAsync(int id, int actingUserId, CancellationToken ct = default)
    {
        if (id == actingUserId)
        {
            return ServiceResult.Fail("You cannot delete your own account.");
        }

        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == id, ct);

        if (user is null)
        {
            return ServiceResult.Fail("That user no longer exists.");
        }

        if (user.Role == Roles.Admin && !await HasAnotherActiveAdminAsync(user.Id, ct))
        {
            return ServiceResult.Fail("This is the last active administrator and cannot be deleted.");
        }

        db.Users.Remove(user);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsSqlState(ex, ForeignKeyViolation))
        {
            // ActivityLog.PerformedByUserId is ON DELETE RESTRICT, because an audit row with
            // no author is not an audit row. Deactivation is the right move for someone who
            // has history, and the message says so rather than showing a raw constraint name.
            return ServiceResult.Fail(
                $"{user.FullName} has activity history and cannot be deleted. Deactivate the account instead.");
        }

        logger.LogInformation("User {Username} deleted.", user.Username);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> SetActiveAsync(
        int id, bool isActive, int actingUserId, CancellationToken ct = default)
    {
        if (id == actingUserId && !isActive)
        {
            return ServiceResult.Fail("You cannot deactivate your own account.");
        }

        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == id, ct);

        if (user is null)
        {
            return ServiceResult.Fail("That user no longer exists.");
        }

        if (!isActive && user.Role == Roles.Admin && !await HasAnotherActiveAdminAsync(user.Id, ct))
        {
            return ServiceResult.Fail("This is the last active administrator and cannot be deactivated.");
        }

        user.IsActive = isActive;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("User {Username} set to active={IsActive}.", user.Username, isActive);
        return ServiceResult.Ok();
    }

    public async Task<IReadOnlyList<SelectListItem>> GetAssetOptionsAsync(CancellationToken ct = default) =>
        await db.Assets
            .AsNoTracking()
            .OrderBy(a => a.AssetTag)
            .Select(a => new SelectListItem($"{a.AssetTag} - {a.Name}", a.Id.ToString()))
            .ToListAsync(ct);

    private Task<bool> HasAnotherActiveAdminAsync(int excludingUserId, CancellationToken ct) =>
        db.Users.AnyAsync(u => u.Id != excludingUserId && u.Role == Roles.Admin && u.IsActive, ct);

    private static bool IsSqlState(DbUpdateException ex, string sqlState) =>
        ex.InnerException is PostgresException pg && pg.SqlState == sqlState;
}
