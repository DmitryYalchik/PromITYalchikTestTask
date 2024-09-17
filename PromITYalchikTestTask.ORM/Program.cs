using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Logging;
using PromITYalchikTestTask.ORM.DataBase;
using PromITYalchikTestTask.ORM.Utils;
using PromITYalchikTestTask.SqlClient.Models;

internal class Program
{
    private static async Task Main(string[] args)
    {
        try
        {
            string connectionString = GetConnectionString();
            Console.WriteLine("Производится попытка подключения...");
            using (var context = new WordsDbContext(connectionString))
            {
                await EnsureDatabaseCreatedAsync(context);
            }
            Console.WriteLine("Подключение успешно установлено");

            Console.WriteLine("Введите наименование текстового файла, находящегося в каталоге с программой (без расширения .txt):");
            string filePath = Console.ReadLine() + ".txt";

            var wordCount = ReadFile(filePath);

            await UpdateDatabaseConcurrently(wordCount, connectionString);

            Console.WriteLine("Программа успешно завершила работу\nНажмите любую кнопку, чтобы закрыть");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"[ ОШИБКА ]");
            Console.ResetColor();
            Console.WriteLine($"\t{ex.Message}\nВнутренняя ошибка: {ex.InnerException}\n\nНажмите любую кнопку, чтобы закрыть");
        }
        finally
        {
            Console.ReadKey();
        }
    }

    /// <summary>
    /// Собирает по ответам/параметрам единую строку подключения
    /// </summary>
    /// <returns>Собранную строку подключения</returns>
    static string GetConnectionString()
    {
        Console.WriteLine("Введите название сервера:");
        string server = Console.ReadLine();

        Console.WriteLine("Введите имя базы:");
        string database = Console.ReadLine();

        Console.WriteLine("Использовать авторизацию Windows? (y/n):");
        bool useWindowsAuth = Console.ReadLine()?.ToLower() == "y";

        string userId = "";
        string password = "";
        if (!useWindowsAuth)
        {
            Console.WriteLine("Введите имя пользователя:");
            userId = Console.ReadLine();

            Console.WriteLine("Введите пароль:");
            password = Console.ReadLine();
        }

        return BuildConnectionString(server, database, useWindowsAuth, userId, password);
    }

    /// <summary>
    /// Сборка единой строки подключения в зависимости от типа авторизации
    /// </summary>
    /// <param name="server">Название сервера</param>
    /// <param name="database">Название базы</param>
    /// <param name="useWindowsAuth">Вид авторизации при подключении (Windows - true, SQL - false)</param>
    /// <param name="userId">Логин пользователя</param>
    /// <param name="password">Пароль пользователя</param>
    /// <returns>Единую строку подключения</returns>
    static string BuildConnectionString(string server, string database, bool useWindowsAuth, string userId, string password)
    {
        if (useWindowsAuth)
        {
            return $"Server={server};Database={database};Trusted_Connection=True;TrustServerCertificate=true;";
        }
        else
        {
            return $"Server={server};Database={database};User Id={userId};Password={password};TrustServerCertificate=true;";
        }
    }

    /// <summary>
    /// Стандартным методом проверяем базу на существование (если нету, создаёт)
    /// </summary>
    /// <param name="context">Контекст бд</param>
    /// <returns></returns>
    static async Task EnsureDatabaseCreatedAsync(WordsDbContext context)
    {
        await context.Database.EnsureCreatedAsync();
    }

    /// <summary>
    /// Читает файл.
    /// Regex'ом находит все слова, удовлетворяющие условия. 
    /// Собирает словарь: если слово есть, +1 к числу; если нет - 1.
    /// </summary>
    /// <param name="filePath">Название файла</param>
    /// <returns>Словарь <'Слово', 'Количество'>, где число использований 4 и более</returns>
    static Dictionary<string, int> ReadFile(string filePath)
    {
        var wordCount = new Dictionary<string, int>();
        string text = File.ReadAllText(filePath);
        var words = Regex.Matches(text, @"\b[а-яА-Яa-zA-Z]{3,20}\b");

        foreach (Match match in words)
        {
            string word = match.Value.ToLower();
            if (wordCount.ContainsKey(word))
            {
                wordCount[word]++;
            }
            else
            {
                wordCount[word] = 1;
            }
        }

        return wordCount.Where(kvp => kvp.Value >= 4).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <summary>
    /// Обновляет данные в бд с использованием конкурентного доступа (по-очереди среди подключённых).
    /// Если на половине "пути" ломается, то не записывает вовсе, чтобы битых или частичных данных не было в базе
    /// </summary>
    /// <param name="wordCount">Словарь <'Слово', 'Количество'>, где число использований 4 и более</param>
    /// <param name="connectionString"></param>
    /// <returns></returns>
    static async Task UpdateDatabaseConcurrently(Dictionary<string, int> wordCount, string connectionString)
    {
        using (var context = new WordsDbContext(connectionString))
        {
            using (var transaction = await context.Database.BeginTransactionAsync())
            {
                try
                {
                    foreach (var entry in wordCount)
                    {
                        string wordText = entry.Key;
                        int count = entry.Value;

                        // SELECT TOP 1 * FROM Words
                        // WHERE Word = N'{ wordText }'
                        var existingWord = await context.Words
                            .Where(w => w.Word == wordText)
                            .FirstOrDefaultAsync();

                        if (existingWord != null)
                        {
                            existingWord.Count += count;

                            // UPDATE Words SET
                            // Word = N'{ existingWord.Word }',
                            // Count = { existingWord.Count }
                            // WHERE Id = N'{ existingWord.Id }'
                            context.Words.Update(existingWord);

                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.Write($"[ УВЕЛИЧЕНО ЗНАЧЕНИЕ ]");
                            Console.ResetColor();
                            Console.WriteLine(String.Format("{0,20} | {1,5}", existingWord.Word, existingWord.Count));
                        }
                        else
                        {
                            var newWord = new WordEntity { Word = wordText, Count = count };

                            // INSERT INTO Words (Word, Count)
                            // VALUES (N'{ existingWord.Word }', { existingWord.Count })
                            context.Words.Add(newWord);
                            
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.Write($"[ ДОБАВЛЕНО ЗНАЧЕНИЕ ]");
                            Console.ResetColor();
                            Console.WriteLine(String.Format("{0,20} | {1,5}", newWord.Word, newWord.Count));
                        }

                        await context.SaveChangesAsync();
                    }

                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write($"[ ОШИБКА ]");
                    Console.ResetColor();
                    Console.WriteLine($"\t{ex.Message}\nВнутренняя ошибка: {ex.InnerException}\n\nНажмите любую кнопку, чтобы закрыть");
                    Console.ReadKey();
                }
            }
        }
    }
}