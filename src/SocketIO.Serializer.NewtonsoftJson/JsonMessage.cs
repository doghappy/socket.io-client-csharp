using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using SocketIO.Core;

namespace SocketIO.Serializer.NewtonsoftJson
{
    public class JsonMessage : IMessage
    {
        public JsonMessage(MessageType type)
        {
            Type = type;
            Id = -1;
        }

        public MessageType Type { get; }
        public string Sid { get; set; }
        public int PingInterval { get; set; }
        public int PingTimeout { get; set; }
        public List<string> Upgrades { get; set; }
        public int BinaryCount { get; set; }
        public string Namespace { get; set; }
        public TimeSpan Duration { get; set; }
        public int Id { get; set; }

        public string Error { get; set; }
        public string ReceivedText { get; set; }
        public List<byte[]> ReceivedBinary { get; set; }

        private bool _parsed;

        private JArray _jsonArray;

        public JArray JsonArray
        {
            get
            {
                Parse();
                return _jsonArray;
            }
        }

        private string _event;

        public string Event
        {
            get
            {
                Parse();
                return _event;
            }
            set => _event = value;
        }

        private void Parse()
        {
            if (_parsed) return;
            var jsonArray = JArray.Parse(ReceivedText);
            SetEvent(jsonArray);
            _jsonArray = jsonArray;
            _parsed = true;
        }

        private void SetEvent(JArray jsonArray)
        {
            if (Type != MessageType.Event && Type != MessageType.Binary)
                return;

            if (jsonArray.Count < 1)
            {
                throw new ArgumentException("Cannot get event name from an empty json array");
            }

            if (jsonArray[0] is null)
            {
                throw new ArgumentException("Event name is null");
            }

            Event = jsonArray[0].Value<string>();
            jsonArray.RemoveAt(0);
        }
    }
}