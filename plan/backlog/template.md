# Backlog Item Template

Use this Markdown scaffold when creating or updating items on the GitHub Project board (`https://github.com/users/thomaslazar/projects/1`). Copy the snippet into the item body and replace placeholders as needed.

## Title & Status
- Title format: `PIDP-### – concise summary` (e.g., `PIDP-026 – harden sandbox auth`).
- Treat the project columns as the lifecycle: Backlog → Ready → In progress → In review → Done.
- Keep the checkbox lists in sync with execution; mark them complete before moving the item to `Done`.

## Markdown Scaffold
````markdown
# {Human-readable title}
- Type: Story | Task
- Prerequisites: None | PIDP-00X, PIDP-00Y
- Status: Planned | In Progress | Completed

## Context
Explain rationale, constraints, and dependencies.

## Work Items
- [ ] Checklist of discrete implementation steps.

## Acceptance Criteria
- [ ] Measurable outcomes required for completion.

## Notes
Optional references, follow-ups, or links.
````

> Tip: record prerequisite items inline so contributors know sequencing dependencies. Update the `Status` line to mirror the project column when the item progresses.
