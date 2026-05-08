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
                Console.WriteLine($"[NewtonSolver] Started for f(x)={equation.Expression}");
                try
                {
                    var stopwatch = Stopwatch.StartNew();
                    var iterations = new List<IterationData>();

                    double x0 = Validator.ParseDouble(equation.InitialGuess, out _);
                    double epsilon = Validator.ParseDouble(equation.Epsilon, out _);
                    int maxIterations = Validator.ParseInt(equation.MaxIterations, out _);
                    
                    double a = Validator.ParseDouble(equation.A, out _);
                    double b = Validator.ParseDouble(equation.B, out _);

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

                        if (Math.Abs(derivative) < 1e-15)
                        {
                            Console.WriteLine($"[NewtonSolver] Divergence detected: derivative near zero at iteration {iterationCount}");
                            return ResultModel.Error($"Метод Ньютона: Похідна наближається до нуля на ітерації {iterationCount}. Метод розбігається.");
                        }

                        double x1 = x0 - fx0 / derivative;
                        
                        if (!Validator.ValidateIterationBounds(x1, a, b, out string? boundError))
                        {
                            Console.WriteLine($"[NewtonSolver] Out of bounds: {boundError} at iteration {iterationCount}, x={x1}");
                            return ResultModel.Error($"Метод Ньютона: {boundError}");
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

                        if (error > 1e20)
                        {
                             Console.WriteLine($"[NewtonSolver] Massive divergence detected");
                             return ResultModel.Error($"Метод Ньютона: Метод розбігається (похибка занадто велика).");
                        }
                    }

                    stopwatch.Stop();
                    Console.WriteLine($"[NewtonSolver] Finished in {iterationCount} iterations. Success: {iterationCount < maxIterations}");

                    if (iterationCount >= maxIterations)
                    {
                        return ResultModel.Error("Метод Ньютона: Перевищено максимальну кількість ітерацій. Метод не збігається.");
                    }

                    // Strict Post-Solver Validation
                    if (!double.IsFinite(x0))
                    {
                        return ResultModel.Error("Метод Ньютона: Отримано нескінченне значення.");
                    }

                    try
                    {
                        double fFinal = _parser.Evaluate(equation.Expression, x0);
                        if (Math.Abs(fFinal) > epsilon * 1000) // allowing some margin for numerical stability
                        {
                            return ResultModel.Error($"Метод Ньютона: Отриманий результат не є коренем. f(x) = {fFinal:E4}");
                        }
                    }
                    catch { return ResultModel.Error("Метод Ньютона: Помилка перевірки результату."); }

                    if (x0 < a - 1e-9 || x0 > b + 1e-9)
                    {
                        return ResultModel.Error($"Метод Ньютона: Знайдений корінь {x0:F4} знаходиться поза межами інтервалу [{a}; {b}].");
                    }

                    return ResultModel.Success(x0, iterations, stopwatch.ElapsedMilliseconds, iterationCount);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    Console.WriteLine($"[NewtonSolver] Critical Error: {ex}");
                    return ResultModel.Error($"Помилка під час обчислення: {ex.Message}");
                }
            }, cancellationToken);
        }
    }
}
