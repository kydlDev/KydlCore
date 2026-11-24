How does my mod get a tab in the KY hub?

With KyHub: AddTab or AddPage for the mod's own pages (a KyPage of sections and rows, which KydlCore draws in the shared look and rebuilds when its State text changes), ConfigEntries for the settings tabs (one tab per config category), RemoveTab / HasTab to manage them. Items and pieces are listed with KyItems.Contribute(new KyContribution { Id = "<modid>:<item>", ... }); duplicate ids are rejected. Every creature or object the mod moves should be tracked with FlightRecorder.Track(component, label, () => facts).
