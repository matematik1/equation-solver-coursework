using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EquationSolver.Models;
using EquationSolver.Services.Parsers;

namespace EquationSolver.Utils
{
    public static class Validator
    {
        private const int MaxDecimalPlaces = 15;
        private const double MaxBoundary = 1000000.0;
        private const double MaxIntervalWidth = 10000.0;
        private const int AbsoluteMaxIterations = 1000000;

        public static double ParseDouble(string? input, out bool success)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                success = false;
                return 0;
            }
            string formatted = input.Replace(',', '.').Trim();
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
            success = int.TryParse(input.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out int result);
            return result;
        }

        private static bool HasTooManyDecimalPlaces(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;
            string s = input.Replace(',', '.').Trim();
            if (s.Contains('e', StringComparison.OrdinalIgnoreCase)) return false; // Allow scientific notation as is
            int dotIndex = s.IndexOf('.');
            if (dotIndex == -1) return false;
            return (s.Length - dotIndex - 1) > MaxDecimalPlaces;
        }

        public static string? ValidateExpression(string? expression, IFunctionParser parser)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return "Введіть функцію.";

            // Заборона подвійних операторів
            string[] forbidden = { "++", "--", "**", "//", "^^", "+*", "+/", "-*", "-/", "*+", "/+", "*^", "/^", "^*", "^/" };
            foreach (var op in forbidden)
            {
                if (expression.Contains(op))
                    return $"Некоректний синтаксис: подвійний оператор '{op}'.";
            }

            // Перевірка дужок
            int brackets = 0;
            foreach (char c in expression)
            {
                if (c == '(') brackets++;
                if (c == ')') brackets--;
                if (brackets < 0) return "Помилка у виразі: закриваюча дужка без відповідної відкриваючої.";
            }
            if (brackets != 0) return "Помилка у виразі: присутні незакриті дужки.";

            if (!parser.IsSyntaxValid(expression))
                return "Синтаксична помилка у виразі функції. Будь ласка, перевірте введення.";

            return null;
        }

        private const double MinAbsoluteValue = 1e-15;

        private static bool IsExtremelySmall(double value)
        {
            return value != 0 && Math.Abs(value) < MinAbsoluteValue;
        }

        private static string? ValidateNumberStability(double value, string fieldName)
        {
            if (IsExtremelySmall(value))
                return $"Значення '{fieldName}' занадто мале для стабільних обчислень. Мінімальне допустиме значення: 1e-15.";
            return null;
        }

        public static string? ValidateEquationModel(EquationModel model, string methodName, IFunctionParser parser)
        {
            var exprError = ValidateExpression(model.Expression, parser);
            if (exprError != null) return exprError;

            // Перевірка кількості знаків після коми
            if (HasTooManyDecimalPlaces(model.A) || HasTooManyDecimalPlaces(model.B) || 
                HasTooManyDecimalPlaces(model.InitialGuess) || HasTooManyDecimalPlaces(model.Epsilon))
                return "Допустимо не більше 15 знаків після коми.";

            // Валідація Epsilon
            double epsilon = ParseDouble(model.Epsilon, out bool epsilonSuccess);
            if (!epsilonSuccess)
                return "Точність (Epsilon) повинна бути коректним числом.";
            if (epsilon < MinAbsoluteValue || epsilon > 1)
                return $"Точність (Epsilon) повинна бути в діапазоні від {MinAbsoluteValue:G} до 1.";

            // Валідація MaxIterations
            string? iterInput = model.MaxIterations?.Trim();
            if (string.IsNullOrWhiteSpace(iterInput))
                return "Введіть кількість ітерацій.";
            
            if (!double.TryParse(iterInput, NumberStyles.Any, CultureInfo.InvariantCulture, out double dIter))
                return "Максимальна кількість ітерацій повинна бути числом.";
            
            if (dIter % 1 != 0)
                return "Максимальна кількість ітерацій повинна бути цілим числом.";

            if (dIter < 1 || dIter > AbsoluteMaxIterations)
                return $"Максимальна кількість ітерацій повинна бути в межах від 1 до {AbsoluteMaxIterations:N0}.";

            if (methodName == "Бісекція" || methodName == "Січні")
            {
                double a = ParseDouble(model.A, out bool aSuccess);
                double b = ParseDouble(model.B, out bool bSuccess);

                if (!aSuccess || !bSuccess)
                    return "Межі інтервалу 'a' та 'b' повинні бути коректними числами.";

                if (!double.IsFinite(a) || !double.IsFinite(b))
                    return "Межі інтервалу повинні бути скінченними числами.";

                // Перевірка на занадто малі значення (крім 0)
                var aStability = ValidateNumberStability(a, "a");
                if (aStability != null) return aStability;
                var bStability = ValidateNumberStability(b, "b");
                if (bStability != null) return bStability;

                // Hard Limits
                if (Math.Abs(a) > MaxBoundary || Math.Abs(b) > MaxBoundary)
                    return $"Значення інтервалу виходять за допустимі межі (±{MaxBoundary:N0}).";

                if (a >= b)
                    return "Значення 'a' повинно бути меншим за 'b'.";

                double diff = Math.Abs(b - a);
                if (diff < MinAbsoluteValue)
                    return "Межі інтервалу 'a' та 'b' занадто близькі.";

                // Dynamic/Smart Validation for Graph readability
                if (diff > MaxIntervalWidth)
                    return $"Різниця між a та b не повинна перевищувати {MaxIntervalWidth:N0}. Поточна різниця: {diff:N0}.";

                if (methodName == "Бісекція")
                {
                    try
                    {
                        double fa = parser.Evaluate(model.Expression, a);
                        double fb = parser.Evaluate(model.Expression, b);
                        
                        if (!double.IsFinite(fa) || !double.IsFinite(fb))
                            return "Функція повертає нескінченне значення або NaN на межах інтервалу.";

                        if (Math.Sign(fa) == Math.Sign(fb))
                            return "На заданому інтервалі функція не змінює знак. Метод бісекції неможливий.";
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
                if (!igSuccess)
                    return "Початкове наближення повинно бути коректним числом.";
                
                if (!double.IsFinite(initialGuess))
                    return "Початкове наближення повинно бути скінченним числом.";

                var igStability = ValidateNumberStability(initialGuess, "Початкове наближення");
                if (igStability != null) return igStability;

                if (Math.Abs(initialGuess) > MaxBoundary)
                    return $"Початкове наближення виходить за допустимі межі (±{MaxBoundary:N0}).";
            }

            return null; // All good
        }
    }
}
