using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquationSolver.Models;
using EquationSolver.Services;
using EquationSolver.Services.Parsers;
using EquationSolver.Utils;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Annotations;
using OxyPlot.Axes;

namespace EquationSolver.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        private readonly SolverFactory _solverFactory;
        private readonly IFunctionParser _parser;
        private readonly IFileService _fileService;
        private CancellationTokenSource? _cancellationTokenSource;

        [ObservableProperty]
        private EquationModel _equation = new EquationModel { Expression = "x^3 - x - 2", A = "1", B = "2", InitialGuess = "1.5", Epsilon = "1e-5", MaxIterations = "100" };

        [ObservableProperty]
        private ObservableCollection<string> _availableMethods = new() { "Бісекція", "Ньютон", "Січні" };

        [ObservableProperty]
        private string _selectedMethod = "Бісекція";

        [ObservableProperty]
        private ObservableCollection<IterationData> _iterations = new();

        [ObservableProperty]
        private ResultModel? _currentResult;

        [ObservableProperty]
        private PlotModel _plotModel = new();

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _isErrorVisible;

        [ObservableProperty]
        private string _rootCoordinates = string.Empty;

        [ObservableProperty]
        private bool _isRootVisible;

        public bool IsNewtonMethod => SelectedMethod == "Ньютон";

        // An action to request a file save path from the View (handled in code-behind)
        public Func<Task<string?>>? RequestSaveFilePathAsync { get; set; }

        public MainViewModel(SolverFactory solverFactory, IFunctionParser parser, IFileService fileService)
        {
            _solverFactory = solverFactory;
            _parser = parser;
            _fileService = fileService;
            InitializePlot();
        }

        public MainViewModel()
        {
            // Designer parameterless constructor
            _parser = new FunctionParser();
            _solverFactory = new SolverFactory(_parser);
            _fileService = new FileService();
            InitializePlot();
        }

        private void InitializePlot()
        {
            PlotModel = new PlotModel { Title = "Графік функції" };
            PlotModel.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "X", MajorGridlineStyle = LineStyle.Solid, MinorGridlineStyle = LineStyle.Dot });
            PlotModel.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Y", MajorGridlineStyle = LineStyle.Solid, MinorGridlineStyle = LineStyle.Dot });
        }

        partial void OnSelectedMethodChanged(string value)
        {
            OnPropertyChanged(nameof(IsNewtonMethod));
        }

        private string FormatValue(double value, string epsilonStr)
        {
            int decimals = 5; // default
            double eps = Validator.ParseDouble(epsilonStr, out bool epsSuccess);
            if (epsSuccess && eps > 0 && eps < 1)
            {
                decimals = (int)Math.Ceiling(Math.Abs(Math.Log10(eps)));
                if (decimals > 15) decimals = 15;
            }
            return value.ToString($"F{decimals}");
        }

        [RelayCommand]
        private async Task SolveAsync()
        {
            ClearErrors();
            
            var validationError = Validator.ValidateEquationModel(Equation, SelectedMethod, _parser);
            if (validationError != null)
            {
                ShowError(validationError);
                IsRootVisible = false;
                return;
            }

            IsBusy = true;
            Iterations.Clear();
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                var solver = _solverFactory.CreateSolver(SelectedMethod);
                CurrentResult = await solver.SolveAsync(Equation, _cancellationTokenSource.Token);

                double a = Validator.ParseDouble(Equation.A, out _);
                double b = Validator.ParseDouble(Equation.B, out _);

                if (CurrentResult.IsSuccess)
                {
                    foreach (var iter in CurrentResult.Iterations)
                    {
                        Iterations.Add(iter);
                    }
                    
                    IsRootVisible = true;
                    string formattedX = FormatValue(CurrentResult.Root!.Value, Equation.Epsilon);
                    double rootY = _parser.Evaluate(Equation.Expression, CurrentResult.Root.Value);
                    string formattedY = FormatValue(rootY, Equation.Epsilon);
                    RootCoordinates = $"X = {formattedX}\nf(X) = {formattedY}";

                    UpdatePlot(Equation.Expression, a, b, CurrentResult.Root!.Value);
                }
                else
                {
                    IsRootVisible = false;
                    ShowError(CurrentResult.ErrorMessage ?? "Невідома помилка під час обчислення.");
                    UpdatePlot(Equation.Expression, a, b, null);
                }
            }
            catch (OperationCanceledException)
            {
                ShowError("Обчислення скасовано користувачем.");
            }
            catch (Exception ex)
            {
                ShowError($"Критична помилка: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            _cancellationTokenSource?.Cancel();
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            if (CurrentResult == null || !CurrentResult.IsSuccess)
            {
                ShowError("Немає успішних результатів одного методу для збереження.");
                return;
            }

            if (RequestSaveFilePathAsync != null)
            {
                var filePath = await RequestSaveFilePathAsync();
                if (!string.IsNullOrEmpty(filePath))
                {
                    await _fileService.SaveResultsAsync(filePath, CurrentResult, Equation.Expression, SelectedMethod);
                }
            }
        }

        private void ShowError(string message)
        {
            ErrorMessage = message;
            IsErrorVisible = true;
        }

        private void ClearErrors()
        {
            ErrorMessage = string.Empty;
            IsErrorVisible = false;
        }

        private void UpdatePlot(string expression, double a, double b, double? root)
        {
            PlotModel.Series.Clear();
            PlotModel.Annotations.Clear();
            PlotModel.Title = $"Графік f(x) = {expression}";

            var lineSeries = new LineSeries
            {
                Title = "f(x)",
                Color = OxyColors.Blue,
                StrokeThickness = 2
            };

            // Expand interval slightly for better visual
            double minX = Math.Min(a, b) - Math.Abs(b - a) * 0.2;
            double maxX = Math.Max(a, b) + Math.Abs(b - a) * 0.2;
            
            // If the user interval is too small or inverted
            if (minX >= maxX)
            {
                minX = -10; maxX = 10;
            }

            int pointsCount = 500;
            double step = (maxX - minX) / pointsCount;

            for (double x = minX; x <= maxX; x += step)
            {
                try
                {
                    double y = _parser.Evaluate(expression, x);
                    if (double.IsFinite(y))
                    {
                        lineSeries.Points.Add(new DataPoint(x, y));
                    }
                }
                catch
                {
                    // Ignore math errors for specific points (like division by zero)
                }
            }

            PlotModel.Series.Add(lineSeries);

            // Add X-axis line (Y=0)
            PlotModel.Annotations.Add(new LineAnnotation
            {
                Type = LineAnnotationType.Horizontal,
                Y = 0,
                Color = OxyColors.Black,
                StrokeThickness = 1,
                LineStyle = LineStyle.Solid
            });

            // Mark the root if found
            if (root.HasValue)
            {
                double rootY = _parser.Evaluate(expression, root.Value);
                if (double.IsFinite(rootY))
                {
                     var rootPoint = new ScatterSeries
                     {
                         MarkerType = MarkerType.Circle,
                         MarkerSize = 6,
                         MarkerFill = OxyColors.Red,
                         Title = "Знайдений корінь"
                     };
                     rootPoint.Points.Add(new ScatterPoint(root.Value, rootY));
                     PlotModel.Series.Add(rootPoint);
                }
            }

            PlotModel.ResetAllAxes(); // IMPORTANT: resets scaling to fit the new data
            PlotModel.InvalidatePlot(true);
        }
    }
}