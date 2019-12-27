Transform Commands
==================
## Summary
These commands are prefixed with `transform`. You can also use `shift` or `tf` instead of `transform`.

Transformation-related commands, such as transforming certain body parts or saving transforms as characters.

## Commands
### *transform*
#### Overloads
**`!transform Center Hair "placeholder"`**

Transforms the given bodypart into the given species on yourself.

| Name | Type | Optional |
| --- | --- | --- |
| chirality | Chirality | `no` |
| bodyPart | Bodypart | `no` |
| species | string | `no` |

**`!transform Hair "placeholder"`**

Transforms the given bodypart into the given species on yourself.

| Name | Type | Optional |
| --- | --- | --- |
| bodyPart | Bodypart | `no` |
| species | string | `no` |

**`!transform "placeholder" Hair "placeholder"`**

Transforms the given bodypart of the target user into the given species.

| Name | Type | Optional |
| --- | --- | --- |
| target | IUser | `no` |
| bodyPart | Bodypart | `no` |
| species | string | `no` |

**`!transform "placeholder" Center Hair "placeholder"`**

Transforms the given bodypart of the target user into the given species.

| Name | Type | Optional |
| --- | --- | --- |
| target | IUser | `no` |
| chirality | Chirality | `no` |
| bodyPart | Bodypart | `no` |
| species | string | `no` |

---

### *colour*
#### Overloads
**`!transform colour Center Hair "placeholder"`**

Transforms the base colour of the given bodypart on yourself into the given colour.

| Name | Type | Optional |
| --- | --- | --- |
| chirality | Chirality | `no` |
| bodypart | Bodypart | `no` |
| colour | Colour | `no` |

**`!transform colour Hair "placeholder"`**

Transforms the base colour of the given bodypart on yourself into the given colour.

| Name | Type | Optional |
| --- | --- | --- |
| bodypart | Bodypart | `no` |
| colour | Colour | `no` |

**`!transform colour "placeholder" Hair "placeholder"`**

Transforms the base colour of the given bodypart on the target user into the given colour.

| Name | Type | Optional |
| --- | --- | --- |
| target | IUser | `no` |
| bodyPart | Bodypart | `no` |
| colour | Colour | `no` |

**`!transform colour "placeholder" Center Hair "placeholder"`**

Transforms the base colour of the given bodypart on the target user into the given colour.

| Name | Type | Optional |
| --- | --- | --- |
| target | IUser | `no` |
| chirality | Chirality | `no` |
| bodyPart | Bodypart | `no` |
| colour | Colour | `no` |

---

### *pattern*
#### Overloads
**`!transform pattern Hair Striped "placeholder"`**

Transforms the pattern on the given bodypart on yourself into the given pattern and secondary colour.

| Name | Type | Optional |
| --- | --- | --- |
| bodypart | Bodypart | `no` |
| pattern | Pattern | `no` |
| colour | Colour | `no` |

**`!transform pattern Center Hair Striped "placeholder"`**

Transforms the pattern on the given bodypart on yourself into the given pattern and secondary colour.

| Name | Type | Optional |
| --- | --- | --- |
| chirality | Chirality | `no` |
| bodypart | Bodypart | `no` |
| pattern | Pattern | `no` |
| colour | Colour | `no` |

**`!transform pattern "placeholder" Hair Striped "placeholder"`**

Transforms the pattern on the given bodypart on the target user into the given pattern and secondary colour.

| Name | Type | Optional |
| --- | --- | --- |
| target | IUser | `no` |
| bodyPart | Bodypart | `no` |
| pattern | Pattern | `no` |
| colour | Colour | `no` |

**`!transform pattern "placeholder" Center Hair Striped "placeholder"`**

Transforms the pattern on the given bodypart on the target user into the given pattern and secondary colour.

| Name | Type | Optional |
| --- | --- | --- |
| target | IUser | `no` |
| chirality | Chirality | `no` |
| bodyPart | Bodypart | `no` |
| pattern | Pattern | `no` |
| colour | Colour | `no` |

---

### *pattern-colour*
#### Overloads
**`!transform pattern-colour Center Hair "placeholder"`**

Transforms the colour of the pattern on the given bodypart on yourself to the given colour.

