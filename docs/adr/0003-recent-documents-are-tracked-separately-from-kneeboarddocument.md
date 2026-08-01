# Recent documents are their own model, an MRU list keyed by path, behind a seam that predates its storage

The welcome screen now offers up to four recently-opened files so the pilot can get back into a
mission without re-navigating a file picker. That needed something to remember *which files*, in
what order, and something to remember it *in* — and both turned out to be decisions worth recording
separately from the reopening feature they support.

**`RecentDocument` is a new, small model — `{ Path, Title, LastOpenedUtc }` — kept out of
`KneeboardDocument` entirely.** `KneeboardDocument` is the deserialized shape of a `.kneeboard`
file: sections, labels, sources — the material a pilot is looking at once it's open. Nothing in that
JSON carries the file's own path; the path is a fact about *how the document was loaded*, supplied by
the picker or by a recents entry, not a fact the file states about itself. Folding `Path` into
`KneeboardDocument` would have made every consumer of that model — the renderer, the section sources,
the tests that build one in memory — carry a field that exists only to serve the recents list.
Keeping it on a separate `RecentDocument` means the loaded-document model stays exactly what its name
says it is, and the recents feature can add fields (a title fallback, a timestamp) without touching
the type that everything else in the app depends on.

**Recording an open is dedup-by-path with move-to-front, capped at four, oldest evicted.**
`RecentDocumentsService.RecordOpenedAsync` removes any existing entry whose `Path` matches the
incoming one before inserting the new entry at the front — so reopening a file already in the list
refreshes its title and timestamp and promotes it, rather than appearing twice at different ages. The
list is then trimmed to four entries from the front, so the entry that falls off is always the one
least recently opened. Four was fixed as the cap because that's what the welcome screen has room to
show as full rows, not because the store can't hold more; if that changes, it's a constant in one
place.

**`IKeyValueStore` exists to make the MRU logic unit-testable, not because the storage mechanism was
in doubt.** The natural home for a handful of persisted strings on a MAUI app is `Preferences`, and
that's what backs it in production (`PreferencesStore`). But `Preferences` is a MAUI Essentials static
that cannot run in the test host — `Kneeboard.Tests` sets `<UseMaui>false</UseMaui>` precisely so it
stays a fast, plain xunit project, and pulling in the real `Preferences` would undo that. `IKeyValueStore`
is deliberately the smallest interface that lets `RecentDocumentsService` be exercised against a
hand-written in-memory fake instead: `Get`/`Set` on a string key, nothing about `Preferences`'s own
richer API (typed getters, containers, removal) leaking through. The seam is about where the MRU
dedup/cap logic can run, not a hedge against switching storage later — if the underlying mechanism
ever does change, that's a one-file swap behind an interface that was never shaped around
`Preferences` in the first place.

## Considered options

Storing `Path` on `KneeboardDocument` and reading recents straight off a list of previously-loaded
documents was the alternative to a separate model. It was rejected for the same reason a stray field
usually is: every place that already constructs or asserts against a `KneeboardDocument` — including
documents that were never opened from a path, if that ever becomes possible — would have to account
for a field that has nothing to do with what the type represents.

Testing `RecentDocumentsService` against the real `Preferences` API, gated behind a platform test
target, was the alternative to `IKeyValueStore`. It was rejected because it would have made the
dedup/cap/eviction logic — the part of this feature actually worth unit testing — depend on a MAUI
runtime to verify, which is exactly what the rest of the service layer avoids.
