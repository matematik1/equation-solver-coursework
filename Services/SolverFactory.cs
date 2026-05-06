using System;
using EquationSolver.Services.Parsers;
using EquationSolver.Services.Solvers;

namespace EquationSolver.Services
{
    public class SolverFactory
    {
        private readonly IFunctionParser _parser;

        public SolverFactory(IFunctionParser parser)
        {
            _parser = parser;
        }

        public IEquationSolver CreateSolver(string methodName)
        {
            return methodName switch
            {
                "Бісекція" => new BisectionSolver(_parser),
                "Ньютон" => new NewtonSolver(_parser),
                "Січні" => new SecantSolver(_parser),
                _ => throw new ArgumentException("Невідомий метод розв'язання.", nameof(methodName))
            };
        }
    }
}
