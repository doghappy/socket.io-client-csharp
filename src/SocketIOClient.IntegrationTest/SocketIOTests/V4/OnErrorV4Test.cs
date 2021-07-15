﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace SocketIOClient.IntegrationTest.SocketIOTests.V4
{
    [TestClass]
    public class OnErrorV4Test : OnErrorTest
    {
        public OnErrorV4Test()
        {
            SocketIOCreator = new SocketIOV4Creator();
        }

        protected override ISocketIOCreateable SocketIOCreator { get; }

        [TestMethod]
        public override async Task Test()
        {
            await base.Test();
        }
    }
}
