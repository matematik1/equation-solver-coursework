namespace EquationSolver.Models
{
    /// <summary>
    /// Represents the result of comparing different equation solving methods.
    /// </summary>
    public class ComparisonResult
    {
        public string MethodName { get; set; } = string.Empty;
        public int IterationsCount { get; set; }
        public long ElapsedMilliseconds { get; set; }
        public double? Root { get; set; }
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
