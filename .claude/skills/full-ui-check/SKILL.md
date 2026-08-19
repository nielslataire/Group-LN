---
name: "full-ui-check"
description: "Full page/component UI check combining Anthropic's frontend-design aesthetic review with Vercel's Web Interface Guidelines compliance audit, in one consolidated report."
argument-hint: "<file-or-pattern>"
---

# Full UI Check

Combines two skills into one pass over a single page or component:

- [anthropic-frontend-design](../anthropic-frontend-design/SKILL.md) — is this distinctive, deliberate design or generic/templated output?
- [vercel-web-design-guidelines](../vercel-web-design-guidelines/SKILL.md) — does it comply with 100+ concrete accessibility/performance/UX rules?

## How to run this check

1. Read `.claude/skills/anthropic-frontend-design/SKILL.md` and `.claude/skills/vercel-web-design-guidelines/SKILL.md` in full before starting — both sets of criteria must be loaded, not summarized from memory.
2. Read the target file(s): `$ARGUMENTS`. If a rendered page is reachable (dev server running), look at it, not just the markup/source.
3. Evaluate against both lenses independently, then produce one consolidated report in the format below.

## Report format

```text
# Full UI Check — <file or page>

## Guideline Compliance (Vercel)
<file>:<line> - <MUST/SHOULD violation, terse>
<file>:<line> - <MUST/SHOULD violation, terse>
✓ pass, if no violations found

## Design Quality (Anthropic)
Palette/Type: <1-2 sentences — deliberate & subject-specific, or generic default? name which of the 3 templated defaults it clusters around, if any>
Structure/Hero: <1-2 sentences — does structure encode real information, does the hero commit to a thesis?>
Motion/Restraint: <1-2 sentences — deliberate or scattered/AI-generated-feeling?>
Copy: <1-2 sentences — active voice, specific, in the interface's own voice?>

## Priority Fixes
1. <highest-impact fix, one line, file:line if applicable>
2. <next>
3. <next>
```

Keep the Guideline Compliance section terse and mechanical (per the Vercel skill's own output rules — sacrifice grammar for brevity). Keep the Design Quality section as honest critique, not praise — call out anything that reads as a templated AI default even if it's not technically "wrong." Priority Fixes should mix both lenses, ranked by actual user impact (a11y/broken interaction first, then design distinctiveness, then polish).
