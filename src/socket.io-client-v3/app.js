'use strict';

const io = require("socket.io-client");

const socket = io("http://localhost:11003", {
    //auth: {
    //    token: "123"
    //},
    transports: ["websocket"],
    query: {
        "token": "v3"
    }
});

socket.on("hi", data => {
    console.log(data);
})

socket.on("connect", () => {
    socket.emit("hi", "socket.io-client-v3");
});
