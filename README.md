# Theodore (ThreadBot)

A Discord Bot for maintaining a list of open threads in your server in a dedicated channel.

[Invite ThreadBot to your Discord Server](https://discord.com/api/oauth2/authorize?client_id=950921256314740766&permissions=534723914832&scope=bot%20applications.commands)

[Discord Support Server](https://discord.gg/Za4NAtJJ9v)

![threadbot](Theo-small.png)

## Important Notes

ThreadBot must be set up using the `/set-thread-channel` command (see below). Once a channel is set, a message will be created and that message will house a list of all threads in the server, grouped by their parent channel.

Once ThreadBot is set up, the thread list will be automatically updated every time a thread is created, locked, or deleted. See the `/update-threads` command below in case a manual update is ever needed.

## Commands

**NOTE:** All commmands require the `Manage Threads` permission in order to use them.

---

`/set-thread-channel`

Set the channel for the thread list to exist in. If you do not specify a channel, it will use the current channel.

**NOTE:** It's recommended that this channel only be used for the thread list, otherwise it'll get buried and be less visible.

---

`/update-threads`

Run a manual update of the thread list. You shouldn't need to do this, but in case of downtime and you want/need to force an update, this command can be used.

---

`/version`

Get the current version number of the bot.

---
