using System.Net;

namespace Converter.Exceptions
{
    class OfflineException : WebException
    {
        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name = "message"> Сообщение </param>
        public OfflineException(string message = null) 
            : base(message)
        { }
    }
}
