'use strict';

//import io from 'socket.io-client';
const io = require("socket.io-client");

const socket = io('http://localhost:3000');
socket.connect();

//socket.emit("callback", "client", data => {
//    console.log(data);
//});
//socket.emit("callback", "client");

socket.on("emit\\args\"", (d, e) => console.log(d, e));
socket.emit("emit\\args\"");