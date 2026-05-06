using System;
using EquationSolver.Models;
using EquationSolver.Services.Parsers;

namespace EquationSolver.Utils
{
    public static class Validator
    {
        public static string? ValidateEquationModel(EquationModel model, string methodName, IFunctionParser parser)
        {
            if (string.IsNullOrWhiteSpace(model.Expression))
                return "Введіть функцію.";

            if (!parser.IsSyntaxValid(model.Expression))
                return "Синтаксична помилка у виразі функції. Будь ласка, перевірте введення.";

            if (model.Epsilon <= 0)
                return "Точність (Epsilon) повинна бути більшою за 0.";

            if (model.MaxIterations <= 0)
                return "Максимальна кількість ітерацій повинна бути більшою за 0.";

            if (methodName == "Бісекція" || methodName == "Січні")
            {
                if (model.A >= model.B)
                    return "Значення 'a' повинно бути меншим за 'b'.";

                if (!double.IsFinite(model.A) || !double.IsFinite(model.B))
                    return "Межі інтервалу повинні бути скінченними числами.";
                
                if (methodName == "Бісекція")
                {
                    try
                    {
                        double fa = parser.Evaluate(model.Expression, model.A);
                        double fb = parser.Evaluate(model.Expression, model.B);
                        
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
                 if (!double.IsFinite(model.InitialGuess))
                    return "Початкове наближення повинно бути скінченним числом.";
            }

            return null; // All good
        }
    }
}
