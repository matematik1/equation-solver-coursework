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
        }

        public void MathButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string template)
            {
                InsertAtCursor(template);
            }
        }

        private void InsertAtCursor(string text)
        {
            var textBox = this.FindControl<TextBox>("FunctionInput");
            
            if (textBox == null)
            {
                var focusManager = TopLevel.GetTopLevel(this)?.FocusManager;
                var focusedElement = focusManager?.GetFocusedElement();
                if (focusedElement is TextBox fb) textBox = fb;
            }

            if (textBox != null)
            {
                int caretIndex = textBox.CaretIndex;
                string currentText = textBox.Text ?? string.Empty;
                textBox.Text = currentText.Insert(caretIndex, text);
                
                if (text.EndsWith("()"))
                {
                    textBox.CaretIndex = caretIndex + text.Length - 1;
                }
                else
                {
                    textBox.CaretIndex = caretIndex + text.Length;
                }
                
                textBox.Focus(); 
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
