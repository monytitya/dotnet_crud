using backend.Models.Request;

namespace backend.Services;

public interface IUnitService
{
    Task<UnitPageResponse> GetPageAsync(UnitQueryRequest request, CancellationToken cancellationToken);
    Task<UnitResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<UnitServiceResult<UnitResponse>> CreateAsync(CreateUnitRequest request, int actorUserId, CancellationToken cancellationToken);
    Task<UnitServiceResult<UnitResponse>> UpdateAsync(int id, UpdateUnitRequest request, int actorUserId, CancellationToken cancellationToken);
    Task<UnitServiceResult<bool>> DeleteAsync(int id, CancellationToken cancellationToken);
}

public enum UnitServiceError { None, InvalidInput, InvalidGroup, DuplicateName, DuplicateSymbol, NotFound }

public sealed record UnitServiceResult<T>(T? Value, UnitServiceError Error, string? Message)
{
    public bool Succeeded => Error == UnitServiceError.None;
}
