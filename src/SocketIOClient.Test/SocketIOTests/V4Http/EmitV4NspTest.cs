using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace SocketIOClient.Test.SocketIOTests.V4Http
{
    [TestClass]
    public class EmitV4NspTest : EmitTest
    {
        public EmitV4NspTest()
        {
            SocketIOCreator = new SocketIOV4NspCreator();
        }

        protected override ISocketIOCreateable SocketIOCreator { get; }

        [TestMethod]
        public override async Task Hi()
        {
            await base.Hi();
        }

        [TestMethod]
        public override async Task EmitWithoutParams()
        {
            await base.EmitWithoutParams();
        }

        #region Emit with 1 params
        [TestMethod]
        public override async Task EmitWith1ParamsNull()
        {
            await base.EmitWith1ParamsNull();
        }

        [TestMethod]
        public override async Task EmitWith1ParamsTrue()
        {
            await base.EmitWith1ParamsTrue();
        }

        [TestMethod]
        public override async Task EmitWith1ParamsFalse()
        {
            await base.EmitWith1ParamsFalse();
        }

        [TestMethod]
        public override async Task EmitWith1ParamsNumber0()
        {
            await base.EmitWith1ParamsNumber0();
        }

        [TestMethod]
        public override async Task EmitWith1ParamsNumberMin()
        {
            await base.EmitWith1ParamsNumberMin();
        }

        [TestMethod]
        public override async Task EmitWith1ParamsNumberMax()
        {
            await base.EmitWith1ParamsNumberMax();
        }

        [TestMethod]
        public override async Task EmitWith1ParamsEmptyString()
        {
            await base.EmitWith1ParamsEmptyString();
        }

        [TestMethod]
        public override async Task EmitWith1ParamsShortString()
        {
            await base.EmitWith1ParamsShortString();
        }

        [TestMethod]
        public override async Task EmitWith1ParamsLongString()
        {
            await base.EmitWith1ParamsLongString();
        }

        [TestMethod]
        public override async Task EmitWith1ParamsEmptyObject()
        {
            await base.EmitWith1ParamsEmptyObject();
        }

        [TestMethod]
        public override async Task EmitWith1ParamsObject()
        {
            await base.EmitWith1ParamsObject();
        }

        [TestMethod]
        public override async Task EmitWith1ParamsBytes()
        {
            await base.EmitWith1ParamsBytes();
        }

        [TestMethod]
        public override async Task EmitWith1ParamsBytesInObject()
        {
            await base.EmitWith1ParamsBytesInObject();
        }

        [TestMethod]
        public override async Task EmitWith2ParamsBytes()
        {
            await base.EmitWith2ParamsBytes();
        }

        [TestMethod]
        public override async Task EmitWith1ParamsArray()
        {
            await base.EmitWith1ParamsArray();
        }
        #endregion

        #region Emit with 2 params
        [TestMethod]
        public override async Task EmitWith2ParamsNull()
        {
            await base.EmitWith2ParamsNull();
        }

        [TestMethod]
        public override async Task EmitWith2ParamsTrueTrue()
        {
            await base.EmitWith2ParamsTrueTrue();
        }

        [TestMethod]
        public override async Task EmitWith2ParamsTrueFalse()
        {
            await base.EmitWith2ParamsTrueFalse();
        }

        [TestMethod]
        public override async Task EmitWith2ParamsFalseTrue()
        {
            await base.EmitWith2ParamsFalseTrue();
        }

        [TestMethod]
        public override async Task EmitWith2ParamsTrueNull()
        {
            await base.EmitWith2ParamsTrueNull();
        }

        [TestMethod]
        public override async Task EmitWith2ParamsStringObject()
        {
            await base.EmitWith2ParamsStringObject();
        }

        [TestMethod]
        public override async Task EmitWith2ParamsArrayAndString()
        {
            await base.EmitWith2ParamsArrayAndString();
        }
        #endregion

        #region Server calls the client's callback
        [TestMethod]
        public override async Task NoParams_NoParams()
        {
            await base.NoParams_NoParams();
        }

        [TestMethod]
        public override async Task OneParams_OneParams_String()
        {
            await base.OneParams_OneParams_String();
        }

        [TestMethod]
        public override async Task TwoParams_TwoParams_StringObject()
        {
            await base.TwoParams_TwoParams_StringObject();
        }

        [TestMethod]
        public override async Task TwoParams_TwoParams_2Binary()
        {
            await base.TwoParams_TwoParams_2Binary();
        }

        [TestMethod]
        public override async Task ClientCallsServerCallback_NoParams_0()
        {
            await base.ClientCallsServerCallback_NoParams_0();
        }

        [TestMethod]
        public override async Task ClientCallsServerCallback_NoParams_1()
        {
            await base.ClientCallsServerCallback_NoParams_1();
        }

        [TestMethod]
        public override async Task ClientCallsServerCallback_1Params_0()
        {
            await base.ClientCallsServerCallback_1Params_0();
        }
        #endregion
    }
}
