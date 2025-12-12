using ExamManagementSystem.Models;

namespace ExamManagementSystem.Repositories;

public interface IExamDetailRepository
{
    Task<List<ExamDtls>> GetDetailsByMasterIdAsync(int masterId);
}



