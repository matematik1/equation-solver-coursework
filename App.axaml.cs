using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using EquationSolver.Services;
using EquationSolver.Services.Parsers;
using EquationSolver.ViewModels;
using EquationSolver.Views;

namespace EquationSolver;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var parser = new FunctionParser();
            var solverFactory = new SolverFactory(parser);
            var fileService = new FileService();

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel(solverFactory, parser, fileService),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}