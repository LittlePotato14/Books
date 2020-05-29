using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Net;

namespace BooksWork
{
    public class FileWorker
    {
        // Как назовём скачанный архив
        public string Name { get; set; }

        // Ссылка на диск
        public string Link { get; set; }

        // Завершена ли загрузка (оно ассинхронно, а мне надо дальше с фалом работать :( )
        public bool Completed { get; set; } = false;

        // Имя папки на диске
        public string ActualName { get; set; }

        /// <summary>
        /// Конструктор.
        /// </summary>
        public FileWorker(string name, string link, string actualName)
        {
            Name = name;
            Link = link;
            ActualName = actualName;
        }

        /// <summary>
        /// Скачивает файл по указанной ссылке.
        /// </summary>
        public void DownloadFromYaDisk()
        {
            try
            {
                if (File.Exists(Name + ".zip"))
                    File.Delete(Name + ".zip");

                WebClient webload = new WebClient();

                webload.DownloadFileCompleted += new AsyncCompletedEventHandler
                    ((object sender, AsyncCompletedEventArgs e) =>
                    {
                        Console.WriteLine("Загрузка завершена.");
                        Completed = true;
                    });

                webload.DownloadFileAsync(new Uri("https://getfile.dokpub.com/yandex/get/" + Link), Name + ".zip");
            }
            catch(Exception ex)
            {
                Console.WriteLine("Упс, что-то пошло не так...\n" + ex.Message);
            }
        }

        /// <summary>
        /// Распаковывает зип файл и удаляет зип.
        /// </summary>
        public void UnpackZip()
        {
            try
            {
                if (Directory.Exists(ActualName))
                    Directory.Delete(ActualName, true);

                ZipFile.ExtractToDirectory(Name + ".zip", @"../Debug");
                File.Delete(Name + ".zip");
                Console.WriteLine("Архив распакован.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Упс, что-то пошло не так...\n" + ex.Message);
            }
        }
    }
}
