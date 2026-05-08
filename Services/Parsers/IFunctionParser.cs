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
            // Standard configuration for mXparser
        }

        public double Evaluate(string expression, double x)
        {
            try
            {
                var argument = new Argument("x", x);
                var expr = new Expression(expression, argument);
                double result = expr.calculate();

                if (double.IsNaN(result))
                {
                    // mXparser returns NaN for many errors like division by zero
                    throw new ArithmeticException("Результат обчислення не є числом (NaN). Можливо, ділення на нуль або корінь з від'ємного числа.");
                }

                if (double.IsInfinity(result))
                {
                    throw new OverflowException("Значення функції занадто велике (нескінченність).");
                }

                return result;
            }
            catch (OverflowException) { throw; }
            catch (ArithmeticException) { throw; }
            catch (Exception ex)
            {
                Console.WriteLine($"Parser Error: {ex}");
                throw new Exception("Помилка під час обчислення функції. Перевірте синтаксис.");
            }
        }

        public bool IsSyntaxValid(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression)) return false;
            
            var argument = new Argument("x", 1.0);
            var expr = new Expression(expression, argument);
            
            // Basic syntax check
            bool isValid = expr.checkSyntax();
            
            if (isValid)
            {
                // Extra check: try to evaluate at some points to see if it's really valid
                try
                {
                    double res = expr.calculate();
                    // If it's NaN but checkSyntax passed, it might be a valid syntax that leads to math error
                    // which is okay for syntax check
                }
                catch
                {
                    return false;
                }
            }
            
            return isValid;
        }
    }
}
