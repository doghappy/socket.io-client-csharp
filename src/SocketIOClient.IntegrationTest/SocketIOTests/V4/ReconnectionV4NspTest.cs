﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace SocketIOClient.IntegrationTest.SocketIOTests.V4
{
    [TestClass]
    public class ReconnectionV4NspTest : ReconnectionTest
    {
        public ReconnectionV4NspTest()
        {
            SocketIOCreator = new SocketIOV4NspCreator();
        }

        protected override ISocketIOCreateable SocketIOCreator { get; }

        [TestMethod]
        public override async Task ReconnectionTrueTest()
        {
            await base.ReconnectionTrueTest();
        }

        [TestMethod]
        public override async Task ReconnectionFalseTest()
        {
            await base.ReconnectionFalseTest();
        }

        [TestMethod]
        public override async Task ReconnectingTest()
        {
            await base.ReconnectingTest();
        }

        [TestMethod]
        public override async Task ReconnectionAttemptsExceededTest()
        {
            await base.ReconnectionAttemptsExceededTest();
        }

        [TestMethod]
        public override async Task ManuallyReconnectionTest()
        {
            await base.ManuallyReconnectionTest();
        }
    }
}
