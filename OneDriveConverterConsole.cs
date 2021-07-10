using System;
using System.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Converter
{
    class OneDriveConverterConsole
    {
        static Microsoft.Extensions.Logging.ILogger logger;
        static string client_id;
        static string client_secret;
        static string tenant;
        static string urlAuth;

        static void Main(string[] args)
        {
            Init();
            if (args.Length < 1)
            {
                throw new ArgumentNullException("Название файла не передано");
            }
            //ConvertFileFromDiskSource(args[1]);
            ConvertFileFromDiskSource(args[0]);
        }

        /// <summary>
        /// Инициализация Ilogger и загрузка конфигурационных данных
        /// </summary>
        private static void Init()
        {
            var loggerFactory = new LoggerFactory();
            var loggerConfig = new LoggerConfiguration()
                .WriteTo.File("logs\\loggs.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();
            loggerFactory.AddSerilog(loggerConfig);
            logger = loggerFactory.CreateLogger<OneDriveConverterConsole>();
            logger.LogInformation("Выполняется загрузка конфигурации OneDrive");
            client_id = ConfigurationManager.AppSettings.Get("client_id");
            client_secret = ConfigurationManager.AppSettings.Get("client_secret");
            tenant = ConfigurationManager.AppSettings.Get("tenant");
            urlAuth = $"https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token";
            logger.LogInformation("Загрузка конфигурации OneDrive прошла успешно");
        }

        /// <summary>
        /// Конвертация файла из диска
        /// </summary>
        /// <param name = "fileSource"> Источник файла </param>
        private static void ConvertFileFromDiskSource(string fileSource)
        {
            var driveConverter = new DocxToPdfConverter(client_id, client_secret, urlAuth, logger);
            IFile file = new FileRepository(logger);
            file.Write(driveConverter.Convert(file.Read(fileSource)), "pdf");
        }
    }
}
