What are the files in BepInEx/cache/KydlCore/flight?

Flight recorder reports. When a KydlDev mod logs an error or one of its self-checks fails, KydlCore writes what the tracked creatures and the game were doing in the last two minutes into one short file, so a bug report can include it. Until then it only keeps that in memory. Old reports are removed at game start: older than 3 days, or beyond 100 files (the oldest go first), and at most 30 are written per game start.
