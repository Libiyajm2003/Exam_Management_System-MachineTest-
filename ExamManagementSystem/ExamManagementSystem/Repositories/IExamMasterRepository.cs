using ExamManagementSystem.Models;

namespace ExamManagementSystem.Repositories;

public interface IExamMasterRepository
{
    Task<List<ExamMaster>> GetExamMastersAsync();
    Task<ExamMaster?> GetExamByIdAsync(int masterId);
    Task<ExamMaster> AddExamAsync(ExamMaster master);
}



