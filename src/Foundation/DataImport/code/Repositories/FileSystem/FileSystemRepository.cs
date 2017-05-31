using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using XC.Foundation.DataImport.Diagnostics;

namespace XC.Foundation.DataImport.Repositories.FileSystem
{
    public class FileSystemRepository : IFileSystemRepository
    {
        public string EnsureFolder(string folder)
        {
            if (!Directory.Exists(folder))
            {
                try
                {
                    Directory.CreateDirectory(folder);
                }
                catch (Exception ex)
                {
                    DataImportLogger.Log.Error(ex.Message, ex);
                    folder = "";
                }
            }
            return folder;
        }

    }
}