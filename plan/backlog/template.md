# Backlog Item Template

## Naming Convention
File names must follow `PIDP-{NNN}-{kebab-summary}.md`, where:
- `{NNN}` is a zero-padded sequence number in backlog order (e.g., `001`, `014`).
- `{kebab-summary}` is a concise kebab-case description (≤ six words).

Number items sequentially as they are added so dependency chains remain clear.

Example: `PIDP-002-validate-config-inputs.md`

## Front Matter
Each backlog file starts with:
```
# {Title}
- Type: Story | Task
- Prerequisites: None | PIDP-00X, PIDP-00Y
- Status: Planned

Use the `Prerequisites` field to list backlog IDs that must be completed before this item begins.
```

## Body Sections
```
## Context
Explain rationale, constraints, and dependencies.

## Work Items
- [ ] Bullet checklist detailing steps.

## Acceptance Criteria
- [ ] Testable outcomes required to mark complete.

## Notes
Optional additional references or follow-ups.
```

## Lifecycle
- Active items live under `/plan/backlog/`.
- Completed items move to `/plan/backlog/done/` with status updated to `Completed`.
