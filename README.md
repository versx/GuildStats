[![Build](https://github.com/versx/GuildStats/workflows/.NET%205.0/badge.svg)](https://github.com/versx/GuildStats/actions)
[![GitHub Release](https://img.shields.io/github/release/versx/GuildStats.svg)](https://github.com/versx/GuildStats/releases/)
[![GitHub Contributors](https://img.shields.io/github/contributors/versx/GuildStats.svg)](https://github.com/versx/GuildStats/graphs/contributors/)
[![Discord](https://img.shields.io/discord/552003258000998401.svg?label=&logo=discord&logoColor=ffffff&color=7389D8&labelColor=6A7EC2)](https://discord.gg/zZ9h9Xa)  

# Discord Guild Statistics Bot  

Displays Discord server statistics for members, bots, roles, and channel count. Supports multiple Discord servers.  

## Prerequisites
1. Download .NET 5.0 installer: `curl https://dotnet.microsoft.com/download/dotnet/scripts/v1/dotnet-install.sh > dotnet-install.sh`  
1. Set executable permissions: `chmod +x dotnet-install.sh`  
1. Start .NET 5.0 installer: `./dotnet-install.sh --version 5.0.404`  

## Installation
1. Clone repository: `git clone https://github.com/versx/GuildStats`  
1. Copy example config `cp config.example.json bin/config.json`  
1. Fill out config options.  
1. Build executable in root project folder (same folder `src` is in): `dotnet build`  
1. Run executable via `bin` folder: `dotnet GuildStats.dll`  

## Updating  
1. Pull latest changes in root folder  
1. Build project `dotnet build`  
1. Run `dotnet bin/GuildStats.dll`  

## Preview  
![Image Preview](example.png)  

## Notes  
**Valid Log Levels:**  
* Trace: 0
* Debug: 1
* Info: 2
* Warning: 3
* Error: 4
* Critical: 5
* None: 6
