# Where the pilot is in the document lives in one immutable Binder

The kneeboard screen used to keep the pilot's place in several mutable fields — a selected section
index and a current page index on the view model, a last-viewed page on each tab's view model — and
publishing a change meant assigning them in an order that never let a binding read two of them
mid-update. It didn't hold: a stale index paired with a newly selected section reached the native
image view as a real, if brief, wrong page, and the fix was a comment explaining why one field had to
be written through its backing field rather than its setter.

All of it now lives in `Binder`, an immutable record holding every section's pages, the page the pilot
is on in each, and how each is framed. Turning, selecting and framing return a new binder; anything
that changes nothing returns the same instance. The screen swaps that one field and then announces, so
nothing it publishes about the pilot's place can disagree with anything else it publishes about it —
and the reference-identity no-op doubles as the signal for "nothing to publish".

The claim is deliberately narrow: **where the pilot is**, not all of the screen's state. The list of
tabs is still a second observable field, and it is still assigned before the binder is published,
because publishing a binder lights one of those tabs and they have to exist by then. That ordering
remains, and it is the only one left.

## Considered options

Mutable per-section state on `SectionViewModel`, extended field by field as features arrive, was the
alternative and the status quo. It's a smaller diff per feature and it keeps observable properties
where MAUI expects them. Rejected because each new piece of per-section state — the framing was the
second — adds another field to keep in step during a switch, and the ordering discipline that requires
is invisible until it breaks in front of a pilot.
