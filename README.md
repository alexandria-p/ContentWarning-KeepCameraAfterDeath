# Keep Camera After Death - mod for Content Warning

You no longer need to fear your crew's ineptitude!

In the event of:
- total crew death, or
- a major fumble where your cameraman left the camera behind before returning to the diving bell.

This mod respawns the camera at its regular spawn position on the surface when the crew returns in the evening. This means you can still export your footage and upload it to spooktube to watch together on the sofa.

As a bonus, you can optionally incentivise your players to stay alive and bring their camera home with rewards that are configurable in the game settings.

The host decides whether or not rewards should be distributed for returning with the camera. If they choose to enable rewards, they can configure the amount of Meta Coins (MC) and cash revenue the crew should receive.

# Contact Us

**If you found this mod on any site except for *Thunderstore* or *r2 Modman*, then I cannot guarantee the safety of the file you have downloaded.** 

Please report this back to me on my GitHub https://github.com/alexandria-p or my Twitter account https://twitter.com/HumbleKraken.

# Installation steps

**All crew members must have this mod installed, and follow these steps**
  
* Install the *r2 Modman* mod manager via *Thunderstore*
* Add this mod to *r2 Modman* and make sure it is enabled
* Run Content Warning via *r2 Modman*. You may need to already have Steam running in the background.

# How does it work?

Here is a breakdown of what happens under the hood.

When a camera is left behind underground, or all crew members die and the camera is forcibly dropped from their inventory, as the diving bell returns to the surface it does a check for any items left behind that it wants to persist for the players to be able to find again in a later dive.

Cameras are one of the item types that are set to persist for future dives (within the same week).

This mod intercepts at this point. It picks up:
- when a camera was left behind this run
- and if that camera does not already exist in the list of persistent objects (so it must be newly dropped)

Instead of letting the crew find that camera again in a future run, this mod will instead save the footage from that camera and load it onto a new camera that it spawns on the surface.

Any camera that this mod "saves" will no longer spawn underground on future runs, to prevent duplicate footage from existing (makes sense, right?)

# Does this mod work if my crew has multiple cameras?

Yes - with a caveat.

Remember how this mod searches for & preserves the footage from dropped cameras when a run ends?

It only preserves the footage of the *first* newly dropped camera it finds, to load onto the new camera it spawns on the surface.

If your crew drop multiple cameras underground in a single run, then all the remaining cameras will continue to persist in the underground world (as they do in the vanilla game) for your crew to find.

# Future improvements

My ideas mostly revolve around handling if a crew somehow has multiple cameras, and manages to leave more than one camera behind on their dive. 

Maybe in the future this mod will save all cameras dropped in a run underground, and spawn as many new cameras as it needs on the surface to copy that footage onto. 

Right now it is easiest for me to only save a single camera's worth of footage, because I am piggy-backing how the game spawns that new (single) camera on the porch at the start of a new day.

# Dependencies

- Hamunii-AutoHookGenPatcher-1.0.3
- CommanderCat101-ContentSettings-1.2.2
- RugbugRedfern-MyceliumNetworking-1.0.14

# References

Scaffolded using Hamunii's tutorials: https://www.youtube.com/watch?v=o0lVCSSKqTY

Uses the Xilo's Content Warning Templates: https://github.com/ContentWarningCommunity/Content-Warning-Mod-Templates

This template uses MonoMod. 
