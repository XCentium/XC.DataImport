using log4net;

namespace XC.Foundation.DataImport.Diagnostics
{
    public class DataImportLogger
    {
        private static ILog log;

        public static ILog Log
        {
            get
            {
                return log ?? (log = LogManager.GetLogger(typeof(DataImportLogger)));
            }
        }
    }
}
