using System;
using System.IO;
using System.Net;
using Microsoft.Extensions.Logging;
using Converter.Exceptions;

namespace Converter
{
    class DocxToPdfConverter
    {
        private static ILogger _logger;
        private static string _client_id;
        private static string _client_secret;
        private static string _urlAuth;
        private static string fileExtension = "docx";
        private const int MaxFilesize = 4 * 1024 * 1024;
        private readonly Lazy<OneDriveConnection> connection = new Lazy<OneDriveConnection>(() => new OneDriveConnection(_client_id, _client_secret, _urlAuth, _logger, fileExtension));

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name = "client_id"> идентификатор клиента </param>
        /// <param name = "client_secret"> пароль клиента </param>
        /// <param name = "urlAuth"> url для аутентификации </param>
        /// <param name = "logger"> логгер </param>
        public DocxToPdfConverter(string client_id, string client_secret, string urlAuth, ILogger logger)
        {
            _logger = logger;
            _client_id = client_id;
            _client_secret = client_secret;
            _urlAuth = urlAuth;
        }

        /// <summary>
        /// Конвертация документа
        /// </summary>
        /// <param name = "docxStream"> Файл с расширением docx </param>
        /// <param name = "streamSize"> [Необязательный параметр] Размер данных </param>
        /// <returns> Файл с расширением pdf </returns>
        public Stream Convert(Stream docxStream, int streamSize = 0)
        {
            SendFile(docxStream, streamSize);
            return connection.Value.Download("pdf");
        }

        /// <summary>
        /// Определение запроса для отправки файла
        /// </summary>
        /// <param name = "stream"> Файл с расширением docx </param>
        /// <param name = "sizeStream"> [Необязательный параметр] Размер файла </param>
        private void SendFile(Stream stream, int sizeStream = 0)
        {
            _logger.LogInformation("Определение запроса для отправки файла...");
            if (    sizeStream > MaxFilesize 
                 || stream.Length > MaxFilesize
               )
            {
                connection.Value.Upload(stream, true);
                return;
            }
            connection.Value.Upload(stream);
            _logger.LogInformation("Файл отправлен успешно");
        }  
    }
}
