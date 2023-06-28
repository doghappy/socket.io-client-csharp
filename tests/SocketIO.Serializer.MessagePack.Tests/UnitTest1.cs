// using System.IO.Compression;
// using MessagePack;
// using MessagePack.Formatters;
// using MessagePack.Resolvers;
//
// namespace SocketIO.Serializer.MessagePack.Tests;
//
// public class UnitTest1
// {
//     [Fact]
//     public void Test1()
//     {
//         var test = CompositeResolver.Create(new[]
//         {
//             new ByteArrayConverter()
//         }, new[]
//         {
//             StandardResolver.Instance
//         });
//         var options = MessagePackSerializerOptions.Standard.WithResolver(test);
//         var bytes = MessagePackSerializer.Serialize(new PackMessage2
//         {
//             Type = 2,
//             Data = new List<object>
//             {
//                 "1:emit",
//                 "hello"u8.ToArray()
//             },
//             Id = 0,
//             Nsp = "/"
//         });
//         // MessagePackSerializerOptions.Standard.Compression = CompressionMode.Compress
//         var hex = BitConverter.ToString(bytes);
//         //
//         // var bytes2 = MessagePackSerializer.Serialize(bytes);
//         // var hex2 = BitConverter.ToString(bytes2);
//         // var json2 = MessagePackSerializer.ConvertToJson(bytes2);
//         //
//         // var bytes3 = MessagePackSerializer.SerializeToJson(new { age = 1 }, options);
//         // var hex3 = BitConverter.ToString(bytes3);
//         // var json3 = MessagePackSerializer.ConvertToJson(bytes3);
//
//         // var model = new Model
//         // {
//         //     Id = 1,
//         //     Data = new byte[] { 1 }
//         // };
//         // var test333 = MessagePackSerializer.Serialize(model, options);
//         // var test334 = MessagePackSerializer.SerializeToJson(model, options);
//         // var test335 = MessagePackSerializer.Deserialize<Model>(test333, options);
//         // var test336 = MessagePackSerializer.ConvertFromJson(test334, options);
//
//         // var json = MessagePackSerializer.ConvertToJson(bytes);
//
//         //https://github.com/neuecc/MessagePack-CSharp/issues/206
//
//         // json.Should().Be("test");
//     }
// }
//
// [MessagePackObject]
// public class Model
// {
//     [Key("id")]
//     public int Id { get; set; }
//     
//     [Key("data")]
//     public byte[] Data { get; set; }
// }