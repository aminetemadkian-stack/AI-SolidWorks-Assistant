# Demo bracket: 100 × 50 × 10 mm with a centered 10 mm through hole

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
