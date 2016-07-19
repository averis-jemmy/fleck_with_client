using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
    public enum MessageType
    {
        Login,
        Message,
        Typing,
        Logout
    }

    public class ChatEntity
    {
        public string UserName { get; set; }
        public MessageType MessageType { get; set; }
        public string Message { get; set; }
        public Guid Token { get; set; }
    }
}
