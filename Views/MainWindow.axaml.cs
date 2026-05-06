using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using EquationSolver.ViewModels;

namespace EquationSolver.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            System.Console.WriteLine("MainWindow created");
        }

        protected override void OnDataContextChanged(System.EventArgs e)
        {
            base.OnDataContextChanged(e);

            if (DataContext is MainViewModel viewModel)
            {
                viewModel.RequestSaveFilePathAsync = async () =>
                {
                    var storageProvider = this.StorageProvider;

                    var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                    {
                        Title = "Зберегти результати",
                        DefaultExtension = "txt",
                        SuggestedFileName = "РезультатиОбчислень.txt",
                        FileTypeChoices = new[]
                        {
                            new FilePickerFileType("Текстовий файл") { Patterns = new[] { "*.txt" } },
                            new FilePickerFileType("JSON файл") { Patterns = new[] { "*.json" } }
                        }
                    });

                    return file?.Path.LocalPath;
                };
            }
        }
    }
}