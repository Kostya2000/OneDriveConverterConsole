using System.IO;

namespace Converter
{
    interface IReadFile
    {
        /// <summary>
        /// Чтение файла
        /// </summary>
        /// <param name = "fileName"></param>
        Stream Read(string fileName);
    }
}
