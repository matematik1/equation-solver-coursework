using System.Threading;
using System.Threading.Tasks;
using EquationSolver.Models;

namespace EquationSolver.Services.Solvers
{
    public interface IEquationSolver
    {
        /// <summary>
        /// Solves the equation asynchronously.
        /// </summary>
        Task<ResultModel> SolveAsync(EquationModel equation, CancellationToken cancellationToken);
    }
}
