using System.Threading.Tasks;
using EquationSolver.Models;

namespace EquationSolver.Services
{
    public interface IFileService
    {
        Task SaveResultsAsync(string filePath, ResultModel result, string equationStr, string methodName);
    }
}
