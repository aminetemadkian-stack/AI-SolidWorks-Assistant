# Plate 120 × 60 × 8 mm with two Ø8 mm through holes (20 mm from each short edge)

plan: Plate with two holes

meta
language = en
requires_confirmation = true

operation sketch.rectangle
x1 = 0
y1 = 0
x2 = 120
y2 = 60

operation feature.extrude
depth_mm = 8

operation feature.hole
x = 20
y = 30
diameter_mm = 8
through = true

operation feature.hole
x = 100
y = 30
diameter_mm = 8
through = true
