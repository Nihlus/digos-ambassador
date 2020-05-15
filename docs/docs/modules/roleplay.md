Roleplay Commands
=================
## Summary
These commands are prefixed with `roleplay`. You can also use `rp` instead of `roleplay`.

Commands for interacting with and managing channel roleplays.

## Submodules
* [Server](roleplay_server.md)
* [Set](roleplay_set.md)

## Commands
### *show*
#### Overloads
**`!roleplay show` (or `roleplay info`)**

Shows information about the current roleplay.

**`!roleplay show a-walk-in-the-park` (or `roleplay info`)**

Shows information about the specified roleplay.

| Name | Type | Optional |
| --- | --- | --- |
| roleplay | Roleplay | `no` |

---

### *list*
#### Overloads
**`!roleplay list`**

Lists all available roleplays in the server.

---

### *list-owned*
#### Overloads
**`!roleplay list-owned @Ada`**

Lists the roleplays that the given user owns.

| Name | Type | Optional |
| --- | --- | --- |
| discordUser | IGuildUser | `yes` |

---

### *create*
#### Overloads
**`!roleplay create John "My short summary" true true`**

Creates a new roleplay with the specified name.

| Name | Type | Optional |
| --- | --- | --- |
| roleplayName | string | `no` |
| roleplaySummary | string | `yes` |
| isNSFW | bool | `yes` |
| isPublic | bool | `yes` |

---

### *delete*
#### Overloads
**`!roleplay delete a-walk-in-the-park`**

Deletes the specified roleplay.

| Name | Type | Optional |
| --- | --- | --- |
| roleplay | Roleplay | `no` |

---

### *join*
#### Overloads
**`!roleplay join a-walk-in-the-park`**

Joins the roleplay owned by the given person with the given name.

| Name | Type | Optional |
| --- | --- | --- |
| roleplay | Roleplay | `no` |

---

### *invite*
#### Overloads
**`!roleplay invite @Ada a-walk-in-the-park`**

Invites the specified user to the given roleplay.

| Name | Type | Optional |
| --- | --- | --- |
| playerToInvite | IGuildUser | `no` |
| roleplay | Roleplay | `no` |

---

### *leave*
#### Overloads
**`!roleplay leave a-walk-in-the-park`**

Leaves the roleplay owned by the given person with the given name.

| Name | Type | Optional |
| --- | --- | --- |
| roleplay | Roleplay | `no` |

---

### *kick*
#### Overloads
**`!roleplay kick @Ada a-walk-in-the-park`**

Kicks the given user from the named roleplay.

| Name | Type | Optional |
| --- | --- | --- |
| discordUser | IGuildUser | `no` |
| roleplay | Roleplay | `no` |

---

### *channel*
#### Overloads
**`!roleplay channel a-walk-in-the-park`**

Makes the roleplay with the given name current in the current channel.

| Name | Type | Optional |
| --- | --- | --- |
| roleplay | Roleplay | `no` |

---

### *start*
#### Overloads
**`!roleplay start a-walk-in-the-park`**

Starts the roleplay with the given name.

| Name | Type | Optional |
| --- | --- | --- |
| roleplay | Roleplay | `no` |

---

### *stop*
#### Overloads
**`!roleplay stop a-walk-in-the-park`**

Stops the given roleplay.

| Name | Type | Optional |
| --- | --- | --- |
| roleplay | Roleplay | `no` |

---

### *include-previous*
#### Overloads
**`!roleplay include-previous a-walk-in-the-park 660204034829058070 660204034829058070`**

Includes previous messages into the roleplay, starting at the given message.

| Name | Type | Optional |
| --- | --- | --- |
| roleplay | Roleplay | `no` |
| startMessage | IMessage | `no` |
| finalMessage | IMessage | `yes` |

---

### *transfer-ownership*
#### Overloads
**`!roleplay transfer-ownership @Ada a-walk-in-the-park`**

Transfers ownership of the named roleplay to the specified user.

| Name | Type | Optional |
| --- | --- | --- |
| newOwner | IGuildUser | `no` |
| roleplay | Roleplay | `no` |

---

### *export*
#### Overloads
**`!roleplay export a-walk-in-the-park PDF`**

 Exports the named roleplay owned by the given user, sending you a file with the contents.

| Name | Type | Optional |
| --- | --- | --- |
| roleplay | Roleplay | `no` |
| format | ExportFormat | `yes` |

---

### *replay*
#### Overloads
**`!roleplay replay a-walk-in-the-park 5m 5m`**

Replays the named roleplay owned by the given user to you.

| Name | Type | Optional |
| --- | --- | --- |
| roleplay | Roleplay | `no` |
| from | DateTimeOffset | `yes` |
| to | DateTimeOffset | `yes` |

---

### *view*
#### Overloads
**`!roleplay view a-walk-in-the-park`**

Views the given roleplay, allowing you to read the channel.

| Name | Type | Optional |
| --- | --- | --- |
| roleplay | Roleplay | `no` |

---

### *hide*
#### Overloads
**`!roleplay hide a-walk-in-the-park`**

Hides the given roleplay.

| Name | Type | Optional |
| --- | --- | --- |
| roleplay | Roleplay | `no` |

---

### *hide-all*
#### Overloads
**`!roleplay hide-all`**

Hides all roleplays in the server for the user.

---

### *refresh*
#### Overloads
**`!roleplay refresh a-walk-in-the-park`**

Manually refreshes the given roleplay, resetting its last-updated time to now.

| Name | Type | Optional |
| --- | --- | --- |
| roleplay | Roleplay | `no` |

---

### *reset-permissions*
#### Overloads
**`!roleplay reset-permissions`**

Resets the permission set of all dedicated channels.

---

### *move-to*
#### Overloads
**`!roleplay move-to John @Ada @Bea` (as well as `roleplay copy-to` or `roleplay move`)**

Moves an ongoing roleplay outside of the bot's systems into a channel with the given name.

| Name | Type | Optional |
| --- | --- | --- |
| newName | string | `no` |
| participants | IGuildUser[] | `no` |

<sub><sup>Generated by DIGOS.Ambassador.Doc</sup></sub>