| Name | Type | Optional |
| --- | --- | --- |
| chirality | Chirality | `no` |
| bodypart | Bodypart | `no` |
| colour | Colour | `no` |

**`!transform pattern-colour Hair "placeholder"`**

Transforms the colour of the pattern on the given bodypart on yourself to the given colour.

| Name | Type | Optional |
| --- | --- | --- |
| bodypart | Bodypart | `no` |
| colour | Colour | `no` |

**`!transform pattern-colour "placeholder" Hair "placeholder"`**

Transforms the colour of the pattern on the given bodypart on the target user to the given colour.

| Name | Type | Optional |
| --- | --- | --- |
| target | IUser | `no` |
| bodyPart | Bodypart | `no` |
| colour | Colour | `no` |

**`!transform pattern-colour "placeholder" Center Hair "placeholder"`**

Transforms the colour of the pattern on the given bodypart on the target user to the given colour.

| Name | Type | Optional |
| --- | --- | --- |
| target | IUser | `no` |
| chirality | Chirality | `no` |
| bodyPart | Bodypart | `no` |
| colour | Colour | `no` |

---

### *list-available*
#### Overloads
**`!transform list-available` (as well as `transform list-species`, `transform species`, or `transform list`)**

Lists the available transformation species.

**`!transform list-available Hair` (as well as `transform list-species`, `transform species`, or `transform list`)**

Lists the available transformations for a given bodypart.

| Name | Type | Optional |
| --- | --- | --- |
| bodyPart | Bodypart | `no` |

---

### *parts*
#### Overloads
**`!transform parts` (as well as `transform list-bodyparts` or `transform bodyparts`)**

Lists the available bodyparts.

---

### *colours*
#### Overloads
**`!transform colours` (as well as `transform list-colours`, `transform list-shades`, or `transform shades`)**

Lists the available colours.

---

### *colour-modifiers*
#### Overloads
**`!transform colour-modifiers` (as well as `transform list-colour-modifiers`, `transform list-shade-modifiers`, or `transform shade-modifiers`)**

Lists the available colour modifiers.

---

### *colour-patterns*
#### Overloads
**`!transform colour-patterns` (as well as `transform list-patterns` or `transform patterns`)**

Lists the available patterns.

---

### *describe*
#### Overloads
**`!transform describe`**

Describes the current physical appearance of the current character.

**`!transform describe "placeholder"`**

Describes the current physical appearance of a character.

| Name | Type | Optional |
| --- | --- | --- |
| character | Character | `no` |

---

### *reset*
#### Overloads
**`!transform reset`**

Resets your form to your default one.

---

### *set-default*
#### Overloads
**`!transform set-default` (or `transform save-default`)**

Sets your current appearance as your current character's default one.

---

### *default-opt-in*
#### Overloads
**`!transform default-opt-in true`**

Sets your default setting for opting in or out of transformations on servers you join.

| Name | Type | Optional |
| --- | --- | --- |
| shouldOptIn | bool | `yes` |

---

### *opt-in*
#### Overloads
**`!transform opt-in`**

Opts into the transformation module on this server.

---

### *opt-out*
#### Overloads
**`!transform opt-out`**

Opts out of the transformation module on this server.

---

### *default-protection*
#### Overloads
**`!transform default-protection Blacklist`**

Sets your default protection type for transformations on servers you join. Available types are Whitelist and Blacklist.

| Name | Type | Optional |
| --- | --- | --- |
| protectionType | ProtectionType | `no` |

---

### *protection*
#### Overloads
**`!transform protection Blacklist`**

Sets your protection type for transformations. Available types are Whitelist and Blacklist.

| Name | Type | Optional |
| --- | --- | --- |
| protectionType | ProtectionType | `no` |

---

### *whitelist*
#### Overloads
**`!transform whitelist "placeholder"`**

Whitelists a user, allowing them to transform you.

| Name | Type | Optional |
| --- | --- | --- |
| user | IUser | `no` |

---

### *blacklist*
#### Overloads
**`!transform blacklist "placeholder"`**

Blacklists a user, preventing them from transforming you.

| Name | Type | Optional |
| --- | --- | --- |
| user | IUser | `no` |

---

### *update-db*
#### Overloads
**`!transform update-db`**

Updates the transformation database with the bundled definitions.

<sub><sup>Generated by DIGOS.Ambassador.Doc</sup></sub>