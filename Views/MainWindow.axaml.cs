using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
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

            // Add global key down handler for shortcut support
            this.AddHandler(KeyDownEvent, OnPreviewKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        }

        private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
        {
            // Reliable Shortcut for '^': Ctrl + E (Exponent)
            if (e.Key == Key.E && e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                var focusManager = TopLevel.GetTopLevel(this)?.FocusManager;
                var focusedElement = focusManager?.GetFocusedElement();

                if (focusedElement is TextBox textBox && textBox.DataContext == (this.DataContext as MainViewModel)?.Equation)
                {
                    int caretIndex = textBox.CaretIndex;
                    string currentText = textBox.Text ?? string.Empty;
                    textBox.Text = currentText.Insert(caretIndex, "^");
                    textBox.CaretIndex = caretIndex + 1;
                    e.Handled = true;
                }
            }
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