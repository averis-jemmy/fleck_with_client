using Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Fleck.Samples.ConsoleApp
{
    class Server
    {
        public static List<ChatEntity> entities = new List<ChatEntity>();

        static void Main()
        {
            FleckLog.Level = LogLevel.Debug;
            var allSockets = new List<IWebSocketConnection>();
            var server = new WebSocketServer("ws://0.0.0.0:8181");
            server.Start(socket =>
                {
                    socket.OnOpen = () =>
                        {
                            Console.WriteLine("Open!");
                            allSockets.Add(socket);
                        };
                    socket.OnClose = () =>
                        {
                            Console.WriteLine("Close!");
                            allSockets.Remove(socket);
                        };
                    socket.OnMessage = message =>
                        {
                            try
                            {
                                var entity = JsonConvert.DeserializeObject<ChatEntity>(message);
                                List<ChatEntity> newEntities = new List<ChatEntity>();
                                if (entity.MessageType == MessageType.Login)
                                {
                                    foreach(ChatEntity model in entities)
                                    {
                                        if (model.UserName != entity.UserName)
                                            newEntities.Add(model);
                                    }

                                    ChatEntity ce = new ChatEntity();
                                    ce.UserName = entity.UserName;
                                    ce.Token = Guid.NewGuid();
                                    ce.MessageType = MessageType.Login;
                                    newEntities.Add(ce);
                                    entities = newEntities;

                                    message = JsonConvert.SerializeObject(ce);
                                }
                                if (entity.MessageType == MessageType.Logout)
                                {
                                    foreach (ChatEntity model in entities)
                                    {
                                        if (model.UserName != entity.UserName)
                                            newEntities.Add(model);
                                    }

                                    ChatEntity ce = new ChatEntity();
                                    ce.UserName = entity.UserName;
                                    ce.Token = entity.Token;
                                    ce.MessageType = MessageType.Logout;
                                    entities = newEntities;

                                    message = JsonConvert.SerializeObject(ce);
                                }
                                if(entity.MessageType == MessageType.Message || entity.MessageType == MessageType.Typing)
                                {
                                    bool tokenVerified = false;
                                    foreach (ChatEntity model in entities)
                                    {
                                        if (model.UserName == entity.UserName && model.Token == entity.Token)
                                            tokenVerified = true;
                                    }

                                    if(!tokenVerified)
                                    {
                                        ChatEntity ce = new ChatEntity();
                                        ce.UserName = entity.UserName;
                                        ce.Token = entity.Token;
                                        ce.MessageType = MessageType.Logout;

                                        message = JsonConvert.SerializeObject(ce);
                                    }
                                }
                            }
                            catch { }
                            Console.WriteLine(message);
                            allSockets.ToList().ForEach(s => s.Send(message));
                        };
                });


            var input = Console.ReadLine();
            while (input != "exit")
            {
                foreach (var socket in allSockets.ToList())
                {
                    socket.Send(input);
                }
                input = Console.ReadLine();
            }

        }
    }
}
