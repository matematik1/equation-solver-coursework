using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using EquationSolver.Models;
using EquationSolver.Services.Parsers;

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
                var stopwatch = Stopwatch.StartNew();
                var iterations = new List<IterationData>();
                
                double a = equation.A;
                double b = equation.B;
                
                double fa = _parser.Evaluate(equation.Expression, a);
                double fb = _parser.Evaluate(equation.Expression, b);

                if (Math.Sign(fa) == Math.Sign(fb))
                {
                    return ResultModel.Error("Метод бісекції: Функція має однаковий знак на кінцях інтервалу (f(a) * f(b) > 0).");
                }

                double c = a;
                int iterationCount = 0;

                while ((b - a) / 2.0 > equation.Epsilon && iterationCount < equation.MaxIterations)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    iterationCount++;
                    c = (a + b) / 2.0;
                    double fc = _parser.Evaluate(equation.Expression, c);

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
                
                if (iterationCount >= equation.MaxIterations)
                {
                    return ResultModel.Error("Перевищено максимальну кількість ітерацій.");
                }

                return ResultModel.Success(c, iterations, stopwatch.ElapsedMilliseconds, iterationCount);
            }, cancellationToken);
        }
    }
}
