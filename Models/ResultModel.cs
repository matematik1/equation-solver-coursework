using System.Collections.Generic;

namespace EquationSolver.Models
{
    /// <summary>
    /// Encapsulates the results of the equation solving process.
    /// </summary>
    public class ResultModel
    {
        public double? Root { get; set; }
        public List<IterationData> Iterations { get; set; } = new();
        public long ElapsedMilliseconds { get; set; }
        public int TotalIterations { get; set; }
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }

        public static ResultModel Success(double root, List<IterationData> iterations, long elapsed, int total)
        {
            return new ResultModel
            {
                Root = root,
                Iterations = iterations,
                ElapsedMilliseconds = elapsed,
                TotalIterations = total,
                IsSuccess = true
            };
        }

        public static ResultModel Error(string message)
        {
            return new ResultModel
            {
                IsSuccess = false,
                ErrorMessage = message
            };
        }
    }
}
