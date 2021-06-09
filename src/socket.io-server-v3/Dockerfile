FROM node
WORKDIR /usr/src/app
COPY package*.json ./
RUN npm i
COPY . .
EXPOSE 11003
CMD ["npm", "start"]