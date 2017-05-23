using System;
namespace XC.DataImport.Repositories.Repositories
{
    public interface IMigrationRepository
    {
        Sitecore.Data.Database Database { get; }
        Sitecore.Data.ID StringToID(string value);
    }
}
