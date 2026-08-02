using Microsoft.EntityFrameworkCore;
using URMS.Application.Contracts.Identity;
using URMS.Application.Contracts.Persistence;
using URMS.Application.DTOs.AdvisorAssignment;
using URMS.Domain.Abstractions;
using URMS.Domain.Entities;

namespace URMS.Infrastructure.Identity;

public class AdvisorAssignmentService : IAdvisorAssignmentService
{
    private readonly IUnitOfWork _unitOfWork;

    public AdvisorAssignmentService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> BulkAssignAsync(BulkAssignStudentsDto dto)
    {
        var assignmentRepo = _unitOfWork.Repository<AdvisorStudentAssignment>();
        var userRepo = _unitOfWork.Repository<ApplicationUser>();

        // Validate that the advisor exists
        var advisor = await userRepo.FindOneAsync(
            u => u.Id == dto.AdvisorId,
            q => q.Include(u => u.Advisor)
        );

        if (advisor is null || advisor.Advisor is null)
            return Result.Failure<int>(new Error("Advisor.NotFound", "Advisor not found.", 404));

        int addedCount = 0;

        foreach (var code in dto.UniversityCodes)
        {
            // Skip if already assigned
            var existing = await assignmentRepo.FindOneAsync(a => a.UniversityCode == code);
            if (existing is not null)
                continue;

            await assignmentRepo.AddAsync(new AdvisorStudentAssignment
            {
                UniversityCode = code,
                AdvisorId = dto.AdvisorId,
                AssignedAt = DateTime.UtcNow
            });

            // If student already registered, link them now
            var studentRepo = _unitOfWork.Repository<Student>();
            var student = await studentRepo.FindOneAsync(s => s.UniversityCode == code);
            if (student is not null && student.AcademicAdvisorId is null)
            {
                student.AcademicAdvisorId = dto.AdvisorId;
            }

            addedCount++;
        }

        await _unitOfWork.CompleteAsync();
        return Result.Success(addedCount);
    }

    public async Task<Result<List<AdvisorStudentAssignmentDto>>> GetAssignmentsByAdvisorAsync(string advisorId)
    {
        var assignmentRepo = _unitOfWork.Repository<AdvisorStudentAssignment>();
        var studentRepo = _unitOfWork.Repository<Student>();

        var assignments = await assignmentRepo.FindAllAsync(
            a => a.AdvisorId == advisorId,
            q => q.Include(a => a.Advisor)
        );

        var dtos = new List<AdvisorStudentAssignmentDto>();
        foreach (var a in assignments)
        {
            var student = await studentRepo.FindOneAsync(s => s.UniversityCode == a.UniversityCode);
            dtos.Add(new AdvisorStudentAssignmentDto(
                a.Id,
                a.UniversityCode,
                a.AdvisorId,
                a.Advisor.FullNameAr,
                a.AssignedAt,
                student is not null
            ));
        }

        return Result.Success(dtos);
    }

    public async Task<Result<List<AdvisorStudentAssignmentDto>>> GetAllAssignmentsAsync()
    {
        var assignmentRepo = _unitOfWork.Repository<AdvisorStudentAssignment>();
        var studentRepo = _unitOfWork.Repository<Student>();

        var assignments = await assignmentRepo.FindAllAsync(
            _ => true,
            q => q.Include(a => a.Advisor)
        );

        var dtos = new List<AdvisorStudentAssignmentDto>();
        foreach (var a in assignments)
        {
            var student = await studentRepo.FindOneAsync(s => s.UniversityCode == a.UniversityCode);
            dtos.Add(new AdvisorStudentAssignmentDto(
                a.Id,
                a.UniversityCode,
                a.AdvisorId,
                a.Advisor.FullNameAr,
                a.AssignedAt,
                student is not null
            ));
        }

        return Result.Success(dtos);
    }

    public async Task<Result> RemoveAssignmentAsync(string universityCode)
    {
        var assignmentRepo = _unitOfWork.Repository<AdvisorStudentAssignment>();

        var assignment = await assignmentRepo.FindOneAsync(a => a.UniversityCode == universityCode);
        if (assignment is null)
            return Result.Failure(new Error("Assignment.NotFound", "Assignment not found for this code.", 404));

        assignmentRepo.Delete(assignment);
        await _unitOfWork.CompleteAsync();
        return Result.Success();
    }

    public async Task<Result> ReassignAsync(AssignStudentDto dto)
    {
        var assignmentRepo = _unitOfWork.Repository<AdvisorStudentAssignment>();
        var studentRepo = _unitOfWork.Repository<Student>();

        var assignment = await assignmentRepo.FindOneAsync(a => a.UniversityCode == dto.UniversityCode);
        if (assignment is null)
            return Result.Failure(new Error("Assignment.NotFound", "Assignment not found for this code.", 404));

        assignment.AdvisorId = dto.AdvisorId;
        assignment.AssignedAt = DateTime.UtcNow;

        // Update the student too if already registered
        var student = await studentRepo.FindOneAsync(s => s.UniversityCode == dto.UniversityCode);
        if (student is not null)
        {
            student.AcademicAdvisorId = dto.AdvisorId;
        }

        await _unitOfWork.CompleteAsync();
        return Result.Success();
    }
}
