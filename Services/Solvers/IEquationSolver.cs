using System.Threading;
using System.Threading.Tasks;
using EquationSolver.Models;

namespace EquationSolver.Services.Solvers
{
    public interface IEquationSolver
    {
        Task<ResultModel> SolveAsync(EquationModel equation, CancellationToken cancellationToken);
    }
}
