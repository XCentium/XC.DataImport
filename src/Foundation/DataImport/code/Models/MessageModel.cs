using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XC.Foundation.DataImport.Models
{
    [Serializable]
    public class MessageModel
    {
        public string text { get; set; }
        public string type { get; set; }
        public string temporary { get; set; }
        public string closable { get; set; }
        public string actions { get; set; }
    }

    public enum MessageType
    {
        Error, Notification, Warning
    }
}
