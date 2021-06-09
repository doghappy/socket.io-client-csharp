using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace SocketIOClient.Test.SocketIOTests
{
    public abstract class OnErrorTest
    {
        protected abstract ISocketIOCreateable SocketIOCreator { get; }

        public abstract Task Test();
    }
}
