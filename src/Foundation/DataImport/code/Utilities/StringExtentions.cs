using Sitecore.Data;
using Sitecore.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace XC.Foundation.DataImport.Utilities
{
    public static class StringExtentions
    {
        private const string _prefix = "XC.DataImport_";
        /// <summary>
        /// Strings to identifier.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static ID StringToID(this string value)
        {
            Assert.ArgumentNotNull(value, "value");
            return new ID(new Guid(MD5.Create().ComputeHash(Encoding.Default.GetBytes(_prefix + value))));
        }
    }
}