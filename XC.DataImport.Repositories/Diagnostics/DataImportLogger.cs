using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XC.DataImport.Repositories.Diagnostics
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
