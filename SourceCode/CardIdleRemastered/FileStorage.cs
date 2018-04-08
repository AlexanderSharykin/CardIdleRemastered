using System;
using System.IO;
using System.Text;

namespace CardIdleRemastered
{
    public class FileStorage
    {
        private object _rwLock = new object();

        public FileStorage()
        {
        }

        public FileStorage(string fileName)
        {
            this.FileName = fileName;
        }

        public string FileName { get; set; }

        public string ReadContent()
        {
            lock (_rwLock)
            {
                if (File.Exists(FileName) == false)
                {
                    Logger.Info(FileName + Environment.NewLine + "Storage file not found.");
                    File.Create(FileName);
                    return null;
                }

                return File.ReadAllText(FileName, Encoding.UTF8);
            }
        }

        public void WriteContent(string content)
        {
            lock (_rwLock)
            {
                File.WriteAllText(FileName, content, Encoding.UTF8);
            }
        }
    }
}
