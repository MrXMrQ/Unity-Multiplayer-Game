FROM ubuntu:22.04

RUN apt-get update && apt-get install -y \
    libglu1 \
    libxcursor1 \
    libxss1 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

COPY . .

RUN chmod +x ./ServerBuild.x86_64

EXPOSE 7777/udp

ENTRYPOINT ["./ServerBuild.x86_64", "-batchmode", "-nographics", "-logFile", "/dev/stdout"]