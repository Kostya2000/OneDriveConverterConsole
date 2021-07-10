using System.IO;
using System;
using Microsoft.Extensions.Logging;

namespace Converter
{
    class FileRepository : IFile
    {
        private string name = null;
        private readonly ILogger logger;

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name = "logger"> Логгер </param>
        public FileRepository(ILogger logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Название файла
        /// </summary>
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        /// <summary>
        /// Чтение файла из диска
        /// </summary>
        /// <param name = "fileName"> Название файла </param>
        /// <exception cref = "FileNotFoundException"> Не удалось найти файл на диске </exception>
        public Stream Read(string fileName)
        {
            name = fileName;
            logger.LogInformation("Начало загрузки файла с диска");
            FileInfo file = new FileInfo(fileName);
            if (!file.Exists)
            {
                logger.LogWarning("Файл не найден");
                throw new FileNotFoundException("Файл не найден");
            }
            var stream = file.OpenRead() as Stream;
            logger.LogInformation("Файл успешно загружен из диска");
            return stream;
        }

        /// <summary>
        /// Запись файла на диск
        /// </summary>
        /// <param name = "stream"> Файл в заданном расширении </param>
        /// <param name = "format"> Расширение </param>
        public void Write(Stream stream, string format)
        {
            using var file = new FileStream(ReplaceExtension(name, format), FileMode.Create);
            logger.LogInformation("Начало выгрузки файла на диск");
            stream.CopyTo(file);
            file.FlushAsync();
            logger.LogInformation("Файл успешно загружен на диск");
        }

        /// <summary>
        /// Замена расширения файла
        /// </summary>
        /// <param name = "fileName"> Название файла </param>
        /// <param name = "format"> Формат файла </param>
        /// <returns> Название файла с расширением pdf </returns>
        private string ReplaceExtension(string fileName, string format)
        {
            var index = fileName.LastIndexOf('.');
            return fileName.Remove(index, fileName.Length - index) + $".{format}";
        }
    }
}

