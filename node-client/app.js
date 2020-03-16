'use strict';

//import io from 'socket.io-client';
const io = require("socket.io-client");

const socket = io('http://localhost:3000');
socket.connect();

socket.on("message send", (d1, d2, d3, d4) => {
    console.log(d1.toString());
    console.log(d2.toString());
    console.log(d3.toString());
    console.log(d4.toString());
});

socket.emit("message send", "node client");