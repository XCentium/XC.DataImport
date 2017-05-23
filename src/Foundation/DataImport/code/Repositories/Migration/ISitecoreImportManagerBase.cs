using System;
namespace XC.DataImport.Repositories.Migration
{
    public interface ISitecoreImportManagerBase
    {
        void Add(string id, Action<string, string> statusMethod, string statusFilename);
        void Remove(string id);
        void Run(string id, Action<string, string> statusMethod, string statusFilename);
        void StartJob(string id, Action<string, string> statusMethod, string statusFileName);
        void RunFromCE(string id, Action<string, string> statusMethod, string statusFilename);
    }
}
