import * as io from "socket.io-client";

const host = "localhost";

const socket = io(`http://${host}:11000`, {
    path: "/path",
    transports: ["websocket"]
});

socket.on("connect", () => {
    console.log(`socket.connected: ${socket.connected}`);

    socket.on("disconnect", reason => {
        console.log(`disconnect: ${reason}`);
    });


    socket.on("hi", message => {
        console.log(`server: ${message}`);
    });

    socket.emit("hi", "doghappy");
});

const nspSocket = io(`http://${host}:11000/nsp`, {
    path: "/path",
    transports: ["websocket"]
});

nspSocket.on("connect", () => {
    console.log(`nspSocket.connected: ${nspSocket.connected}`);

    nspSocket.on("disconnect", reason => {
        console.log(`disconnect: ${reason}`);
    });

    nspSocket.on("hi", message => {
        console.log(`server: ${message}`);
    });

    nspSocket.emit("hi", "doghappy");
    nspSocket.emit("ack", "doghappy ack", (data) => {
        console.log(data);
    });

    //nspSocket.emit("sever disconnect", false);

    nspSocket.emit("binary ack", "name", {
        bytes: Buffer.from("socket.io-client", "utf-8"),
        source: "client002 - source"
    }, response => {
        console.log(`clientSource: ${response.clientSource}`);
        console.log(`source: ${response.source}`);
        console.log(`bytes: ${response.bytes.toString()}`);
    });
})