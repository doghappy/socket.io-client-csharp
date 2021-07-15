﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIOClient.IntegrationTest.Models;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SocketIOClient.IntegrationTest.SocketIOTests
{
    public abstract class EmitTest
    {
        protected abstract ISocketIOCreateable SocketIOCreator { get;  }

        public virtual async Task Hi()
        {
            string result = null;
            var client = SocketIOCreator.Create();
            client.On("hi", response =>
            {
                result = response.GetValue<string>();
            });
            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("hi", ".net core");
            };
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.AreEqual($"{SocketIOCreator.Prefix}.net core", result);
        }

        public virtual async Task EmitWithoutParams()
        {
            bool result = false;
            var client = SocketIOCreator.Create();
            client.On("no params", response =>
            {
                result = true;
            });
            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("no params");
            };
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.IsTrue(result);
        }

        #region Emit with 1 params
        public virtual async Task EmitWith1ParamsNull()
        {
            JsonValueKind result = JsonValueKind.Undefined;
            var client = SocketIOCreator.Create();
            client.On("1 params", response =>
            {
                var element = response.GetValue();
                result = element.ValueKind;
            });
            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("1 params", data: null);
            };
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.AreEqual(JsonValueKind.Null, result);
        }

        public virtual async Task EmitWith1ParamsTrue()
        {
            JsonValueKind result = JsonValueKind.Undefined;
            var client = SocketIOCreator.Create();
            client.On("1 params", response =>
            {
                var element = response.GetValue();
                result = element.ValueKind;
            });
            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("1 params", true);
            };
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.AreEqual(JsonValueKind.True, result);
        }

        public virtual async Task EmitWith1ParamsFalse()
        {
            JsonValueKind result = JsonValueKind.Undefined;
            var client = SocketIOCreator.Create();
            client.On("1 params", response =>
            {
                var element = response.GetValue();
                result = element.ValueKind;
            });
            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("1 params", false);
            };
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.AreEqual(JsonValueKind.False, result);
        }

        public virtual async Task EmitWith1ParamsNumber0()
        {
            int result = -1;
            var client = SocketIOCreator.Create();
            client.On("1 params", response =>
            {
                result = response.GetValue<int>();
            });
            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("1 params", 0);
            };
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.AreEqual(0, result);
        }

        public virtual async Task EmitWith1ParamsNumberMin()
        {
            int result = -1;
            var client = SocketIOCreator.Create();
            client.On("1 params", response =>
            {
                result = response.GetValue<int>();
            });
            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("1 params", int.MinValue);
            };
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.AreEqual(int.MinValue, result);
        }

        public virtual async Task EmitWith1ParamsNumberMax()
        {
            int result = -1;
            var client = SocketIOCreator.Create();
            client.On("1 params", response =>
            {
                result = response.GetValue<int>();
            });
            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("1 params", int.MaxValue);
            };
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.AreEqual(int.MaxValue, result);
        }

        public virtual async Task EmitWith1ParamsEmptyString()
        {
            string result = null;
            var client = SocketIOCreator.Create();
            client.On("1 params", response =>
            {
                result = response.GetValue<string>();
            });
            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("1 params", "");
            };
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.AreEqual(string.Empty, result);
        }

        public virtual async Task EmitWith1ParamsShortString()
        {
            string result = null;
            var client = SocketIOCreator.Create();
            client.On("1 params", response =>
            {
                result = response.GetValue<string>();
            });
            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("1 params", "American, 中国, の");
            };
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.AreEqual("American, 中国, の", result);
        }

        public virtual async Task EmitWith1ParamsLongString()
        {
            string result = null;
            string longString = @"
000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111
222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222
333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333
444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444
555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555
666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666
777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777
888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888
999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999
AmericanAmericanAmericanAmericanAmericanAmericanAmericanAmericanAmericanAmericanAmericanAmericanAme
你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你
ののののののののののののののののののののののののののののののののののののののののののののののののののののののののののののの
";
            var client = SocketIOCreator.Create();
            client.On("1 params", response =>
            {
                result = response.GetValue<string>();
            });
            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("1 params", longString);
            };
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.AreEqual(longString, result);
        }

        public virtual async Task EmitWith1ParamsEmptyObject()
        {
            string result = null;
            var client = SocketIOCreator.Create();
            client.On("1 params", response =>
            {
                var element = response.GetValue();
                result = element.GetRawText();
            });
            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("1 params", new { });
            };
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.AreEqual("{}", result);
        }

        public virtual async Task EmitWith1ParamsObject()
        {
            ObjectResponse result = null;
            var client = SocketIOCreator.Create();
            client.JsonSerializer = new MyJsonSerializer(client.Options.EIO);
            client.On("1 params", response =>
            {
                result = response.GetValue<ObjectResponse>();
            });
            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("1 params", new ObjectResponse
                {
                    A = 97,
                    B = "b",
                    C = new ObjectC
                    {
                        D = "d",
                        E = 2.71828182846
                    }
                });
            };
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.AreEqual(97, result.A);
            Assert.AreEqual("b", result.B);
            Assert.AreEqual("d", result.C.D);
            Assert.AreEqual(2.71828182846, result.C.E);
        }

        public virtual async Task EmitWith1ParamsBytes()
        {
            SocketIOResponse result = null;
            string longString = @"
000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111
222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222
333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333
444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444
555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555
666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666
777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777
888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888
999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999
AmericanAmericanAmericanAmericanAmericanAmericanAmericanAmericanAmericanAmericanAmericanAmericanAme
你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你
ののののののののののののののののののののののののののののののののののののののののののののののののののののののののののののの
";
            byte[] bytes = Encoding.UTF8.GetBytes(longString);
            var client = SocketIOCreator.Create();
            client.On("1 params", response =>
            {
                result = response;
            });
            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("1 params", bytes);
            };
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.AreEqual(1, result.InComingBytes.Count);
            Assert.AreEqual(longString, Encoding.UTF8.GetString(result.InComingBytes[0]));
            Assert.AreEqual(longString, Encoding.UTF8.GetString(result.GetValue<byte[]>()));
        }

        public virtual async Task EmitWith1ParamsBytesInObject()
        {
            SocketIOResponse result = null;
            string longString = @"
000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111
222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222
333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333
444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444
555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555
666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666
777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777
888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888
999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999
AmericanAmericanAmericanAmericanAmericanAmericanAmericanAmericanAmericanAmericanAmericanAmericanAme
你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你
ののののののののののののののののののののののののののののののののののののののののののののののののののののののののののののの
";
            byte[] bytes = Encoding.UTF8.GetBytes(longString);
            var client = SocketIOCreator.Create();
            client.On("1 params", response =>
            {
                result = response;
            });
            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("1 params", new BytesInObjectResponse
                {
                    Code = 6,
                    Message = bytes
                });
            };
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.AreEqual(1, result.InComingBytes.Count);
            Assert.AreEqual(longString, Encoding.UTF8.GetString(result.InComingBytes[0]));

            var model = result.GetValue<BytesInObjectResponse>();
            Assert.AreEqual(6, model.Code);
            Assert.AreEqual(longString, Encoding.UTF8.GetString(model.Message));
        }
        #endregion

        #region Emit with 2 params
        public virtual async Task EmitWith2ParamsNull()
        {
            JsonValueKind result0 = JsonValueKind.Undefined;
            JsonValueKind result1 = JsonValueKind.Undefined;
            var client = SocketIOCreator.Create();
            client.On("2 params", response =>
            {
                var element0 = response.GetValue();
                result0 = element0.ValueKind;
                var element1 = response.GetValue(1);
                result1 = element1.ValueKind;
            });
            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("2 params", null, null);
            };
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.AreEqual(JsonValueKind.Null, result0);
            Assert.AreEqual(JsonValueKind.Null, result1);
        }

        public virtual async Task EmitWith2ParamsTrueTrue()
        {
            JsonValueKind result0 = JsonValueKind.Undefined;
            JsonValueKind result1 = JsonValueKind.Undefined;
            var client = SocketIOCreator.Create();
            client.On("2 params", response =>
            {
                result0 = response.GetValue().ValueKind;
                result1 = response.GetValue(1).ValueKind;
            });
            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("2 params", true, true);
            };
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.AreEqual(JsonValueKind.True, result0);
            Assert.AreEqual(JsonValueKind.True, result1);
        }

        public virtual async Task EmitWith2ParamsTrueFalse()
        {
            JsonValueKind result0 = JsonValueKind.Undefined;
            JsonValueKind result1 = JsonValueKind.Undefined;
            var client = SocketIOCreator.Create();
            client.On("2 params", response =>
            {
                result0 = response.GetValue().ValueKind;
                result1 = response.GetValue(1).ValueKind;
            });
            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("2 params", true, false);
            };
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.AreEqual(JsonValueKind.True, result0);
            Assert.AreEqual(JsonValueKind.False, result1);
        }

        public virtual async Task EmitWith2ParamsFalseTrue()
        {
            JsonValueKind result0 = JsonValueKind.Undefined;
            JsonValueKind result1 = JsonValueKind.Undefined;
            var client = SocketIOCreator.Create();
            client.On("2 params", response =>
            {
                result0 = response.GetValue().ValueKind;
                result1 = response.GetValue(1).ValueKind;
            });
            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("2 params", false, true);
            };
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.AreEqual(JsonValueKind.False, result0);
            Assert.AreEqual(JsonValueKind.True, result1);
        }

        public virtual async Task EmitWith2ParamsTrueNull()
        {
            JsonValueKind result0 = JsonValueKind.Undefined;
            JsonValueKind result1 = JsonValueKind.Undefined;
            var client = SocketIOCreator.Create();
            client.On("2 params", response =>
            {
                result0 = response.GetValue().ValueKind;
                result1 = response.GetValue(1).ValueKind;
            });
            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("2 params", true, null);
            };
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.AreEqual(JsonValueKind.True, result0);
            Assert.AreEqual(JsonValueKind.Null, result1);
        }

        public virtual async Task EmitWith2ParamsStringObject()
        {
            string result0 = null;
            ObjectResponse result1 = null;
            string longString = @"
000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111
222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222
333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333
444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444
555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555
666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666
777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777
888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888
999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999
AmericanAmericanAmericanAmericanAmericanAmericanAmericanAmericanAmericanAmericanAmericanAmericanAme
你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你
ののののののののののののののののののののののののののののののののののののののののののののののののののののののののののののの
";
            var client = SocketIOCreator.Create();
            client.On("2 params", response =>
            {
                result0 = response.GetValue<string>();
                result1 = response.GetValue<ObjectResponse>(1);
            });
            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("2 params", longString, new ObjectResponse
                {
                    A = 97,
                    B = "b",
                    C = new ObjectC
                    {
                        D = "d",
                        E = 2.71828182846
                    }
                });
            };
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.AreEqual(longString, result0);
            Assert.AreEqual(97, result1.A);
            Assert.AreEqual("b", result1.B);
            Assert.AreEqual("d", result1.C.D);
            Assert.AreEqual(2.71828182846, result1.C.E);
        }

        public virtual async Task EmitWith2ParamsBytes()
        {
            SocketIOResponse result = null;
            string longString = @"
000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111
222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222
333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333
444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444
555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555
666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666
777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777
888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888
999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999
AmericanAmericanAmericanAmericanAmericanAmericanAmericanAmericanAmericanAmericanAmericanAmericanAme
你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你
ののののののののののののののののののののののののののののののののののののののののののののののののののののののののののののの
";
            var client = SocketIOCreator.Create();
            client.On("2 params", response =>
            {
                result = response;
            });
            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("2 params", Encoding.UTF8.GetBytes(longString + "abc"), new BytesInObjectResponse
                {
                    Code = 64,
                    Message = Encoding.UTF8.GetBytes(longString + "xyz")
                });
            };
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.AreEqual(2, result.InComingBytes.Count);
            Assert.AreEqual(longString + "abc", Encoding.UTF8.GetString(result.InComingBytes[0]));
            Assert.AreEqual(longString + "xyz", Encoding.UTF8.GetString(result.InComingBytes[1]));

            byte[] bytes = result.GetValue<byte[]>();
            Assert.AreEqual(longString + "abc", Encoding.UTF8.GetString(bytes));

            var model = result.GetValue<BytesInObjectResponse>(1);
            Assert.AreEqual(64, model.Code);
            Assert.AreEqual(longString + "xyz", Encoding.UTF8.GetString(model.Message));
        }
        #endregion

        #region Server calls the client's callback
        public virtual async Task NoParams_NoParams()
        {
            bool result = false;
            var client = SocketIOCreator.Create();
            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("no params | cb: no params", response =>
                {
                    result = true;
                });
            };
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.IsTrue(result);
        }

        public virtual async Task OneParams_OneParams_String()
        {
            string result = null;
            var client = SocketIOCreator.Create();
            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("1 params | cb: 1 params", response =>
                {
                    result = response.GetValue<string>();
                }, "str1");
            };
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.AreEqual("str1", result);
        }

        public virtual async Task TwoParams_TwoParams_StringObject()
        {
            SocketIOResponse result = null;
            var client = SocketIOCreator.Create();
            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("2 params | cb: 2 params", response =>
                {
                    result = response;
                },
                "str1",
                new ObjectResponse
                {
                    A = 97,
                    B = "b",
                    C = new ObjectC
                    {
                        D = "d",
                        E = 2.71828182846
                    }
                });
            };
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.AreEqual("str1", result.GetValue<string>());

            var model = result.GetValue<ObjectResponse>(1);
            Assert.AreEqual(97, model.A);
            Assert.AreEqual("b", model.B);
            Assert.AreEqual("d", model.C.D);
            Assert.AreEqual(2.71828182846, model.C.E);
        }

        public virtual async Task TwoParams_TwoParams_2Binary()
        {
            SocketIOResponse result = null;
            string longString = @"
000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111
222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222
333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333
444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444
555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555
666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666
777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777
888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888
999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999
AmericanAmericanAmericanAmericanAmericanAmericanAmericanAmericanAmericanAmericanAmericanAmericanAme
你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你
ののののののののののののののののののののののののののののののののののののののののののののののののののののののののののののの
";
            var client = SocketIOCreator.Create();
            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("2 params | cb: 2 params", response =>
                {
                    result = response;
                },
                Encoding.UTF8.GetBytes(longString + "abc"),
                new BytesInObjectResponse
                {
                    Code = 64,
                    Message = Encoding.UTF8.GetBytes(longString + "xyz")
                });
            };
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.AreEqual(2, result.InComingBytes.Count);
            Assert.AreEqual(longString + "abc", Encoding.UTF8.GetString(result.InComingBytes[0]));
            Assert.AreEqual(longString + "xyz", Encoding.UTF8.GetString(result.InComingBytes[1]));

            byte[] bytes = result.GetValue<byte[]>();
            Assert.AreEqual(longString + "abc", Encoding.UTF8.GetString(bytes));

            var model = result.GetValue<BytesInObjectResponse>(1);
            Assert.AreEqual(64, model.Code);
            Assert.AreEqual(longString + "xyz", Encoding.UTF8.GetString(model.Message));
        }
        #endregion

        #region Client calls the server's callback
        public virtual async Task ClientCallsServerCallback_NoParams_0()
        {
            bool flag0 = false;
            bool flag1 = false;
            bool flag2 = false;
            var client = SocketIOCreator.Create();
            client.On("no params", response => flag0 = true);
            client.On("client calls the server's callback 0", response => flag1 = true);
            client.OnConnected += async (sender, e) =>
            {
                flag2 = true;
                await client.EmitAsync("client calls the server's callback 0");
            };
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.IsTrue(flag2);
            Assert.IsTrue(flag1);
            Assert.IsFalse(flag0);
        }

        public virtual async Task ClientCallsServerCallback_NoParams_1()
        {
            bool flag0 = false;
            bool flag1 = false;
            bool flag2 = false;
            var client = SocketIOCreator.Create();
            client.On("no params", response => flag0 = true);
            client.On("client calls the server's callback 0", async response =>
            {
                flag1 = true;
                await response.CallbackAsync();
            });
            client.OnConnected += async (sender, e) =>
            {
                flag2 = true;
                await client.EmitAsync("client calls the server's callback 0");
            };
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.IsTrue(flag2);
            Assert.IsTrue(flag1);
            Assert.IsTrue(flag0);
        }

        public virtual async Task ClientCallsServerCallback_1Params_0()
        {
            SocketIOResponse result = null;
            var client = SocketIOCreator.Create();
            client.On("1 params", response =>
            {
                result = response;
            });
            client.On("client calls the server's callback 1", async response =>
            {
                byte[] bytes = response.GetValue<byte[]>();
                string text = Encoding.UTF8.GetString(bytes) + "...";
                await response.CallbackAsync(Encoding.UTF8.GetBytes(text));
            });
            client.OnConnected += async (sender, e) =>
            {
                byte[] bytes = Encoding.UTF8.GetBytes("ClientCallsServerCallback_1Params_0");
                await client.EmitAsync("client calls the server's callback 1", bytes);
            };
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.AreEqual(1, result.InComingBytes.Count);
            Assert.AreEqual("ClientCallsServerCallback_1Params_0...", Encoding.UTF8.GetString(result.GetValue<byte[]>()));
        }
        #endregion
    }
}
