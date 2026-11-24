# Changelog

## 1.1.2
- New: KY-Items has a grid view (4 square icons per row with name and prefab) next to the list; switch at the top right, remembered in `[General] KY-Items View`.
- New: a Materials tab in the KY hub for deposits and crafting ingredients (`KyKind.Material`).
- New for mod authors: `ModLog.ExpectWarning` marks warnings a test provokes on purpose, so they do not count as failures.
- New for mod authors' TestLab suites: the start-of-test arena cleanup removes dropped items, creatures, and logs, stumps and pieces that were not in the world template, and puts back terrain a test changed.
- Changed for mod authors: `KyPage.Sections` and `KySection.Rows` are read-only lists; sections and rows are added with `Section(...)`, `Value(...)`, `Slider(...)` and the other builder methods as before. A mod that reads them must be rebuilt against this version.
- Changed: every method logs on entry and on each return when the debug log is on; per-frame lines are throttled, so a log stays readable. Less log spam overall (the flight recorder's echo and repeated network lines are throttled).
- Fixed: Valheim could hang at start because the log listener wrote log lines about itself without end.
- Fixed: logged values stay on one line, and a logged value containing the text "CHECK OFF" no longer counts as a failed check.
- Fixed: the UI kit compiles again in a mod's own Unity project.

### Testing
- Unit tests: 570 passed. Integration tests: 39 passed.
- Coverage of the testable code: 98.4%. Mutation testing (Stryker): 96.3% of the mutants are caught by the tests.
- SonarQube: no Medium or High impact issue.
- DrakesTestLab: the KydlCore suite green 3 times in a row on this build, and GunPowdered's full suites on top of it.

## 1.1.1
- New: in the KY hub (F6) every settings category of a mod has its own tab, for every mod. Categories that belong together share one tab (for example all "Staff of Protection - ..." sections of ComfortMagicBubbles). "General" comes first and "DebugLog" last, also when a mod has tabs of its own (they sit in between). A mod with a single category keeps its one "Settings" tab.
- New for mod authors: `KyHub.SetGroupPanel` puts a panel of the mod's own at the end of one settings tab and can leave out the plain entries of the sections that panel draws itself (they stay in the F1 menu and the cfg file); `KyHub.RemoveGroupPanel` takes it away again. An overload with `atTop` puts the panel above the entries instead, and `KyHub.SetGroupPage` does the same with a declarative page (buttons and notes, like `AddPage`), so a mod's own actions share the tab of their config category instead of a second tab with the same name.
- Changed: every page in the KY hub is now shown with KydlCore's own text sizes, font and text colours, also a page a mod builds itself (before, such a page could be shown with bigger text than the Settings pages). A size, font or text colour a mod sets is replaced.
- Fixed: on a Settings page, a value that a mod keeps in step with another one now follows at once. Example: the `Hex` box of a colour row did not change while the Red / Green / Blue sliders were moved.

### Testing
- Unit tests: 543 passed. Integration tests: 36 passed.
- Property-based tests (FsCheck, included in the unit tests above): 23 property checks with generated inputs.
- Coverage of the testable code: 98.3%. Mutation testing (Stryker): 96.3% of the mutants are caught by the tests.
- Performance benchmarks (BenchmarkDotNet): 13.
- Static analysis (SonarQube): no Medium or High issues.
- In game with DrakesTestLab: the full suite of 8 tests per run, run 3 times in a row on the release build, all green (the release requirement).

## 1.1.0
- New for mod authors: the flight recorder. `FlightRecorder.Track(component, label, facts)` keeps the last two minutes of a creature or object (position, speed, health, AI target, network owner, whether its zone is loaded, the mod's own facts); `FlightRecorder.Dump(reason)`, `Note`, `Untrack`, `LastDumpPath` and `TrackedCount` complete it. It is always on, in memory only.
- It writes a report file to `BepInEx/cache/KydlCore/flight/` only when something goes wrong: a failed DrakesTestLab test, a CHECK OFF, an error of a KydlDev mod, or a mod's own `FlightRecorder.Dump`. At most 30 files per game start; files older than 3 days and everything beyond the newest 100 are removed at game start. Attach such a file to a bug report.
- New for mod authors: `ModLog.State`, `ModLog.Decision`, `ModLog.Every` and `ModLog.IsDue` (debug lines as before, and always on the flight recorder's timeline, even with the debug log off).
- New settings in the `[Diagnostics]` section (hub Settings tab): `FlightSampleSeconds` (1 s), `FlightWindowSeconds` (120 s), `FlightMaxDumps` (30), `FlightKeepDays` (3), `FlightKeepFiles` (100). They only size the recorder; it cannot be switched off.
- Nothing was removed or renamed: mods built for KydlCore 1.0.0 keep working. A server that runs 1.1.0 needs the same version on every player who has KydlCore (exact match).
- Tested in game with the full TestLab suite (8 tests, including the new flight recorder test): 3 green runs in a row on 2026-11-08; no errors from this mod, all 13 KydlDev mods loaded together.

## 1.0.0
- Tested in game with the full TestLab suite (7 tests): 3 green runs in a row on 2026-11-05, plus the ecosystem run with all 12 KydlDev mods loaded together.
- New for mod authors: PieceLearning (learn a build piece with the game's normal "new piece" message, check known pieces / items).
- Settings pages: a setting name that is too long is cut with "..." instead of running under its slider, long text values
  (webhook URLs) stay inside their box, and sliders are about 10 % shorter with a gap to the name. New mod icon.
- KydlDev spec layout; KY-Items: Jotunn-free registry with namespaced contributions (KyItems.Contribute / Withdraw / FindById), duplicates rejected; KyHub.HasTab; KydlCore can no longer be switched off (no EnableAll); exact network version, joins servers without KydlCore (VersionCheckOnly). Checked in game on 2026-11-04 (clean start and quit with all mods loaded, no errors from this mod).
- First version (project created 2026-10-29). Checked in game on 2026-11-04 (clean start and quit with all mods loaded, no errors from this mod).
