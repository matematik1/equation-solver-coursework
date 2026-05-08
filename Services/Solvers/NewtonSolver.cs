using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using EquationSolver.Models;
using EquationSolver.Services.Parsers;
using EquationSolver.Utils;

namespace EquationSolver.Services.Solvers
{
    public class NewtonSolver : IEquationSolver
    {
        private readonly IFunctionParser _parser;
        // Small dx for numerical differentiation
        private const double Dx = 1e-7;

        public NewtonSolver(IFunctionParser parser)
        {
            _parser = parser;
        }

        public async Task<ResultModel> SolveAsync(EquationModel equation, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var stopwatch = Stopwatch.StartNew();
                    var iterations = new List<IterationData>();

                    double x0 = Validator.ParseDouble(equation.InitialGuess, out _);
                    double epsilon = Validator.ParseDouble(equation.Epsilon, out _);
                    int maxIterations = Validator.ParseInt(equation.MaxIterations, out _);

                    int iterationCount = 0;
                    double error = double.MaxValue;

                    while (error > epsilon && iterationCount < maxIterations)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        iterationCount++;

                        double fx0 = _parser.Evaluate(equation.Expression, x0);
                        
                        // Numerical derivative: f'(x) ≈ (f(x + dx) - f(x - dx)) / (2 * dx)
                        double fx0PlusDx = _parser.Evaluate(equation.Expression, x0 + Dx);
                        double fx0MinusDx = _parser.Evaluate(equation.Expression, x0 - Dx);
                        double derivative = (fx0PlusDx - fx0MinusDx) / (2 * Dx);

                        if (Math.Abs(derivative) < 1e-12)
                        {
                            return ResultModel.Error($"Метод Ньютона: Похідна наближається до нуля на ітерації {iterationCount}. Метод розбігається.");
                        }

                        double x1 = x0 - fx0 / derivative;
                        
                        if (!double.IsFinite(x1))
                        {
                            return ResultModel.Error($"Метод Ньютона: Отримано нескінченне значення або NaN на ітерації {iterationCount}.");
                        }

                        error = Math.Abs(x1 - x0);

                        double fx1 = _parser.Evaluate(equation.Expression, x1);

                        iterations.Add(new IterationData
                        {
                            IterationNumber = iterationCount,
                            Xn = x1,
                            FXn = fx1,
                            Error = error
                        });

                        x0 = x1;

                        // Check for divergence
                        if (error > 1e20)
                        {
                             return ResultModel.Error($"Метод Ньютона: Метод розбігається (помилка занадто велика).");
                        }
                    }

                    stopwatch.Stop();

                    if (iterationCount >= maxIterations)
                    {
                        return ResultModel.Error("Метод Ньютона: Перевищено максимальну кількість ітерацій. Метод не збігається.");
                    }

                    return ResultModel.Success(x0, iterations, stopwatch.ElapsedMilliseconds, iterationCount);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    Console.WriteLine($"Newton Error: {ex}");
                    return ResultModel.Error($"Помилка під час обчислення: {ex.Message}");
                }
            }, cancellationToken);
        }
    }
}
