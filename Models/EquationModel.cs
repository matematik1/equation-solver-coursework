namespace EquationSolver.Models
{
    /// <summary>
    /// Represents input data for solving an equation.
    /// </summary>
    public class EquationModel
    {
        public string Expression { get; set; } = string.Empty;
        
        // Interval endpoints
        public string A { get; set; } = "1";
        public string B { get; set; } = "2";
        
        // Initial approximation for methods like Newton
        public string InitialGuess { get; set; } = "1.5";
        
        public string Epsilon { get; set; } = "1e-5";
        
        public string MaxIterations { get; set; } = "1000";
    }
}
