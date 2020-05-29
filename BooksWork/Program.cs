using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace BooksWork
{
    class Program
    {
        /// <summary>
        /// Словарик для перевода текста.
        /// </summary>
        static Dictionary<string, string> alphChars = new Dictionary<string, string>
        {
            {"A", "А"},
            {"B", "Б"},
            {"V", "В"},
            {"G", "Г"},
            {"D", "Д"},
            {"E", "Е"},
            {"J", "Ж"},
            {"Z", "З"},
            {"I", "И"},
            {"K", "К"},
            {"L", "Л"},
            {"M", "М"},
            {"N", "Н"},
            {"O", "О"},
            {"P", "П"},
            {"R", "Р"},
            {"S", "С"},
            {"T", "Т"},
            {"U", "У"},
            {"F", "Ф"},
            {"H", "Х"},
            {"C", "Ц"},
            {"Q", "КУ"},
            {"W", "У"},
            {"X", "КС"},
            {"Y" , "Ы"}
        };

        /// <summary>
        /// Точка входа.
        /// </summary>
        static async Task Main()
        {
            // Скачиваю книги с Яндекс диска.
            FileWorker fw = new FileWorker("books", "https://yadi.sk/d/fh3lefTcQ_hLLA", "Книги");
            fw.DownloadFromYaDisk();

            // Я запуталась и не понимаю, как убрать этот костыль, в ином случае будет исключение
            //*не может получить доступ к файлу*.
            while (!fw.Completed) { }

            // Распаковываю скачанный архив книг.
            fw.UnpackZip();
            Console.WriteLine();

            // Перевожу книги по порядку.
            FirstMethod(fw);
            Console.WriteLine();

            // Перевожу книги параллельно.
            SecondMethod(fw);
            Console.WriteLine();

            // Получаю книгу GET запросом и перевожу.
            await FromWeb("https://www.gutenberg.org/files/1342/1342-0.txt", fw);
        }

        /// <summary>
        /// Обработка книги с сайта.
        /// </summary>
        /// <param name="ur"> Ссылка. </param>
        static async Task FromWeb(string ur, FileWorker fw)
        {
            string response;

            // Получаю книгу.
            try
            {
                HttpClient client = new HttpClient();
                response = await client.GetStringAsync(ur);
                Console.WriteLine("Книга получена.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Хм, что-то пошло не так..\n" + ex.Message);
                return;
            }

            // Запускаю таймер.
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            // Перевод.
            Tuple<int, int> sums = Translit(fw.ActualName + "/", ".txt", "new_book_from_web", new string[] { response });
            Console.WriteLine($"Web file: {sums.Item1} -> {sums.Item2}");

            // Затраченное время.
            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:000}",
                ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds / 10);
            Console.WriteLine("Web time: " + elapsedTime);
        }

        /// <summary>
        /// Параллельно переводит книги.
        /// </summary>
        /// <param name="fw"></param>
        static void SecondMethod(FileWorker fw)
        {
            try
            {
                // Запуск таймера.
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                // Для всех непереведённых файлов директории.
                Parallel.ForEach(Directory.GetFiles(fw.ActualName).Where(x => !x.Contains("new")).ToList(), (path) =>
                {
                    // Перевод.
                    Tuple<int, int> sums = Translit(fw.ActualName + "/", Path.GetFileName(path), "new2_");
                    Console.WriteLine($"File {Path.GetFileName(path)}: {sums.Item1} -> {sums.Item2}");
                });

                // Затраченное время.
                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:000}",
                    ts.Hours, ts.Minutes, ts.Seconds,
                    ts.Milliseconds / 10);
                Console.WriteLine("Second method time: " + elapsedTime);
            }
            catch (AggregateException e)
            {
                Console.WriteLine("An action has thrown an exception. THIS WAS UNEXPECTED.\n{0}", e.InnerException.ToString());
            }
        }

        /// <summary>
        /// Синхронный перевод.
        /// </summary>
        static void FirstMethod(FileWorker fw)
        {
            // Запуск общего таймера.
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            // Для всех файлов в директории.
            foreach (string path in Directory.GetFiles(fw.ActualName))
            {
                // Запуск таймера для конкретного файла.
                Stopwatch currentWatch = new Stopwatch();
                currentWatch.Start();

                // Перевод.
                Tuple<int, int> sums = Translit(fw.ActualName + "/", Path.GetFileName(path));

                // Подсчёт времени для конкретного файла.
                currentWatch.Stop();
                Console.WriteLine($"File {Path.GetFileName(path)}: {sums.Item1} -> {sums.Item2}");
                TimeSpan currentSpan = currentWatch.Elapsed;
                string currentTime = String.Format("{0:00}:{1:00}:{2:00}.{3:000}",
                    currentSpan.Hours, currentSpan.Minutes, currentSpan.Seconds,
                    currentSpan.Milliseconds / 10);
                Console.WriteLine("Time: " + currentTime);
            }

            // Подсчёт общего времени.
            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:000}",
                ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds / 10);
            Console.WriteLine("First method time: " + elapsedTime);
        }

        /// <summary>
        /// Переводит книгу, используя словарь.
        /// </summary>
        /// <param name="path"> Путь к папке с книгами. </param>
        /// <param name="name"> Имя файла книги. </param>
        /// <param name="added"> Префикс для имени переведённой книги. </param>
        /// <param name="lines"> Строки для перевода. </param>
        /// <returns> Возвращает кол-во символов до и после перевода. </returns>
        static Tuple<int, int> Translit(string path, string name, string added = "new_", string[] lines = null)
        {
            // Считываю файл, если массив строк не был передан.
            try
            {
                if (lines is null) 
                    lines = File.ReadAllLines(path + name);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Упс, что-то пошло не так...\n" + ex.Message);
            }

            // Для сравнения кол-ва символов.
            int oldSum = 0;
            int newSum = 0;

            // Прохожу по всем строкам.
            for (int i = 0; i < lines.Length; i++)
            {
                StringBuilder newLine = new StringBuilder();

                // Для каждого символа в строке.
                foreach (char symb in lines[i])
                {
                    oldSum++;

                    // Заглавная буква.
                    if (alphChars.ContainsKey(symb.ToString()))
                    {
                        string newS = alphChars[symb.ToString()];
                        newLine.Append(newS);
                        newSum += newS.Length;
                    }

                    // Прописная буква.
                    else if (alphChars.ContainsKey(symb.ToString().ToUpper()))
                    {
                        string newS = alphChars[symb.ToString().ToUpper()].ToLower();
                        newLine.Append(newS);
                        newSum += newS.Length;
                    }

                    // Не буква.
                    else if (!Char.IsLetter(symb))
                    {
                        newSum++;
                        newLine.Append(symb.ToString());
                    }
                }

                // Заменяю старую строку на перевод.
                lines[i] = newLine.ToString();
            }

            // Записываю перевод книги.
            try
            {
                File.WriteAllLines(path + added + name, lines);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Упс, что-то пошло не так...\n" + ex.Message);
            }

            return new Tuple<int, int>(oldSum, newSum);
        }
    }
}
