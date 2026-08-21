# Keep Build Designator (RimWorld 1.6)

Compile with `RIMWORLD_MANAGED` set to RimWorld 1.6's `RimWorldWin64_Data\Managed` directory and `HARMONY_PATH` set to an existing `0Harmony.dll` (for example one supplied by a Harmony-based mod).
The Harmony patch intercepts right-click while a `Designator_Build` drag is active, clears only the drag state, and leaves the build designator selected.
