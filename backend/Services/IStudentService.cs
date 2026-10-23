using backend.Models.Request;

namespace backend.Services;

public interface IStudentService
{
    Task<IReadOnlyList<StudentResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<StudentResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<StudentResponse> CreateAsync(StudentRequest request, CancellationToken cancellationToken);
    Task<StudentResponse?> UpdateAsync(int id, StudentRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
