using System.Collections.Generic;

namespace SocketIOClient
{
    public class EventHandlerBox
    {
        public EventHandler EventHandler { get; set; }
        public IList<EventHandler> EventHandlers { get; set; }
    }
}
