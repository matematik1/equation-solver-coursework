using System;
using System.Globalization;
using EquationSolver.Models;
using EquationSolver.Services.Parsers;

namespace EquationSolver.Utils
{
    public static class Validator
    {
        public static double ParseDouble(string? input, out bool success)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                success = false;
                return 0;
            }
            string formatted = input.Replace(',', '.');
            success = double.TryParse(formatted, NumberStyles.Any, CultureInfo.InvariantCulture, out double result);
            return result;
        }

        public static int ParseInt(string? input, out bool success)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                success = false;
                return 0;
            }
            success = int.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out int result);
            return result;
        }

        public static string? ValidateEquationModel(EquationModel model, string methodName, IFunctionParser parser)
        {
            if (string.IsNullOrWhiteSpace(model.Expression))
                return "Введіть функцію.";

            if (!parser.IsSyntaxValid(model.Expression))
                return "Синтаксична помилка у виразі функції. Будь ласка, перевірте введення.";

            double epsilon = ParseDouble(model.Epsilon, out bool epsilonSuccess);
            if (!epsilonSuccess || epsilon <= 0)
                return "Точність (Epsilon) повинна бути коректним числом більшим за 0.";

            int maxIter = ParseInt(model.MaxIterations, out bool iterSuccess);
            if (!iterSuccess || maxIter <= 0)
                return "Максимальна кількість ітерацій повинна бути цілим числом більшим за 0.";

            if (methodName == "Бісекція" || methodName == "Січні")
            {
                double a = ParseDouble(model.A, out bool aSuccess);
                double b = ParseDouble(model.B, out bool bSuccess);

                if (!aSuccess || !bSuccess)
                    return "Межі інтервалу 'a' та 'b' повинні бути коректними числами.";

                if (a >= b)
                    return "Значення 'a' повинно бути меншим за 'b'.";

                if (!double.IsFinite(a) || !double.IsFinite(b))
                    return "Межі інтервалу повинні бути скінченними числами.";
                
                if (methodName == "Бісекція")
                {
                    try
                    {
                        double fa = parser.Evaluate(model.Expression, a);
                        double fb = parser.Evaluate(model.Expression, b);
                        
                        if (Math.Sign(fa) == Math.Sign(fb))
                            return "Для методу бісекції функція повинна мати різні знаки на кінцях інтервалу [a, b].";
                    }
                    catch (Exception ex)
                    {
                        return $"Помилка обчислення функції на кінцях інтервалу: {ex.Message}";
                    }
                }
            }
            
            if (methodName == "Ньютон")
            {
                double initialGuess = ParseDouble(model.InitialGuess, out bool igSuccess);
                if (!igSuccess || !double.IsFinite(initialGuess))
                    return "Початкове наближення повинно бути коректним скінченним числом.";
            }

            return null; // All good
        }
    }
}
