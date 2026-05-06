using System;
using org.mariuszgromada.math.mxparser;

namespace EquationSolver.Services.Parsers
{
    public interface IFunctionParser
    {
        /// <summary>
        /// Evaluates the mathematical expression at the given x value.
        /// </summary>
        double Evaluate(string expression, double x);
        
        /// <summary>
        /// Validates if the mathematical expression has correct syntax.
        /// </summary>
        bool IsSyntaxValid(string expression);
    }

    public class FunctionParser : IFunctionParser
    {
        public FunctionParser()
        {
            // Optional: configure mxparser to be fast or quiet
            // mXparser.disableImpliedMultiplicationMode();
        }

        public double Evaluate(string expression, double x)
        {
            var argument = new Argument("x", x);
            var expr = new Expression(expression, argument);
            return expr.calculate();
        }

        public bool IsSyntaxValid(string expression)
        {
            var argument = new Argument("x", 1);
            var expr = new Expression(expression, argument);
            return expr.checkSyntax();
        }
    }
}
