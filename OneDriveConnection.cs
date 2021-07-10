using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Logging;
using Converter.Exceptions;

namespace Converter
{
    class OneDriveConnection
    {
        private JToken token;
        private JToken uploadUrl;
        private readonly ILogger logger;
        private readonly string client_id;
        private readonly string client_secret;
        private readonly string urlAuth;
        private const int pushSize = 327680;
        private string guid = Guid.NewGuid()
                                  .ToString();
        private readonly WebHeaderCollection authorization = new WebHeaderCollection();
        private string host = $"https://graph.microsoft.com/v1.0/drive/root:/";

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name = "client_id"> идентификатор клиента </param>
        /// <param name = "client_secret"> пароль клиента </param>
        /// <param name = "urlAuth"> url для аутентификации </param>
        /// <param name = "logger"> логгер </param>
        /// <param name = "fileExtension"> Расширение файла </param>
        public OneDriveConnection(string client_id, string client_secret, string urlAuth, ILogger logger, string fileExtension)
        {
            this.logger = logger;
            this.client_id = client_id;
            this.client_secret = client_secret;
            this.urlAuth = urlAuth;
            guid = guid + $".{fileExtension}";
        }

        /// <summary>
        /// Деструктор
        /// </summary>
        ~OneDriveConnection()
        {
            Delete();
        }

        /// <summary>
        /// Загрузка на OneDrive
        /// </summary>
        /// <param name = "stream"> файл с расширением docx </param>
        /// <param name = "large"> [Необязательный параметр] Указываем, если файл размером больше 4МБ </param>
        public void Upload(Stream stream, object large = null)
        {
            if (large == null)
            {
                FilesSending(stream);
                return;
            }
            SendLargeFile(stream);
        }

        /// <summary>
        /// Скачиваем файл с сервера
        /// </summary>
        /// <param name = "format"> Формат скачиваемого файла </param>
        /// <returns> Файл в указанном формате </returns>
        public Stream Download(string format)
        {
            logger.LogInformation($"Запрос на скачивания {guid} файла...");
            var header = new WebHeaderCollection
            {
                Authorization
            };
            var request = WebRequest.Create(host + $"{guid}:/content?format={format}");
            request.Method = "GET";
            request.Headers = header;
            request.ContentType = "application/pdf";
            var resp = request.GetResponse();
            if (resp == null)
            {
                logger.LogError("Не удалось подключится к серверу");
                throw new OfflineException("Не удалось подключится к серверу");
            }
            var stream = resp.GetResponseStream();
            logger.LogInformation("Файл успешно загружен из OneDrive");
            return stream;
        }
        
        /// <summary>
        /// Отправка целого файла
        /// </summary>
        /// <param name = "stream"> Файл </param>
        private void FilesSending(Stream stream)
        {
            logger.LogInformation($"Запрос на отправку {guid} файла (до 4 МБ)...");
            var header = new WebHeaderCollection
            {
                Authorization
            };
            var request = WebRequest.Create(host + $"{guid}:/content");
            request.Headers = header;
            request.Method = "PUT";
            using var requestStream = request.GetRequestStream();
            stream.CopyTo(requestStream);
            logger.LogInformation($"Файл {guid} успешно загружен");
            _ = request.GetResponse();
        }

        /// <summary>
        /// Отправка файла с размром больше 4 МБ на сервер
        /// </summary>
        /// <param name = "stream"> Файл с расширением docx </param>
        private void SendLargeFile(Stream stream)
        {
            logger.LogInformation("Запрос на отправку файла размером больше 4МБ на OneDrive...");
            int temporarySize = 0;
            int counter = 0;
            logger.LogInformation("Отправка фрагментов:");
            while (temporarySize + pushSize < stream.Length - 1)
            {
                logger.LogInformation((++counter).ToString() + " Content-Range " + "bytes " + (temporarySize).ToString() + "-" + (temporarySize + pushSize - 1).ToString() + "/" + (stream.Length).ToString());
                FramesSending(stream, temporarySize, temporarySize + pushSize - 1, pushSize);
                if (temporarySize + pushSize >= stream.Length)
                {
                    stream.Position = temporarySize + 1;
                    break;
                }
                stream.Position = temporarySize + pushSize;
                temporarySize += pushSize;
            }
            logger.LogInformation("Отправка последнего фрагмента:");
            if (temporarySize != stream.Length - 1)
            {
                logger.LogInformation("Content-Range " + "bytes " + (temporarySize).ToString() + "-" + (stream.Length - 1).ToString() + "/" + (stream.Length).ToString());
                FramesSending(stream, temporarySize, stream.Length - 1, (int)stream.Length);
            }
            logger.LogInformation("Файл успешно передан на сервера");
        }

