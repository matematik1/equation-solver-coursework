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
    public class BisectionSolver : IEquationSolver
    {
        private readonly IFunctionParser _parser;

        public BisectionSolver(IFunctionParser parser)
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
                    
                    double a = Validator.ParseDouble(equation.A, out _);
                    double b = Validator.ParseDouble(equation.B, out _);
                    double epsilon = Validator.ParseDouble(equation.Epsilon, out _);
                    int maxIterations = Validator.ParseInt(equation.MaxIterations, out _);
                    
                    double fa = _parser.Evaluate(equation.Expression, a);
                    double fb = _parser.Evaluate(equation.Expression, b);

                    if (Math.Sign(fa) == Math.Sign(fb))
                    {
                        return ResultModel.Error("Метод бісекції: Функція має однаковий знак на кінцях інтервалу.");
                    }

                    double c = a;
                    int iterationCount = 0;

                    while ((b - a) / 2.0 > epsilon && iterationCount < maxIterations)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        iterationCount++;
                        c = (a + b) / 2.0;
                        double fc = _parser.Evaluate(equation.Expression, c);

                        if (!double.IsFinite(fc))
                        {
                            return ResultModel.Error($"Метод бісекції: Отримано нескінченне значення або NaN на ітерації {iterationCount}.");
                        }

                        iterations.Add(new IterationData
                        {
                            IterationNumber = iterationCount,
                            Xn = c,
                            FXn = fc,
                            Error = (b - a) / 2.0
                        });

                        if (Math.Abs(fc) < 1e-15) // exact root found
                            break;

                        if (Math.Sign(fc) == Math.Sign(fa))
                        {
                            a = c;
                            fa = fc;
                        }
                        else
                        {
                            b = c;
                            fb = fc;
                        }
                    }

                    stopwatch.Stop();
                    
                    if (iterationCount >= maxIterations)
                    {
                        return ResultModel.Error("Перевищено максимальну кількість ітерацій. Метод може не збігатися до заданої точності.");
                    }

                    return ResultModel.Success(c, iterations, stopwatch.ElapsedMilliseconds, iterationCount);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    Console.WriteLine($"Bisection Error: {ex}");
                    return ResultModel.Error($"Помилка під час обчислення: {ex.Message}");
                }
            }, cancellationToken);
        }
    }
}
