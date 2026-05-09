namespace EquationSolver.Models
{
    public class IterationData
    {
        public int IterationNumber { get; set; }
        public double Xn { get; set; }
        public double FXn { get; set; }
        public double Error { get; set; }
    }
}
