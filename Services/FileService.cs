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
                // Save as TXT
                var sb = new StringBuilder();
                sb.AppendLine("=== Результати розв'язання рівняння ===");
                sb.AppendLine($"Рівняння: {equationStr}");
                sb.AppendLine($"Метод: {methodName}");
                sb.AppendLine($"Статус: {(result.IsSuccess ? "Успішно" : "Помилка")}");
                
                if (result.IsSuccess)
                {
                    sb.AppendLine($"Корінь: {result.Root}");
                    sb.AppendLine($"Кількість ітерацій: {result.TotalIterations}");
                    sb.AppendLine($"Час виконання: {result.ElapsedMilliseconds} мс");
                    sb.AppendLine();
                    sb.AppendLine("=== Таблиця ітерацій ===");
                    sb.AppendLine($"{"№",-5} | {"Xn",-20} | {"f(Xn)",-20} | {"Похибка",-20}");
                    sb.AppendLine(new string('-', 75));
                    foreach (var iter in result.Iterations)
                    {
                        sb.AppendLine($"{iter.IterationNumber,-5} | {iter.Xn,-20:F8} | {iter.FXn,-20:F8} | {iter.Error,-20:E4}");
                    }
                }
                else
                {
                    sb.AppendLine($"Повідомлення про помилку: {result.ErrorMessage}");
                }

                await File.WriteAllTextAsync(filePath, sb.ToString());
            }
        }
    }
}
