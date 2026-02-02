const socket = require('socket.io');
const http = require('http');

const server = http.createServer();
const port = process.env.PORT
const io = socket(server, {
    pingInterval: 2000,
    pingTimeout: 1000,
    transports: [process.env.TRANSPORT],
    cors: {
        origin: "*",
        methods: ["GET", "POST"]
    }
});
server.listen(port, () => {
    console.log(`Server is listening on port ${port}`);
});