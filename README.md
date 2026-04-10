# Unity-Multiplayer-Game

Welcome to the Unity Multiplayer Game repository. This project features a multiplayer experience with dedicated server support and containerized deployment.

## How to Play (Windows)
To join a match and start playing, follow these simple steps:

- Navigate to the Releases section of this repository.

- Download the latest version of the game.
 
- Extract the ZIP file and navigate to the Windows folder.
 
- Run the Unity-Multiplayer-Game.exe.
 
- Enter the server IP (if prompted) and enjoy the game!

## Dedicated Server Setup (Linux & Docker)

If you want to host your own dedicated server, we provide a pre-configured Docker setup for easy deployment on Linux environments.
Prerequisites

- A Linux-based server (Ubuntu/Debian recommended).

- Docker and Docker Compose installed.

### Installation & Deployment

- Transfer Files: Copy all files from the LINUX folder of the release to your server.

- Organization: Ensure that the game server binaries, the Dockerfile, and the docker-compose.yml are in the same directory.

- Start the Server: Open a terminal in that directory and run the following command:

```Bash

docker-compose up --build -d
```

### Command Breakdown:

- up: Starts the containers.

- --build: Rebuilds the image (ensures your latest server binaries are used).

- -d: Detached mode (runs the server in the background).
    Deployment: Dockerized Headless Linux Server

# About this Project

This project was born out of pure passion for game development and a deep interest in multiplayer architecture. It’s a personal "passion project" created because I simply felt like diving into the complexities of networking, real-time synchronization, and building fun gameplay loops from scratch.   
