Transformation Quickstart
=========================

Hello, and welcome to the quickstart document for the Ambassador's Transformation module! This page will run you through
the absolute basics to start playing with and enjoying yourself with the weird and wonderful transformations available.

### What is this?
The TF module builds on the bot's character plugin, allowing you to transform your characters on a piece-by-piece basis,
shaping their forms to your heart's content. Want to be a half-shark, half-wolf hybrid? Go for it! Want to give your
friend some nice stripes and a glowing drone mask? All yours.

In a nutshell, the bot has a bunch of preprogrammed species in it which you can mix and match, as well as fine-tuning 
your looks with patterns, colours, and shades.

### How do I use it?
Firstly, you need a character. Create one with the following command, replacing "Ada" with whatever name you want it 
to have.

```
!ch create "Ada"
```

If you want to explore the character module and its available options more, give the `!help ch` command a go. Next, you
need to opt into transformations - this is a little bit of security if you don't want others to start messing with 
your looks.

```
!tf opt-in
```

Now you're all set to start shifting. You'll primarily be using these commands:

```
!tf @MyTarget#1234 face shark-dronie
```

```
!tf @MyTarget#1234 colour face "dark black"
```

```
!tf @MyTarget#1234 pattern face swirly purple
```

Most commands follow the target-thing-what order - first, you tell the bot who you want to target, then you tell it what 
thing you want to change, and then into what. Sometimes you can omit some parts and let it figure it out itself - for 
that, check out the manual in `!help tf`. Typically, you can omit the target to target yourself, for example.

To see someone's appearance, use 

```
!tf describe @MyTarget#1234
```

That's it, more or less! There are a few more useful commands which you can use to see what's available, which I highly 
recommend checking out.

```
!tf parts
!tf colours
!tf species
!tf patterns
!tf colour-modifiers
```

### Who can I talk to for help?
Me! I'm on Discord most of the time, and I usually respond pretty quickly. My tag is Jax#7487 - add me and I'll get back
to you as soon as I can. You can also open an issue here on GitHub, if that suits you better.

The bot also has a full manual available in the `!help` command, which I highly recommend that you start with. 

### Can I add more species?
Yes! Adding more species is an awesome way to both contribute to the bot, and to let more people become their favourite 
things. Adding a species involves some writing and getting to know a little bit of the bot's inner workings, but it's 
not so tricky once you get into it. There's a guide available over at [Writing Transformations][wrtf] which describes 
the process.

Have fun!


[wrtf]: https://github.com/Nihlus/digos-ambassador/wiki/Writing-Transformations
