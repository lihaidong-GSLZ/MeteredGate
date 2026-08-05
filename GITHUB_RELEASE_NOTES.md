# Metered Gate 0.3.0

0.3.0 removes the fragile placement-controller patching architecture. The player prototype now derives directly from `LayoutEntityProto` and reuses the original Flat Connector's layout, ports, graphics, icon, and native placement-height range.

## Highlights

- No Harmony dependency and no bundled `0Harmony.dll`;
- no `ZipperProto` inheritance or Flat Balancer data dependency;
- continuous 20 kW electricity consumption with native power priority;
- replayable inspector commands;
- safe v1 → v2 migration for 0.1.0/0.2.0 buildings;
- formal mod, assembly, entry-point, prototype, and configuration identities preserved.

## Install

Download `MeteredGate-0.3.0.zip`, delete the old `MeteredGate` mod directory, and extract the complete new `MeteredGate` folder into the game's Mods directory. Deleting the old directory is important because 0.3.0 no longer ships `0Harmony.dll`.

Back up saves before upgrading. Do not remove the mod while a save contains Metered Gate buildings.
