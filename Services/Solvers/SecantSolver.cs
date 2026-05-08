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
    public class SecantSolver : IEquationSolver
    {
        private readonly IFunctionParser _parser;

        public SecantSolver(IFunctionParser parser)
        {
            _parser = parser;
        }

        public async Task<ResultModel> SolveAsync(EquationModel equation, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                Console.WriteLine($"[SecantSolver] Started for f(x)={equation.Expression}");
                try
                {
                    var stopwatch = Stopwatch.StartNew();
                    var iterations = new List<IterationData>();

                    double x0 = Validator.ParseDouble(equation.A, out _); // First guess
                    double x1 = Validator.ParseDouble(equation.B, out _); // Second guess
                    double epsilon = Validator.ParseDouble(equation.Epsilon, out _);
                    int maxIterations = Validator.ParseInt(equation.MaxIterations, out _);
                    
                    double a = Validator.ParseDouble(equation.A, out _);
                    double b = Validator.ParseDouble(equation.B, out _);

                    int iterationCount = 0;
                    double error = double.MaxValue;

                    double fx0 = _parser.Evaluate(equation.Expression, x0);
                    double fx1 = _parser.Evaluate(equation.Expression, x1);

                    while (error > epsilon && iterationCount < maxIterations)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        iterationCount++;

                        double denominator = fx1 - fx0;
                        if (Math.Abs(denominator) < 1e-18)
                        {
                            Console.WriteLine($"[SecantSolver] Divergence detected: denominator near zero at iteration {iterationCount}");
                            return ResultModel.Error($"Метод Січних: Ділення на нуль (f(x1) ≈ f(x0)) на ітерації {iterationCount}. Метод розбігається.");
                        }

                        double x2 = x1 - fx1 * (x1 - x0) / denominator;
                        
                        if (!Validator.ValidateIterationBounds(x2, a, b, out string? boundError))
                        {
                            Console.WriteLine($"[SecantSolver] Out of bounds: {boundError} at iteration {iterationCount}, x={x2}");
                            return ResultModel.Error($"Метод Січних: {boundError}");
                        }

                        error = Math.Abs(x2 - x1);

                        double fx2 = _parser.Evaluate(equation.Expression, x2);

                        iterations.Add(new IterationData
                        {
                            IterationNumber = iterationCount,
                            Xn = x2,
                            FXn = fx2,
                            Error = error
                        });

                        x0 = x1;
                        fx0 = fx1;
                        x1 = x2;
                        fx1 = fx2;

                        // Check for divergence
                        if (error > 1e20)
                        {
                            Console.WriteLine($"[SecantSolver] Massive divergence detected");
                            return ResultModel.Error($"Метод Січних: Метод розбігається (похибка занадто велика).");
                        }
                    }

                    stopwatch.Stop();
                    Console.WriteLine($"[SecantSolver] Finished in {iterationCount} iterations. Success: {iterationCount < maxIterations}");

                    if (iterationCount >= maxIterations)
                    {
                        return ResultModel.Error("Метод Січних: Перевищено максимальну кількість ітерацій. Метод не збігається.");
                    }

                    return ResultModel.Success(x1, iterations, stopwatch.ElapsedMilliseconds, iterationCount);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SecantSolver] Critical Error: {ex}");
                    return ResultModel.Error($"Помилка під час обчислення: {ex.Message}");
                }
            }, cancellationToken);
        }
    }
}
