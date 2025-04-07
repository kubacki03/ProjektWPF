using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjektWPF.Models
{
    public class ConversationMessage
    {
        public long Id { get; set; }
        public long UserAssistantId { get; set; }
        public string Sender { get; set; } 
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }

      
    }

}
