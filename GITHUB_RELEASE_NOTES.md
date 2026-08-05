# Metered Gate 0.3.1

0.3.1 fixes the height restriction that was not actually enforced in 0.3.0. `PlacementHeightRange` controls the placement tool, but ordinary `LayoutEntityProto` buildings do not automatically receive a final range validator.

The mod now registers `MeteredGateHeightValidator` through the game's public dependency and entity-validation APIs. It calculates placement height relative to rounded terrain height and enforces the original Flat Connector range for normal placement, Shift movement, cloning, blueprints, and moves.

## Cycle controls

The Inspector now provides `-30 s`, `-1 s`, `+1 s`, and `+30 s` buttons. All changes continue to use the replayable input-command path, reset the current cycle, and clamp the configured duration to 1–3600 seconds.

## Other behavior

- Still no Harmony dependency or bundled `0Harmony.dll`;
- no ZipperProto/MiniZipperProto inheritance;
- continuous 20 kW consumption with native power priority;
- existing v1 → v2 migration for 0.1.0/0.2.0 buildings remains;
- formal IDs and save format are unchanged.

## Install

Download `MeteredGate-0.3.1.zip`, delete the old `MeteredGate` mod directory, and extract the complete new folder into the game's Mods directory. Back up saves before upgrading.
