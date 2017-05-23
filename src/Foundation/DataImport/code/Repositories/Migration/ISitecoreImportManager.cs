using System;
namespace XC.DataImport.Repositories.Migration
{
    public interface ISitecoreImportManager : ISitecoreImportManagerBase
    {
        int GetJobItemCount();
        Tuple<int, string> GetStatus(string id);
        XC.Foundation.DataImport.Models.IMappingModel Mapping { get; set; }
        XC.DataImport.Repositories.Repositories.ISitecoreDatabaseRepository SourceRepository { get; set; }
        XC.DataImport.Repositories.Repositories.ISitecoreDatabaseRepository TargetRepository { get; set; }
    }
}
