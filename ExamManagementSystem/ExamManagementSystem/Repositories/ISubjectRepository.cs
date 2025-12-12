using ExamManagementSystem.Models;

namespace ExamManagementSystem.Repositories;

public interface ISubjectRepository
{
    Task<List<SubjectMst>> GetSubjectsAsync();
    Task<SubjectMst?> GetSubjectByIdAsync(int id);
}



