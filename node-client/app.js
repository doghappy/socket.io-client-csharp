'use strict';

//import io from 'socket.io-client';
const io = require("socket.io-client");

const socket = io('http://localhost:3000');

socket.on("App\\Events\\GlassRowChanged", (d1, d2) => {
    console.log(d1);
    console.log(d2);
});

socket.on("connected", () => {

});

socket.connect();

