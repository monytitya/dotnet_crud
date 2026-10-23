using backend.Data;
using backend.Models.Data;
using backend.Models.Request;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public sealed class StudentService(AppDbContext db) : IStudentService
{
    public async Task<IReadOnlyList<StudentResponse>> GetAllAsync(CancellationToken cancellationToken) =>
        await db.Students.AsNoTracking()
            .OrderBy(student => student.Name)
            .Select(student => new StudentResponse(student.Id, student.Name, student.Email))
            .ToListAsync(cancellationToken);

    public async Task<StudentResponse?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        await db.Students.AsNoTracking()
            .Where(student => student.Id == id)
            .Select(student => new StudentResponse(student.Id, student.Name, student.Email))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<StudentResponse> CreateAsync(StudentRequest request, CancellationToken cancellationToken)
    {
        var student = new Student { Name = request.Name.Trim(), Email = request.Email.Trim().ToLowerInvariant() };
        db.Students.Add(student);
        await db.SaveChangesAsync(cancellationToken);
        return new StudentResponse(student.Id, student.Name, student.Email);
    }

    public async Task<StudentResponse?> UpdateAsync(int id, StudentRequest request, CancellationToken cancellationToken)
    {
        var student = await db.Students.FindAsync([id], cancellationToken);
        if (student is null) return null;
        student.Name = request.Name.Trim();
        student.Email = request.Email.Trim().ToLowerInvariant();
        await db.SaveChangesAsync(cancellationToken);
        return new StudentResponse(student.Id, student.Name, student.Email);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var student = await db.Students.FindAsync([id], cancellationToken);
        if (student is null) return false;
        db.Students.Remove(student);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
