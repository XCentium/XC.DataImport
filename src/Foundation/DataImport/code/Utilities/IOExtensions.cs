using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using XC.Foundation.DataImport.Configurations;
using XC.Foundation.DataImport.Repositories.FileSystem;

namespace XC.Foundation.DataImport.Utilities
{
    public static class IOExtensions
    {
        public static string GetRootFolder(string path)
        {
            while (true)
            {
                string temp = Path.GetDirectoryName(path);
                if (String.IsNullOrEmpty(temp))
                    break;
                path = temp;
            }
            return path;
        }

        public static string FindMappingById(this IFileSystemRepository fileSystemRepository, string mappingId)
        {
            var folderPath = fileSystemRepository.EnsureFolder(DataImportConfigurations.MappingFolder);
            if (Directory.Exists(folderPath))
            {
                var files = Directory.GetFiles(folderPath);
                return files.FirstOrDefault(f => f.Contains(mappingId));
            }
            return string.Empty;
        }
        public static string FindBatchMappingById(this IFileSystemRepository fileSystemRepository, string mappingId)
        {
            var folderPath = fileSystemRepository.EnsureFolder(DataImportConfigurations.BatchMappingFolder);
            if (Directory.Exists(folderPath))
            {
                var files = Directory.GetFiles(folderPath);
                return files.FirstOrDefault(f => f.Contains(mappingId));
            }
            return string.Empty;
        }
        public static string FindFile(string rootFolder, string uid, string pattern)
        {
            if (Directory.Exists(rootFolder))
            {
                var files = Directory.GetFiles(rootFolder, pattern, SearchOption.AllDirectories);
                return files.Any(f => f.Contains(uid)) ? Path.Combine(rootFolder, files.FirstOrDefault(f => f.Contains(uid))) : string.Empty ;
            }
            return string.Empty;
        }

        public static string GetFolderForFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return string.Empty;
            }
            return Path.GetDirectoryName(filePath);
        }

        public static string GetParentFolderForFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return string.Empty;
            }

            var parent = GetFolderForFile(filePath);

            return Directory.GetParent(parent).ToString();
        }
    }
}