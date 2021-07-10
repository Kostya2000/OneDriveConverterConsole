using System.IO;

namespace Converter
{
    interface IWriteFile
    {
        /// <summary>
        /// Запись файла
        /// </summary>
        void Write(Stream stream, string format);
    }
}