        /// <summary>
        /// Отправка фрейма
        /// </summary>
        /// <param name = "stream"> Фрейм </param>
        /// <param name = "beginPosition"> Позиция начала фрейма </param>
        /// <param name = "endPosition"> Позиция конца фрейма </param>
        /// <param name = "pushSize"> Размер фрагмента </param>
        private void FramesSending(Stream stream, long beginPosition, long endPosition, int pushSize)
        {
            var header = new WebHeaderCollection
            {
                Authorization,
                { "Content-Length", (endPosition - beginPosition + 1).ToString() },
                { "Content-Range", "bytes " + (beginPosition).ToString() + "-" + (endPosition).ToString() + "/" + (stream.Length).ToString() }
            };
            var request = WebRequest.Create(UploadUrl(guid).ToString());
            request.Headers = header;
            request.Method = "PUT";
            request.ContentType = "multipart/form-data";
            using var requestStream = request.GetRequestStream();
            stream.CopyTo(requestStream, pushSize);
            _ = request.GetResponse();
        }

        /// <summary>
        /// Получение ресурс для отправки диапозонов байт
        /// </summary>
        /// <param name = "guid"> GUID файла </param>
        /// <returns> Url адрес приемника </returns>
        private JToken UploadUrl(string guid)
        {
            return uploadUrl ??= GetUploadUrl(guid);
        }

        /// <summary>
        /// Аутентификация по принципу lazy
        /// </summary>
        private JToken Token
        {
            get { return token ??= GetToken(); }
        }

        /// <summary>
        /// Аутентификация с OneDrive
        /// </summary>
        /// <returns> Токен доступа </returns>
        /// <exception cref = "OfflineException"> Не удалось подключиться к серверу </exception>
        private JToken GetToken()
        {
            var body = new Dictionary<string, string>
            {
                { "client_id", client_id },
                { "scope", ".default" },
                { "grant_type", "client_credentials" },
                { "client_secret", client_secret }
            };
            using var encodedBody = new FormUrlEncodedContent(body).ReadAsByteArrayAsync();

            var request = WebRequest.Create(urlAuth);
            request.Method = "POST";
            using var requestStream = request.GetRequestStream();
            if (requestStream == null)
            {
                logger.LogError("Не удалось подключится к серверу");
                throw new OfflineException("Не удалось подключится к серверу");
            }
            requestStream.Write(encodedBody.Result, 0, encodedBody.Result.Length);
            var responseMessage = request.GetResponse();
            if (responseMessage == null)
            {
                logger.LogCritical("Соединение разорвано");
                throw new OfflineException();
            }
            var response = new StreamReader(responseMessage.GetResponseStream());
            return GetAttrValue(response, "access_token");
        }

        /// <summary>
        /// Запрос на получение ресурса для отправки большого файла
        /// </summary>
        /// <returns> Url адрес приемника </returns>
        /// <exception cref = "OfflineException"> Не удалось подключиться к серверу </exception>
        private JToken GetUploadUrl(string guid)
        {
            var header = new WebHeaderCollection
            {
                Authorization
            };
            logger.LogInformation("Выполняется запрос на получение URL с OneDrive");
            var request = WebRequest.Create(host + $"{guid}:/createUploadSession");
            request.Method = "POST";
            request.Headers = header;
            using var requestStream = request.GetRequestStream();
            if (requestStream == null)
            {
                logger.LogError("Не удалось подключится к серверу");
                throw new OfflineException("Не удалось подключится к серверу");
            }
            var response = request.GetResponse() as HttpWebResponse;
            var responseStream = new StreamReader(response.GetResponseStream());
            if (responseStream == null)
            {
                logger.LogCritical("Не удалось подключиться к серверу");
                throw new OfflineException("Не удалось подключиться к серверу");
            }
            return GetAttrValue(responseStream, "uploadUrl");
        }

        /// <summary>
        /// Получаем значение по заданному атрибуту из сообщения от сервера
        /// </summary>
        /// <param name = "responseMessage"> Сообщение от сервера </param>
        /// <param name = "attr"> Заданный атрибут </param>
        /// <returns> Значение атрибута </returns>
        /// <exception cref = "HttpRequestException"> Неправильный запрос </exception>
        private JToken GetAttrValue(StreamReader responseMessage, string attr)
        {
            var responseString = responseMessage.ReadToEnd();
            var document = JObject.Parse(responseString);
            var attrValue = document[attr];
            if (attrValue == null)
            {
                logger.LogCritical("Неправильный запрос к серверу");
                throw new HttpRequestException("Неправильный запрос к серверу");
            }
            logger.LogInformation($"Значение атрибута {attr} получен успешно");
            return attrValue;
        }

        /// <summary>
        ///  Токен авторизации
        /// </summary>
        private WebHeaderCollection Authorization
        {
            get { 
                  if (authorization.Count == 0) 
                     authorization.Add("Authorization", "Bearer " + Token.ToString()); 
                  return authorization; 
                }
        }
        
        /// <summary>
        /// Удаление файл
        /// </summary>
        /// <exception cref = "OfflineException"> Проблемы с сетью </exception>
        private void Delete()
        {
            logger.LogInformation($"Запрос на удаление {guid} файла...");
            var header = new WebHeaderCollection
            {
                Authorization
            };
            var request = WebRequest.Create(host + $"{guid}");
            request.Method = "DELETE";
            request.Headers = header;
            if (!(request.GetResponse() is HttpWebResponse resp))
            {
                logger.LogError("Не удалось подключится к серверу");
                throw new OfflineException("Не удалось подключится к серверу");
            }
            _ = resp.GetResponseStream();
            logger.LogInformation($"Файл {guid} успешно удален");
        }
    }
}
