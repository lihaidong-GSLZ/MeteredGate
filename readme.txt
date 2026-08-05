Metered Gate 0.3.1

A 1x1 metered logistics gate for flat conveyors.

- Configurable cycle duration and release quota.
- Inspector cycle controls: -30 s, -10 s, -1 s, +1 s, +10 s, and +30 s.
- One-item internal buffer and round-robin outputs.
- Four dynamically configurable flat-conveyor ports.
- Uses the original Flat Connector layout, model, icon, and height range.
- Enforces that range with the game's public entity-addition validation API.
- Consumes 20 kW continuously while enabled and uses the native power-priority UI.
- Does not inherit ZipperProto or MiniZipperProto.
- Does not use or bundle Harmony.
- Migrates existing 0.1.0/0.2.0 MeteredGate_Entity save data.

Install by extracting the complete MeteredGate folder into the game's Mods directory.
Delete the old MeteredGate folder first so that the obsolete 0Harmony.dll from 0.2.0 is not left behind.
Do not remove this mod while a save still contains Metered Gate buildings.
