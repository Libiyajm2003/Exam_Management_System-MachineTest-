using ExamManagementSystem.Models;

namespace ExamManagementSystem.Repositories;

public interface IStudentRepository
{
    Task<List<StudentMst>> GetStudentsAsync();
    Task<StudentMst?> GetStudentByIdAsync(int id);
    Task<StudentMst> CreateStudentAsync(StudentMst student);
}



