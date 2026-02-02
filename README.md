# SteamPanno

a visual representation of someone's Steam library

<img src="/doc/panno.png" alt="SteamPanno"/>

[![LicenseBadge](https://img.shields.io/github/license/kirnosenko/SteamPanno.svg)](https://raw.githubusercontent.com/kirnosenko/SteamPanno/master/LICENSE)
[![Release](https://img.shields.io/github/v/release/kirnosenko/SteamPanno)](https://github.com/kirnosenko/SteamPanno/releases/latest)

## Features

 * Create Personalized Pannos: Generate a panno for yourself, your friends, or any Steam user with a public profile.
 * Customizable Resolution: Set any resolution to fit your needs, whether it's for a wallpaper, avatar, or social media post.
 * Multiple Generation Algorithms: Choose from a variety of layout styles to find the one you like best.
 * Track Your Playtime: Monitor how your gaming habits evolve over different periods of time.

## Getting Steam API Key

Since the initial release unauthorized Steam API access has been blocked.
So now you have to get your Steam API Key [here](https://steamcommunity.com/dev/apikey).

## Downloads

You may download the latest release for your system:

### Windows

- [steampanno-1.1.0-windows-x64](https://github.com/kirnosenko/SteamPanno/releases/download/1.1.0/steampanno-1.1.0-windows-x64.zip) (x64 binaries)

### Linux

- [steampanno-1.1.0-linux-x64](https://github.com/kirnosenko/SteamPanno/releases/download/1.1.0/steampanno-1.1.0-linux-x64.zip) (x64 binaries)

### macOS

- [steampanno-1.1.0-macos-uni](https://github.com/kirnosenko/SteamPanno/releases/download/1.1.0/steampanno-1.1.0-macos-uni.zip) (Apple Silicon and Apple Intel app package)

## How To Use It

If you are running the standalone version and do not have it in your Steam library, the Steam integration features will not work.
In this case, you will either need to manually enter the Steam ID or use the copy-paste function.

![Copy](/doc/copy.png)
![Paste](/doc/paste.png)

Also you can change application Steam ID to make Steam think you have it in your library. Steam ID is stored in steamid.json which path is:

 * Windows: %USERPROFILE%/AppData/Local/steampanno
 * Linux: $XDG_DATA_HOME/steampanno
 * macOS: ~/Library/Application Support/steampanno 

## Links

 * [RIP SteamPanno on Steam](https://store.steampowered.com/app/4026140/SteamPanno/)
