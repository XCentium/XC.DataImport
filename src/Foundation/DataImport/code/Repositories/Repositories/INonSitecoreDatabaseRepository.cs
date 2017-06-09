using Sitecore.Data.Items;
using System;
using System.Collections.Generic;
using System.Data;
namespace XC.DataImport.Repositories.Repositories
{
    public interface INonSitecoreDatabaseRepository
    {
        Sitecore.Data.Database Database { get; }
        int GetSourceItemsForImportCount(Action<string, string> statusMethod, string statusFilepath, Tuple<string, string, string> filter = null);
        DataTable GetDataSet(Action<string, string> statusMethod, string statusFilepath, Tuple<string, string, string> filter = null);
        Item MigrateItem(DataRow dataRow, Item parentItem, DateTime startDate, int i, Action<string, string> statusMethod, string statusFilepath);
        void ClearMultilistFieldValues(Action<string, string> statusMethod, string statusFilepath, DataTable dataSet, Tuple<string, string, string> filter = null);
        void ClearMultilistFieldValues(Action<string, string> statusMethod, string statusFilepath, Item item);
        List<Item> RetrieveItemsToProcess(Tuple<string, string, string> filter = null);

        bool DetailedLogging { get; }
    }
}
