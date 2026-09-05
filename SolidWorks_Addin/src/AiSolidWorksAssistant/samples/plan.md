# Sample plan: demo bracket (Phase 1 echo engine)
#
# The Phase 1 echo engine proposes THIS plan for every request so the whole
# pipeline (context → parse → validate → confirm → execute) can be exercised
# without a real AI model. The user must still approve it in the chat pane.

plan: Demo bracket 100x50x10 with center hole

meta
language = en
requires_confirmation = true

operation sketch.rectangle
x1 = 0
y1 = 0
x2 = 100
y2 = 50

operation feature.extrude
depth_mm = 10

operation feature.hole
x = 50
y = 25
diameter_mm = 10
through = true
