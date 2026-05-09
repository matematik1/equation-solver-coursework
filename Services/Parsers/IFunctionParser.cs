using System;
using org.mariuszgromada.math.mxparser;

namespace EquationSolver.Services.Parsers
{
    public interface IFunctionParser
    {
        double Evaluate(string expression, double x);
        bool IsSyntaxValid(string expression);
    }

    public class FunctionParser : IFunctionParser
    {
        public FunctionParser()
        { }

        public double Evaluate(string expression, double x)
        {
            try
            {
                var argument = new Argument("x", x);
                var expr = new Expression(expression, argument);
                double result = expr.calculate();

                if (double.IsNaN(result))
                {
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
            
            bool isValid = expr.checkSyntax();
            
            if (isValid)
            {
                try
                {
                    double res = expr.calculate();
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
