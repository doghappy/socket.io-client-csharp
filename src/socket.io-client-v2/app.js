'use strict';

const io = require("socket.io-client");

const socket = io("http://localhost:11002", {
    //auth: {
    //    token: "123"
    //},
    transports: ["websocket"],
    query: {
        "token": "V2"
    }
});

socket.on("ContinuouslyReceiveData", data => {
    console.log(data.index);
})

socket.on("2 params", (p1, p2) => {
    console.log(p1.toString());
    console.log(p2.msg.toString());
})

socket.on("connect", () => {
    console.log("connected")
    //socket.emit("ContinuouslyReceiveData", {
    //    count: 100,
    //    path: "设计表.rar"
    //})

    socket.emit("2 params", Buffer.from("abc", 'utf-8'), {
        msg: Buffer.from("xyz", 'utf-8')
    })
});
