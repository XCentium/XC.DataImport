using Sitecore.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using XC.DataImport.Repositories.Diagnostics;

namespace XC.DataImport.Controllers
{
    public abstract class ImportController : Controller
    {
        /// <summary>
        /// Writes the status.
        /// </summary>
        /// <param name="status">The status.</param>
        /// <param name="endLine">if set to <c>true</c> [end line].</param>
        public void WriteStatus(string status, string statusFileName)
        {
            try
            {
                using (var file = System.IO.File.AppendText(statusFileName))
                {
                    file.WriteLine(string.Format("<div>{0}</div>", status));
                }
            }
            catch
            {
                DataImportLogger.Log.Info(status);
            }
        }
    }
}