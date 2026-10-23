using backend.Data;
using backend.Models.Data;
using backend.Models.Request;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public sealed class UnitService(AppDbContext db) : IUnitService
{
    private static readonly string[] ValidGroups = ["Weight", "Volume", "Count"];

    public async Task<UnitPageResponse> GetPageAsync(UnitQueryRequest request, CancellationToken cancellationToken)
    {
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var query = db.Units.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (request.Status.Equals("active", StringComparison.OrdinalIgnoreCase)) query = query.Where(unit => unit.IsActive);
            else if (request.Status.Equals("inactive", StringComparison.OrdinalIgnoreCase)) query = query.Where(unit => !unit.IsActive);
            else return new([], pageNumber, pageSize, 0, 0, 0, 0, 0);
        }
        if (!string.IsNullOrWhiteSpace(request.Group))
        {
            var group = request.Group.Trim();
            query = query.Where(unit => unit.Group == group);
        }
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(unit => unit.Name.Contains(term) || unit.Symbol.Contains(term) || unit.Group.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderBy(unit => unit.Name)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(unit => new UnitResponse(unit.Id, unit.Name, unit.Symbol, unit.Measure,
                unit.Group, unit.Description, unit.IsActive, unit.CreatedAtUtc, unit.UpdatedAtUtc))
            .ToListAsync(cancellationToken);
        var active = await query.CountAsync(unit => unit.IsActive, cancellationToken);
        var groups = await query.Select(unit => unit.Group).Distinct().CountAsync(cancellationToken);
        return new(rows, pageNumber, pageSize, total,
            (int)Math.Ceiling(total / (double)pageSize), total, active, groups);
    }

    public async Task<UnitResponse?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        await db.Units.AsNoTracking().Where(unit => unit.Id == id)
            .Select(unit => new UnitResponse(unit.Id, unit.Name, unit.Symbol, unit.Measure,
                unit.Group, unit.Description, unit.IsActive, unit.CreatedAtUtc, unit.UpdatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<UnitServiceResult<UnitResponse>> CreateAsync(
        CreateUnitRequest request, int actorUserId, CancellationToken cancellationToken)
    {
        var validation = await ValidateAsync(request.Name, request.Symbol, request.Group, null, cancellationToken);
        if (validation is not null) return validation;
        var unit = new AppUnit
        {
            Name = request.Name.Trim(), Symbol = request.Symbol.Trim(), Measure = request.Measure,
            Group = NormalizeGroup(request.Group), Description = request.Description?.Trim(),
            IsActive = request.IsActive, CreatedBy = actorUserId
        };
        db.Units.Add(unit);
        await db.SaveChangesAsync(cancellationToken);
        return new(new UnitResponse(unit.Id, unit.Name, unit.Symbol, unit.Measure,
            unit.Group, unit.Description, unit.IsActive, unit.CreatedAtUtc, unit.UpdatedAtUtc), UnitServiceError.None, null);
    }

    public async Task<UnitServiceResult<UnitResponse>> UpdateAsync(
        int id, UpdateUnitRequest request, int actorUserId, CancellationToken cancellationToken)
    {
        var unit = await db.Units.FindAsync([id], cancellationToken);
        if (unit is null) return new(null, UnitServiceError.NotFound, "Unit not found.");
        var validation = await ValidateAsync(request.Name, request.Symbol, request.Group, id, cancellationToken);
        if (validation is not null) return validation;
        unit.Name = request.Name.Trim();
        unit.Symbol = request.Symbol.Trim();
        unit.Measure = request.Measure;
        unit.Group = NormalizeGroup(request.Group);
        unit.Description = request.Description?.Trim();
        unit.IsActive = request.IsActive;
        unit.UpdatedBy = actorUserId;
        unit.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return new(new UnitResponse(unit.Id, unit.Name, unit.Symbol, unit.Measure,
            unit.Group, unit.Description, unit.IsActive, unit.CreatedAtUtc, unit.UpdatedAtUtc), UnitServiceError.None, null);
    }

    public async Task<UnitServiceResult<bool>> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var unit = await db.Units.FindAsync([id], cancellationToken);
        if (unit is null) return new(false, UnitServiceError.NotFound, "Unit not found.");
        db.Units.Remove(unit);
        await db.SaveChangesAsync(cancellationToken);
        return new(true, UnitServiceError.None, null);
    }

    private async Task<UnitServiceResult<UnitResponse>?> ValidateAsync(
        string name, string symbol, string group, int? excludingId, CancellationToken cancellationToken)
    {
        if (!ValidGroups.Contains(group.Trim(), StringComparer.OrdinalIgnoreCase))
            return new(null, UnitServiceError.InvalidGroup, "Group must be Weight, Volume, or Count.");
        var normalizedName = name.Trim();
        if (await db.Units.AnyAsync(unit => unit.Name.ToUpper() == normalizedName.ToUpper() && unit.Id != excludingId, cancellationToken))
            return new(null, UnitServiceError.DuplicateName, "A unit with this name already exists.");
        var normalizedSymbol = symbol.Trim();
        if (await db.Units.AnyAsync(unit => unit.Symbol.ToUpper() == normalizedSymbol.ToUpper() && unit.Id != excludingId, cancellationToken))
            return new(null, UnitServiceError.DuplicateSymbol, "A unit with this symbol already exists.");
        return null;
    }

    private static string NormalizeGroup(string group) =>
        ValidGroups.First(valid => valid.Equals(group.Trim(), StringComparison.OrdinalIgnoreCase));
}
