using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XC.DataImport.Repositories.Models
{
    [Serializable]
    public class MessageModel
    {
        public string Text { get; set; }
        public string Type { get; set; }
    }

    public enum MessageType
    {
        Error, Notification, Warning
    }
}
