# Modnix Changelog

# Version ???

* Faster startup: Scan mod list in parallel with startup checks.
* Faster startup: Check game version in parallel with injection status.
* Faster startup: Check ppml and modnix injection status in parallel, and check only public classes.
* Detect whether game is running, and disables setup / modifications accordingly.
* If mod is in a subfolder, prompt and delete whole subfolder.
* Remove empty folders left by deleting a mod.

# Version 0.80, 2020-02-29

* First public beta.
* One-click install.
* Full GUI showing injection state, game state and communities, mod list, basic mod info, and log of GUI.
* Supported mods: PPML 0.1, PPML 0.2, Modnix.
* Modnix mod phase: SplashMod, MainMod.
* Detect Phoenix Point on default EPG path.
* Rename PPML exe and delete modding dlls from game root on setup, to prevent accidents.