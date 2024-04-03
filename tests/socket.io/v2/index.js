const https = require('https');
const http = require('http');
const socket = require('socket.io');
const fs = require('fs');
const path = require('path');
const template = require('./template');

const wsServers = [
    {
        name: 'v2-ws',
        port: 11200,
        server: () => http.createServer(),
        options: {
            pingInterval: 5000,
            pingTimeout: 10000,
        },
        onCreated: template.registerEvents
    },
    {
        name: 'v2-ws-token',
        port: 11201,
        server: () => http.createServer(),
        options: {},
        onCreated: template.useAuthMiddlewares
    },
    {
        name: 'v2-ws-mp',
        port: 11202,
        server: () => http.createServer(),
        options: {
            parser: require('socket.io-msgpack-parser'),
            pingInterval: 5000,
            pingTimeout: 10000,
        },
        onCreated: template.registerEvents
    },
    {
        name: 'v2-ws-token-mp',
        port: 11203,
        server: () => http.createServer(),
        options: {
            parser: require('socket.io-msgpack-parser')
        },
        onCreated: template.useAuthMiddlewares
    }
];

const httpServers = [
    {
        name: 'v2-http',
        port: 11210,
        server: () => http.createServer(),
        options: {
            transports: ["polling"],
            pingInterval: 5000,
            pingTimeout: 10000,
        },
        onCreated: template.registerEvents
    },
    {
        name: 'v2-http-token',
        port: 11211,
        server: () => http.createServer(),
        options: {
            transports: ["polling"]
        },
        onCreated: template.useAuthMiddlewares
    },
    {
        name: 'v2-http-mp',
        port: 11212,
        server: () => http.createServer(),
        options: {
            parser: require('socket.io-msgpack-parser'),
            transports: ["polling"],
            pingInterval: 5000,
            pingTimeout: 10000,
        },
        onCreated: template.registerEvents
    },
    {
        name: 'v2-http-token-mp',
        port: 11213,
        server: () => http.createServer(),
        options: {
            parser: require('socket.io-msgpack-parser'),
            transports: ["polling"]
        },
        onCreated: template.useAuthMiddlewares
    }
];

let servers = [
    ...wsServers,
    ...httpServers
];

if (process.env.PORT && process.env.NAME){
    const server = servers.find(s => s.name === process.env.NAME);
    server.port = process.env.PORT;
    servers = [server];
}

for (const server of servers) {
    console.log(`Starting server '${server.name}' on port ${server.port}...`);
    template.start(server.name, server.port, server.server(), server.options, server.onCreated);
}