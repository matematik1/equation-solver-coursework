namespace EquationSolver.Models
{
    /// <summary>
    /// Represents input data for solving an equation.
    /// </summary>
    public class EquationModel
    {
        public string Expression { get; set; } = string.Empty;
        
        // Interval endpoints
        public double A { get; set; }
        public double B { get; set; }
        
        // Initial approximation for methods like Newton
        public double InitialGuess { get; set; }
        
        public double Epsilon { get; set; } = 1e-5;
        
        public int MaxIterations { get; set; } = 1000;
    }
}
