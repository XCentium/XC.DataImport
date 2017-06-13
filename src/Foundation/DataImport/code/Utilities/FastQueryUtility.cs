using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace XC.Foundation.DataImport.Utilities
{
    public static class FastQueryUtility
    {
        /// <summary>
        /// Escapes the dashes.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public static string EscapeDashes(string path)
        {
            var newPath = new List<string>();
            var pathSegments = path.Split('/');
            lock (pathSegments)
            {
                foreach (var segment in pathSegments)
                {
                    if (segment.Contains(" ") || segment.Contains("-"))
                    {
                        newPath.Add(string.Format("#{0}#", segment));
                    }
                    else
                    {
                        newPath.Add(segment);
                    }
                }
            }
            return string.Join("/", newPath);
        }
    }
}