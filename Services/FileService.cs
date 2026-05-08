using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using EquationSolver.Models;

namespace EquationSolver.Services
{
    public class FileService : IFileService
    {
        public async Task SaveResultsAsync(string filePath, ResultModel result, string equationStr, string methodName)
        {
            if (filePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var data = new
                {
                    Equation = equationStr,
                    Method = methodName,
                    Result = result
                };
                string jsonString = JsonSerializer.Serialize(data, options);
                await File.WriteAllTextAsync(filePath, jsonString);
            }
            else
            {
                // Save as TXT with professional formatting
                var sb = new StringBuilder();
                sb.AppendLine("============================================================");
                sb.AppendLine("         ЗВІТ ПРО РОЗВ'ЯЗАННЯ НЕЛІНІЙНОГО РІВНЯННЯ         ");
                sb.AppendLine("============================================================");
                sb.AppendLine($"Дата та час:        {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
                sb.AppendLine($"Рівняння f(x):      {equationStr}");
                sb.AppendLine($"Метод розв'язання:  {methodName}");
                sb.AppendLine($"Статус:             {(result.IsSuccess ? "УСПІШНО" : "ПОМИЛКА")}");
                sb.AppendLine("------------------------------------------------------------");

                if (result.IsSuccess)
                {
                    sb.AppendLine($"Знайдений корінь X: {result.Root:F15}");
                    
                    // Evaluate f(root) for the report
                    double fRoot = 0;
                    try 
                    { 
                        var parser = new Parsers.FunctionParser();
                        fRoot = parser.Evaluate(equationStr, result.Root!.Value);
                    } catch { }
                    
                    sb.AppendLine($"Значення f(X):      {fRoot:E4}");
                    sb.AppendLine($"Кількість ітерацій: {result.TotalIterations}");
                    sb.AppendLine($"Час виконання:      {result.ElapsedMilliseconds} мс");
                    sb.AppendLine("------------------------------------------------------------");
                    sb.AppendLine();
                    sb.AppendLine("ТАБЛИЦЯ ІТЕРАЦІЙ:");
                    sb.AppendLine(string.Format("{0,-5} | {1,-20} | {2,-20} | {3,-15}", "№", "Xn", "f(Xn)", "Похибка"));
                    sb.AppendLine(new string('-', 68));
                    foreach (var iter in result.Iterations)
                    {
                        sb.AppendLine(string.Format("{0,-5} | {1,-20:F12} | {2,-20:E8} | {3,-15:E4}", 
                            iter.IterationNumber, iter.Xn, iter.FXn, iter.Error));
                    }
                }
                else
                {
                    sb.AppendLine($"Повідомлення про помилку: {result.ErrorMessage}");
                }

                sb.AppendLine("============================================================");
                await File.WriteAllTextAsync(filePath, sb.ToString());
            }
        }
    }
}
