# Twitch integrations for [Ratopia](https://store.steampowered.com/app/2244130/Ratopia/).
Works as of now but is a WIP so there might be errors.

## Features include
* Replacing citizen names with stream chatters.
* Reflect chat messages within the game, making the rats 'talk'.
* Saves chatters that have been picked and block them from being picked again. **Autosave is currently not supported.**

# How to use
Download and place [BepInEx](https://github.com/BepInEx/BepInEx) into the source folder and add this as a plugin.
Run once and edit the config file BepInEx/config/catNull.RatopiaTwitchIntegration.cfg to reflect desired behavior.

## Potential ideas?
* Let the rename button 'kick' users and replace them with someone else at random.
* Priority list for users in config.

## Missing/Bugs
* When making a new game it does not reset the picked chatters list, unlike when loading a save.
* Autosave does not generate an rti file like normal saving does - resulting in, when loading an autosave, picks being able to reoccur.
