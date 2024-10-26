# Doki
 An experimental DDLC+ modding framework, written in C#.

# GOAL
Eventually, I'd imagine a future where it'd be as easy to mod Plus as it is to mod the base game. And this is an attempt in getting us closer to that. A write up on how the game works internally is coming soon, which should clear up some confusion on how this mod works and on how to mod DDLC+'s scripts and assets properly. <br>
# WARNING
This is not 100% complete and is more of a proof of concept. <br> This project is also dedicated to Kizby who passed away in late 2022, she's the one who inspired me to mod Plus way back in July 2021 with her TestDDLCMod.

# CREDITS
Kizby - Assets related patches, and her efforts in preventing bundles from being unloaded. <br>
Yedrah - Custom background image (In the DokiModTest's asset bundle) <br>
noia - Everything else

# HOW TO SETUP
Downlod dnSpy if you don't have it already -> and drag DDLC.dll from the DDLC+'s Managed folder into it, find the "RenpyDialogueLine" class and right click -> Edit Type (Change it from Visibility: NonPublic to Visibility: Public) and save change (File -> Save Module). <br>
The reason for this is so dialogue can be parsed & instantiated, and manipulated. I don't know why the developers made this class internal by default. <br>

Now it should be possible to Build the mod loader (Doki), make sure the game's assemblies are referenced correctly. If not, remove the references and add them again from the Managed folder. <br>

Build Doki, drag Doki.dll into the base game folder (Where the .exe is, etc) <br>

Download the modified UnityEngine.CoreModule.dll from the releases tab, and replace it in your game's managed folder. (This is important, if you don't do this, the mod loader will not work as it won't be initialized!)

Run the game once, wait for the Mod Loader to initialize, close the game. <br>

Build DokiModTest -> drag the DokiModTest.dll to Doki\Mods in the game's directory. <br>
Move the DokiModTest folder in the releases to the game's directory. (Basically the mod's working files, the example script, asset bundle it uses, etc) <br>

Enjoy semi-working DDLC+ framework! lol
