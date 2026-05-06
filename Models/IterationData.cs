namespace EquationSolver.Models
{
    /// <summary>
    /// Represents a single iteration step during the solving process.
    /// </summary>
    public class IterationData
    {
        public int IterationNumber { get; set; }
        public double Xn { get; set; }
        public double FXn { get; set; }
        public double Error { get; set; }
    }
}
