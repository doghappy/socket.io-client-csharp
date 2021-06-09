'use strict';

const io = require("socket.io-client");

const socket = io("http://localhost:11003", {
    //auth: {
    //    token: "123"
    //},
    transports: ["websocket"],
    query: {
        "token": "V3x"
    }
});

socket.on("hi", data => {
    console.log(data);
})

socket.on("error", data => {
    console.log(data);
})

socket.prependAny((event, ...args) => {
    console.log(`got ${event} - prependAny`);
});

const listener = (event, ...args) => {
    console.log(`got ${event} - onAny`);
};

const listener2 = (event, ...args) => {
    console.log(`got ${event} - onAny 2`);
};

socket.onAny(listener);
socket.onAny(listener);
socket.onAny(listener2);

socket.on("connect", () => {
    socket.emit("hi", "a");

    setTimeout(() => {
        socket.offAny(listener);
        socket.emit("hi", "b");
    }, 2000)
    //socket.offAny(listener);
    //socket.emit("hi", "b");
});
