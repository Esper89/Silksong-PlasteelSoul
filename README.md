# Plasteel Soul

Plasteel Soul is a Silksong mod that lets you access Steel Soul-exclusive game content outside of
Steel Soul mode.

## Installation

You can download this mod from the
[releases page](https://github.com/Esper89/Silksong-PlasteelSoul/releases/latest), below the
changelog. This mod requires [BepInEx](https://github.com/BepInEx/BepInEx) 5, and is installed to
your `BepInEx/plugins` directory.
[Configuration Manager](https://github.com/BepInEx/BepInEx.ConfigurationManager) can be used with
this mod.

## Usage

Once this mod is installed, by default, you will be able to access all Steel Soul-exclusive game
content through various means. Specifically, in non-Steel Soul mode,

- Steel Seer Zi is accessible in the same location in the Blasted Steps and offers the same _A
  Vassal Lost_ wish as she does in Steel Soul mode. The Summoned Saviour boss fight is identical and
  the rewarded Growstone functions normally.
- The Shell Satchel can be purchased from Jubilana in Songclave for 290 rosaries after completing
  the _The Lost Merchant_ wish.
- Skynx is accessible in the same location east of Styx's nest in Sinner's Road, and continues to
  provide rosaries in exchange for Silkeaters. Styx is alive and well regardless.
- The Chrome Bell Lacquer can be applied to your Bellhome. You can still choose the White Bell
  Lacquer instead.

Each of these features can be disabled by editing this mod's config file at
`BepInEx/config/Esper89.PlasteelSoul.cfg` or using Configuration Manager.

## Languages

This mod adds new text to the game. If your language is not supported by this mod, you can add
support for it yourself by copying `languages/en.json` to `languages/xx.json` (where `xx` is your
language's lowercase two-letter code) and translating all of the text.

## Building

To build this mod for development, run `dotnet build` in the project's root directory. The output
will be in `target/Debug`. If you create a text file in the project root called `game-dir.txt` and
input the path to your Silksong installation, the output of debug builds will be automatically
installed into that game directory.

To build this mod in release mode, run `dotnet build --configuration Release`. This will create
`target/PlasteelSoul.zip` for easy distribution.

## License

Copyright © 2025 Esper Thomson

This program is free software: you can redistribute it and/or modify it under the terms of version
3 of the GNU Affero General Public License as published by the Free Software Foundation.

This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without
even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero
General Public License for more details.

You should have received a copy of the GNU Affero General Public License along with this program.
If not, see <https://www.gnu.org/licenses>.

Additional permission under GNU AGPL version 3 section 7

If you modify this Program, or any covered work, by linking or combining it with Hollow Knight:
Silksong (or a modified version of that program), containing parts covered by the terms of its
license, the licensors of this Program grant you additional permission to convey the resulting work.
