using System;
using System.Collections;
using System.Collections.Generic;
using MessagePack;
using SocketIO.Core;

namespace SocketIO.Serializer.MessagePack
{
    [MessagePackObject]
    public class PackMessage : IMessage
    {
        public PackMessage()
        {
            Options = new PackMessageOptions
            {
                Compress = true
            };
        }

        public PackMessage(MessageType type) : this()
        {
            Type = type;
            Id = -1;
        }

        [Key("type")]
        public int RawType { get; set; }

        [Key("data")]
        public object Data { get; set; }

        [Key("options")]
        public PackMessageOptions Options { get; }

        [Key("id")]
        public int Id { get; set; }

        [Key("nsp")]
        public string Namespace { get; set; }

        [IgnoreMember]
        public MessageType Type { get; }

        [IgnoreMember]
        public string Sid { get; set; }

        [IgnoreMember]
        public int PingInterval { get; set; }

        [IgnoreMember]
        public int PingTimeout { get; set; }

        [IgnoreMember]
        public List<string> Upgrades { get; set; }

        [IgnoreMember]
        public int BinaryCount { get; set; }

        [IgnoreMember]
        public TimeSpan Duration { get; set; }

        [IgnoreMember]
        public string Error { get; set; }

        [IgnoreMember]
        public string ReceivedText { get; set; }

        [IgnoreMember]
        public List<byte[]> ReceivedBinary { get; set; }

        private bool _parsed;
        private List<object> _dataList;

        [IgnoreMember]
        public List<object> DataList
        {
            get
            {
                Parse();
                return _dataList;
            }
        }

        private string _event;

        [IgnoreMember]
        public string Event
        {
            get
            {
                Parse();
                return _event;
            }
            // set => _event = value;
        }

        private void Parse()
        {
            if (_parsed) return;
            _dataList = new List<object>();
            if (Data is IEnumerable)
            {
                _dataList.AddRange((IEnumerable<object>)Data);
            }
            else if (Data is not null)
            {
                _dataList.Add(Data);
            }

            if (Type is MessageType.Event or MessageType.Binary)
            {
                _event = _dataList[0].ToString();
                _dataList.RemoveAt(0);
            }

            _parsed = true;
        }
    }
}