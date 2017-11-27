using Sitecore.Services.Infrastructure.Web.Http;
using System.Web.Mvc;
using XC.Foundation.DataImport.Diagnostics;

namespace XC.Foundation.DataImport.Controllers
{
    public abstract class BaseImportController : ServicesApiController
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