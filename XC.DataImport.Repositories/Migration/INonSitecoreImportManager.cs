using System;
namespace XC.DataImport.Repositories.Migration
{
    public interface INonSitecoreImportManager : ISitecoreImportManagerBase
    {
        int GetJobItemCount(Action<string, string> statusMethod, string statusFilename);
        Tuple<int, string> GetStatus(string id);
        XC.DataImport.Repositories.Models.IMapping Mapping { get; set; }
        XC.DataImport.Repositories.Repositories.INonSitecoreDatabaseRepository SourceRepository { get; set; }
        XC.DataImport.Repositories.Repositories.INonSitecoreDatabaseRepository TargetRepository { get; set; }
    }
}
