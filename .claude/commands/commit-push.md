---
description: Commit local changes and push to GitHub via a Sonnet subagent
argument-hint: "[optional: extra instructions, e.g. a commit message or scope]"
---

Launch a **subagent** to do the commit-and-push work. Do not do it yourself in the main loop.

Use the `Agent` tool with:
- `subagent_type: general-purpose`
- `model: sonnet`
- `run_in_background: false`
- `description: "Commit and push changes"`

Give the subagent this prompt:

commit local changes and push current branch into upstream
