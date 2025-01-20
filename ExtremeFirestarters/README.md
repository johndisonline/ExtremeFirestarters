Version Log

0.4.0
- Rebalanced flint & flint - did twice the damage it should have.
- Added Config file
	- 4 modes - custom, hardcore, vanilla+, and "items only"
	- Default setting is hardcore, with features as described.
	- vanilla+ turns off health damage, speeds up firestarters, and increases their durability (But keeps hunger damage and durability tweaks)
	- items only only adds the new items
	- Custom mode allows you to toggle invidiual config settings
- Added "pyromaniac" trait and added to hunter and malefactor.
- Rewrote Firestarter class instead of extending.

Known Bug: The ingition tooltip (above fires and other ignitable objecsts) no longer shows Firestarters.

0.3.0
- Fixed bug where hunger would go negative.
- Added 'merge' ability to matches - dragging one on top the other will merge durability.
- Added `ignitionSound`s, chose best vanilla sounds for item
- Confirmed working on game version 1.20.1
- Survival good trader sells bow drill and flint and steel (expensive!)
- Tool Vessel can drop crude or regular bow drill
- Default Firestarter can now be stored on a tool rack, and doesn't need a wall
- All Firestarters can now be stored on a tool rack, and some on a shelf.
- Change ground storage to halves or quandrants based on size of firestarter.

Known Bug: The default firestarter sound still plays despite the other being played. Will have to entirely remove base class instead of extend.


0.2.0
- Stabilized for game version 1.20.0

0.1.0
- alpha release

Thanks to:
Configure Everything by DanaCraluminum - https://mods.vintagestory.at/configureeverything 
For figuring out how to do config files

flintandsteel by Minni6in - https://mods.vintagestory.at/show/mod/1792|
And
Better Flint and Steel - https://mods.vintagestory.at/flintandsteeladdon

I don't think i ended up using any of their code, but as this was my first mod, i learned a bit from looking at theirs.
