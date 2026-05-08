using System;
using System.Collections.Generic;

namespace StressTests
{
    public static class RandomFunctionGenerator
    {
        private static readonly Random _random = new Random();

        private static readonly string[] _functions = { "sin", "cos", "tan", "sqrt", "ln", "log10", "exp", "abs" };
        private static readonly string[] _operators = { "+", "-", "*", "/" };

        public static List<string> GenerateFunctions(int count)
        {
            var result = new List<string>();
            
            // Add some known edge cases first
            result.Add("1/x");
            result.Add("tan(x)");
            result.Add("ln(x)");
            result.Add("sqrt(x)");
            result.Add("x^2 + 4");
            result.Add("exp(x) + 100");
            result.Add("sqrt(x^5 - sin(pi))");

            for (int i = 0; i < count; i++)
            {
                result.Add(GenerateRandomExpression(_random.Next(1, 3)));
            }

            return result;
        }

        private static string GenerateRandomExpression(int complexity)
        {
            int type = _random.Next(0, 4);
            string expr = "";

            switch (type)
            {
                case 0: // Polynomial
                    int degree = _random.Next(1, 6);
                    double coeff = _random.Next(-10, 11);
                    expr = $"{coeff}*x^{degree}";
                    if (_random.NextDouble() > 0.5)
                        expr += $" + {_random.Next(-20, 21)}";
                    break;

                case 1: // Simple Trig/Transcendental
                    string func = _functions[_random.Next(_functions.Length)];
                    double multiplier = _random.Next(1, 6);
                    expr = $"{multiplier}*{func}(x)";
                    if (_random.NextDouble() > 0.5)
                        expr += $" - {_random.Next(1, 10)}";
                    break;

                case 2: // Combined
                    string f1 = _functions[_random.Next(_functions.Length)];
                    string op = _operators[_random.Next(_operators.Length)];
                    expr = $"x {op} {f1}(x)";
                    break;
                
                case 3: // x^2 - constant
                    expr = $"x^2 - {_random.Next(1, 100)}";
                    break;
            }

            if (complexity > 1)
            {
                string op = _operators[_random.Next(_operators.Length)];
                expr = $"({expr}) {op} ({GenerateRandomExpression(complexity - 1)})";
            }

            return expr;
        }
    }
}
