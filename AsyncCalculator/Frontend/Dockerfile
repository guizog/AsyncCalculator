# Use a lightweight Node.js base image
FROM node:20.9.0-bullseye-slim

# Set the working directory inside the container
WORKDIR /app

# Copy package.json and package-lock.json to install dependencies
COPY package*.json ./

# Install application dependencies
RUN npm install

# Copy the rest of the application code
COPY . .

# Expose the port your Node.js application listens on
EXPOSE 1337

# Command to run the application when the container starts
CMD ["node", "server.js"]