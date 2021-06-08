using System;
using SocketIOClient.Test.Attributes;
using System.Reflection;

namespace SocketIOClient.Test.SocketIOTests
{
    public abstract class SocketIOTest
    {
        protected abstract string Url { get; }

        protected abstract string Prefix { get; }

        protected string Version
        {
            get
            {
                var type = GetType();
                var attr = type.GetCustomAttribute<SocketIOVersionAttribute>();
                if (attr is null)
                {
                    throw new MissingMemberException($"The {type.Name} class is missing the '{nameof(SocketIOVersionAttribute)}'");
                }
                return attr.Version.ToString();
            }
        }

        protected string GetConstant(string name)
        {
            var serverInfoType = typeof(ServerInfo);
            string fieldName = $"{Version}_{name}";
            var field = serverInfoType.GetField(fieldName);
            if (field is null)
            {
                throw new MissingFieldException(nameof(ServerInfo), fieldName);
            }
            return field.GetValue(null).ToString();
        }
    }
}
