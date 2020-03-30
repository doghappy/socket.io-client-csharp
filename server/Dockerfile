FROM node
WORKDIR /usr/src/app
COPY package*.json ./
RUN npm i
COPY . .
EXPOSE 3000
EXPOSE 3001
CMD ["npm", "start"]
