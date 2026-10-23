using backend.Models.Request;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController, Authorize, Route("api/students")]
public sealed class StudentsController(IStudentService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        Ok(await service.GetAllAsync(cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var student = await service.GetByIdAsync(id, cancellationToken);
        return student is null ? NotFound(new { message = "Student not found." }) : Ok(student);
    }

    [HttpPost]
    public async Task<IActionResult> Create(StudentRequest request, CancellationToken cancellationToken)
    {
        var student = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = student.Id }, student);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, StudentRequest request, CancellationToken cancellationToken)
    {
        var student = await service.UpdateAsync(id, request, cancellationToken);
        return student is null ? NotFound(new { message = "Student not found." }) : Ok(student);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        await service.DeleteAsync(id, cancellationToken)
            ? NoContent()
            : NotFound(new { message = "Student not found." });
}
