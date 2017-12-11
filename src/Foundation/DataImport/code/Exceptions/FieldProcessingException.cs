using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace XC.Foundation.DataImport.Exceptions
{
    public class FieldProcessingException : Exception
    {
        public FieldProcessingException() : base() { }
        public FieldProcessingException(string message): base(message) { }
        public FieldProcessingException(string message, Exception exception) : base(message, exception) { }
    }
}