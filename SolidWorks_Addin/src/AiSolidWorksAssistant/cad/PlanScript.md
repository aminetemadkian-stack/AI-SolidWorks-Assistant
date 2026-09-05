# PlanScript — plan format reference (v1)

`PlanScript` is the tiny, human-readable format the assistant uses to describe a
sequence of CAD operations. It is the **only** format allowed to reach the
SOLIDWORKS runtime — the AI layer may propose plans, but execution requires
validation **and** explicit user approval.

> `پارسر` فارسی و انگلیسی را می‌فهمد: اعداد فارسی (۱۰۲۴)، جداکننده‌ی اعشار `٫`،
> و واحد پس از عدد (`10 mm`) هم پذیرفته می‌شوند.

## Syntax

```text
# comment
plan: <title>

meta
<key> = <value>

operation <type>
<key> = <value>
```

- Comments: lines starting with `#`, `//` or `;`
- Markdown noise is ignored: `---`, ` ``` `, `>` quote lines
- Key/value separators: `=` or `:`
- Block types: `meta` and `operation <type>`
- Digits: ASCII, Persian (۰-۹) and Arabic-Indic (٠-٩)
- Booleans: `true/false`, `yes/no`, `1/0`, `بله/نه/خیر`

## Operations

### Phase 1 (executable today)

```text
operation sketch.rectangle
x1 = 0
y1 = 0
x2 = 100
y2 = 50
plane = Front Plane        # optional

operation sketch.circle
x = 50
y = 25
diameter_mm = 10

operation feature.extrude
depth_mm = 10

operation feature.cut
depth_mm = 5

operation feature.hole
x = 50
y = 25
diameter_mm = 10
through = true
plane = Front Plane        # optional; localized SOLIDWORKS installs may rename planes
```

Units: all lengths are **millimeters** in the plan; the runtime converts to
meters for the SOLIDWORKS API.

`feature.extrude` / `feature.cut` act on the currently active sketch — draw a
sketch first in the same plan, or have one open in SOLIDWORKS.

### Planned, execution in Phase 6 (rejected by the Phase 1 validator)

```text
operation feature.fillet    radius_mm = 3
operation feature.chamfer   distance_mm = 2
operation feature.pattern   ... (schema TBD in Phase 6)
operation feature.mirror    ... (schema TBD in Phase 6)
operation assembly.insert   ... (schema TBD in Phase 8)
operation assembly.mate     ... (schema TBD in Phase 8)
```

## Safety invariants (do not break)

1. The AI layer only ever *proposes* PlanScript text — it cannot run anything.
2. `PlanValidator` must reject: unknown types, non-numeric values, non-positive
   dimensions, values above the sanity bound, and Phase 6+ operations.
3. Nothing executes without an explicit user approval in the UI.
4. The macro generator (`SwMacroGenerator`) is the only component that turns an
   operation into SOLIDWORKS calls; it is deterministic and fully unit-tested.
