﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace SocketIOClient.IntegrationTest.SocketIOTests.V3
{
    [TestClass]
    public class OffAnyV3Test : OnAnyTest
    {
        public OffAnyV3Test()
        {
            SocketIOCreator = new SocketIOV3Creator();
        }

        protected override ISocketIOCreateable SocketIOCreator { get; }

        [TestMethod]
        public override async Task Test()
        {
            await base.Test();
        }
    }
}
