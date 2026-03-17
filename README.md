# Baguettefy — Dofus Translation Bot

A Discord bot for Dofus players that provides accurate English ↔ French translations using the official Ankama-sourced data from [dofusdb.fr](https://dofusdb.fr). Google Translate gets you most of the way, but quest names, achievement titles, item names, and dungeon names often don't match Ankama's exact wording — Baguettefy does.

## Features

- **Game content lookup** — search by English or French name and get both translations for quests, achievements, items, dungeons, and NPCs.
- **Prerequisite chains** — given a quest or achievement name, generates a full dependency diagram (as a PNG) showing every quest/achievement that must be completed first.
- **Message translation** — right-click any Discord message to translate it French → English via LibreTranslate. Works on plain text and on images (uses Tesseract OCR to extract text from screenshots before translating).
- **Interactive translation panel** — post a pinned message with buttons so users can trigger lookups without knowing the slash commands.

## How it works

On startup the bot fetches all quest, achievement, and dungeon data from the dofusdb.fr API and stores it in a local cache. The cache is automatically refreshed every 30 days. All translation lookups use this local cache, so they are fast and work offline once populated.

Free-text translation (message and image translate) is handled by a self-hosted [LibreTranslate](https://libretranslate.com/) instance that runs as a sidecar container.

## Prerequisites

- [Docker](https://docs.docker.com/get-docker/) (recommended) or .NET 9 SDK
- A Discord bot token

## Setup

### 1. Create a Discord bot

1. Go to the [Discord Developer Portal](https://discord.com/developers/applications) and create a new application.
2. Under **Bot**, create a bot and copy the token.
3. Enable the **Message Content Intent** under **Privileged Gateway Intents**.
4. Under **OAuth2 → URL Generator**, select the `bot` and `applications.commands` scopes, and at minimum: Send Messages, Read Message History, Attach Files, Use Application Commands.
5. Use the generated URL to invite the bot to your server.

### 2. Configure `config.json`

Create `config.json` next to `docker-compose.yml`:

```json
{
  "discordtoken": "YOUR_DISCORD_BOT_TOKEN"
}
```

## Running

### With Docker (recommended)

The `docker-compose.yml` pulls the bot directly from GitHub and spins up LibreTranslate alongside it.

```bash
docker-compose up -d
```

The game data cache is persisted in a Docker volume (`db_cache`). The `config.json` file is bind-mounted from the host.

### Without Docker

A LibreTranslate instance must be running locally on port `5000` before starting the bot.

```bash
dotnet run
```

To force a full refresh of the game data cache on startup:

```bash
dotnet run -- -forceupdate
```

## Discord commands

### Slash commands (available to all members)

| Command | Description |
|---|---|
| `/translate_item <name>` | Look up an item by English or French name and return both translations. |
| `/translate_quest <name>` | Look up a quest by English or French name and return both translations. |
| `/translate_achieve <name>` | Look up an achievement by English or French name and return both translations. |
| `/translate_dungeon <name>` | Look up a dungeon by English or French name and return both translations. |
| `/translate_npc <name>` | Look up an NPC by name and return both translations. |
| `/prerequisites <name>` | Given a quest or achievement name (English or French), generates a PNG diagram of its full prerequisite chain. |

### Admin commands

Require the **Manage Channels** permission.

| Command | Description |
|---|---|
| `/host_translations` | Posts a persistent message with Quest / Achievement / Item / Dungeon Name buttons in the current channel. Users click a button to open a modal and get a translation without using slash commands directly. |

### Context menu commands

Right-click any message → **Apps** → **Translate**

Translates the message content from French to English. If the message contains image attachments, the bot uses OCR to extract French text from each image before translating.
