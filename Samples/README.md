# Sample plans

Ready-to-load `PlanScript` files for testing the Phase 1 pipeline.

## How to use them

Phase 1 proposes the embedded sample plan automatically for any chat input.
These extra files are for experimenting with the format and for the Phase 2+
parser test harness (they will be loadable from the chat pane as
"plan files" once the file-drop flow lands).

To try a custom plan today: replace the contents of
`SolidWorks_Addin/src/AiSolidWorksAssistant/samples/plan.md` and rebuild, or
feed the text to `PlanParser.Parse(...)` in a unit test.

## Files

- [`bracket.plan.md`](bracket.plan.md) — the demo bracket (same as the embedded sample)
- [`plate-with-two-holes.plan.md`](plate-with-two-holes.plan.md) — rectangle + extrude + two through holes
