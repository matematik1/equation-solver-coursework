using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EquationSolver.Models;
using EquationSolver.Services;
using EquationSolver.Services.Parsers;
using EquationSolver.Services.Solvers;
using EquationSolver.Utils;

namespace StressTests
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("=== EquationSolver Mass Testing System ===");

            var parser = new FunctionParser();
            var solverFactory = new SolverFactory(parser);
            var results = new List<TestResult>();

            var testFunctions = new[]
            {
                "x^2 - 4",
                "x^3 - x - 2",
                "x^5 - 3x + 1",
                "sin(x)",
                "cos(x) - x",
                "e^x - 2",
                "ln(x) - 1",
                "x*sin(x) - 1",
                "x^3 + sin(x) - 5",
                "1/x - 0.5",
                "tan(x) - x",
                "x^2 - 10^5",
                "x^10 - 1"
            };

            var methods = new[] { "Бісекція", "Ньютон", "Січні" };
            var epsilons = new[] { "1e-5", "1e-10", "1e-15" };

            int testId = 1;

            // 1. MASS TESTING OF VALID SOLVABLE CASES
            Console.WriteLine("Running functional tests...");
            foreach (var func in testFunctions)
            {
                foreach (var method in methods)
                {
                    foreach (var eps in epsilons)
                    {
                        var model = CreateDefaultModel(func, eps, method);
                        results.Add(await RunSingleTest(testId++, func, method, model, solverFactory, parser));
                    }
                }
            }

            // 2. STRESS TESTING - LARGE/SMALL VALUES
            Console.WriteLine("Running stress tests...");
            var stressCases = new (string Func, string A, string B, string IG, string Eps)[]
            {
                ("x - 1e-15", "0", "1", "0.5", "1e-15"),
                ("x - 1000000", "999000", "1000000", "999500", "1e-5"),
                ("x^2 - 2", "1", "2", "1.5", "1e-100"), // Invalid Epsilon
                ("x^3", "0", "1000000", "1", "1e-5"),   // Large B
            };

            foreach (var sc in stressCases)
            {
                var model = new EquationModel { Expression = sc.Func, A = sc.A, B = sc.B, InitialGuess = sc.IG, Epsilon = sc.Eps, MaxIterations = "1000" };
                foreach (var method in methods)
                {
                    results.Add(await RunSingleTest(testId++, sc.Func, method, model, solverFactory, parser));
                }
            }

            // 3. VALIDATION TESTING - INVALID INPUTS
            Console.WriteLine("Running validation tests...");
            var validationCases = new (string Func, string A, string B, string IG, string Eps, string Iter)[]
            {
                ("x^2 ++ 1", "0", "1", "0.5", "1e-5", "100"),    // Bad syntax
                ("(x+1", "0", "1", "0.5", "1e-5", "100"),        // Bad brackets
                ("x^2 - 4", "2", "1", "1.5", "1e-5", "100"),     // a > b
                ("x^2 + 4", "0", "5", "1", "1e-5", "100"),       // No root (Bisection should fail validation)
                ("x^2 - 4", "1", "2", "1.5", "1e-5", "10000000") // Too many iterations
            };

            foreach (var vc in validationCases)
            {
                var model = new EquationModel { Expression = vc.Func, A = vc.A, B = vc.B, InitialGuess = vc.IG, Epsilon = vc.Eps, MaxIterations = vc.Iter };
                foreach (var method in methods)
                {
                    results.Add(await RunSingleTest(testId++, vc.Func, method, model, solverFactory, parser));
                }
            }

            GenerateReport(results);
            Console.WriteLine($"\nTesting complete. Report generated: StressTests/TestReport.txt");
            PrintSummary(results);
        }

        static EquationModel CreateDefaultModel(string func, string eps, string method)
        {
            var model = new EquationModel
            {
                Expression = func,
                Epsilon = eps,
                MaxIterations = "500"
            };

            // Basic logic to find a likely interval or guess for known test functions
            if (func.Contains("sin") || func.Contains("cos")) { model.A = "0"; model.B = "3.14"; model.InitialGuess = "1.5"; }
            else if (func.Contains("ln") || func.Contains("log")) { model.A = "0.1"; model.B = "5"; model.InitialGuess = "2"; }
            else if (func.Contains("1/x")) { model.A = "0.1"; model.B = "2"; model.InitialGuess = "1"; }
            else { model.A = "-10"; model.B = "10"; model.InitialGuess = "0"; }

            // Special case for x^2-4 on [-10, 10] Bisection won't work because f(-10)=96, f(10)=96.
            // Let's refine for functional tests to ensure they CAN pass if solvers are good.
            if (func == "x^2 - 4") { model.A = "0"; model.B = "5"; model.InitialGuess = "3"; }
            if (func == "x^3 - x - 2") { model.A = "1"; model.B = "2"; model.InitialGuess = "1.5"; }

            return model;
        }

        static async Task<TestResult> RunSingleTest(int id, string func, string method, EquationModel model, SolverFactory factory, IFunctionParser parser)
        {
            var tr = new TestResult { Id = id, Function = func, Method = method, Model = model };
            
            // 1. Validation Check
            var valError = Validator.ValidateEquationModel(model, method, parser);
            if (valError != null)
            {
                tr.Status = "VALIDATION_REJECTED";
                tr.ErrorMessage = valError;
                return tr;
            }

            // 2. Solving with Timeout
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            var sw = Stopwatch.StartNew();
            try
            {
                var solver = factory.CreateSolver(method);
                var result = await solver.SolveAsync(model, cts.Token);
                sw.Stop();
                tr.ExecutionTimeMs = sw.ElapsedMilliseconds;

                if (result.IsSuccess)
                {
                    tr.Root = result.Root;
                    tr.Iterations = result.TotalIterations;
                    try
                    {
                        tr.FRoot = parser.Evaluate(func, result.Root.Value);
                        double epsVal = Validator.ParseDouble(model.Epsilon, out _);
                        if (Math.Abs(tr.FRoot.Value) <= epsVal * 100) // allow some margin for numerical derivative
                        {
                            tr.Status = "PASS";
                        }
                        else
                        {
                            tr.Status = "FAIL_ACCURACY";
                            tr.ErrorMessage = $"f(root) = {tr.FRoot} is too large for epsilon {epsVal}";
                        }
                    }
                    catch (Exception ex)
                    {
                        tr.Status = "ERROR_EVAL";
                        tr.ErrorMessage = ex.Message;
                    }
                }
                else
                {
                    tr.Status = "FAIL_SOLVER";
                    tr.ErrorMessage = result.ErrorMessage;
                }
            }
            catch (OperationCanceledException)
            {
                tr.Status = "TIMEOUT";
                tr.ErrorMessage = "Execution exceeded 2 seconds";
            }
            catch (Exception ex)
            {
                tr.Status = "CRASH";
                tr.ErrorMessage = ex.ToString();
            }

            return tr;
        }

        static void GenerateReport(List<TestResult> results)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== EquationSolver Mass Testing Report ===");
            sb.AppendLine($"Generated on: {DateTime.Now}");
            sb.AppendLine();

            foreach (var r in results)
            {
                sb.AppendLine($"Test ID: {r.Id}");
                sb.AppendLine($"Function: {r.Function}");
                sb.AppendLine($"Method: {r.Method}");
                sb.AppendLine($"Parameters: A={r.Model.A}, B={r.Model.B}, IG={r.Model.InitialGuess}, Eps={r.Model.Epsilon}, MaxIter={r.Model.MaxIterations}");
                sb.AppendLine($"Status: {r.Status}");
                if (r.Root.HasValue) sb.AppendLine($"Root: {r.Root}");
                if (r.FRoot.HasValue) sb.AppendLine($"f(root): {r.FRoot:E4}");
                if (r.Iterations > 0) sb.AppendLine($"Iterations: {r.Iterations}");
                sb.AppendLine($"Time: {r.ExecutionTimeMs} ms");
                if (!string.IsNullOrEmpty(r.ErrorMessage)) sb.AppendLine($"Error: {r.ErrorMessage}");
                sb.AppendLine(new string('-', 40));
            }

            sb.AppendLine();
            sb.AppendLine("=== SUMMARY ===");
            int total = results.Count;
            int pass = results.Count(r => r.Status == "PASS");
            int rejected = results.Count(r => r.Status == "VALIDATION_REJECTED");
            int fail = total - pass - rejected;
            int crashes = results.Count(r => r.Status == "CRASH");

            sb.AppendLine($"Total Tests: {total}");
            sb.AppendLine($"Passed: {pass}");
            sb.AppendLine($"Validation Rejected: {rejected}");
            sb.AppendLine($"Failed: {fail}");
            sb.AppendLine($"Crashes: {crashes}");

            if (pass > 0)
            {
                sb.AppendLine($"Average Time (Pass): {results.Where(r => r.Status == "PASS").Average(r => r.ExecutionTimeMs):F2} ms");
                sb.AppendLine($"Average Iterations (Pass): {results.Where(r => r.Status == "PASS").Average(r => r.Iterations):F1}");
            }

            File.WriteAllText("TestReport.txt", sb.ToString());
        }

        static void PrintSummary(List<TestResult> results)
        {
            int total = results.Count;
            int pass = results.Count(r => r.Status == "PASS");
            int rejected = results.Count(r => r.Status == "VALIDATION_REJECTED");
            int crashes = results.Count(r => r.Status == "CRASH");

            Console.WriteLine($"\nTotal: {total} | Passed: {pass} | Rejected: {rejected} | Failed: {total - pass - rejected} | Crashes: {crashes}");
        }
    }

    class TestResult
    {
        public int Id { get; set; }
        public string Function { get; set; } = "";
        public string Method { get; set; } = "";
        public EquationModel Model { get; set; } = null!;
        public string Status { get; set; } = "";
        public double? Root { get; set; }
        public double? FRoot { get; set; }
        public int Iterations { get; set; }
        public long ExecutionTimeMs { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
