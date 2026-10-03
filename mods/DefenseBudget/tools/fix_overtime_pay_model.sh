#!/bin/bash
# Post-process the TRELLIS Overtime Pay heart (run after generate_models.sh overtime_pay
# and blender_clean_export.py -- overtime_pay). Seed 42 on make_overtime_pay_ref.ps1's
# image gives the best $ emblem but bakes an orange smudge (from the gloss highlight)
# and grey sawtooth patches along the side band. This:
#  1. dumps each face's UVs + orientation vs the heart's depth axis (Blender),
#  2. repaints the texture with geometry masks: side band -> one flat magenta (gold
#     emblem walls kept), salmon smudges on the faces -> surrounding pink (system python + PIL),
#  3. fills the open boundary loops in the side wall and maps the new faces to that magenta.
# The remaining edge zigzag is folded geometry; fixing it needs a remesh + re-bake.
set -eu
BLENDER="/c/Program Files/Blender Foundation/Blender 5.1/blender.exe"
TOOLS="$(cd "$(dirname "$0")" && pwd)"
MODELS="$TOOLS/../DefenseBudget/models"
TMP="$(mktemp -d)"
"$BLENDER" --background --python "$TOOLS/blender_dump_uv_faces.py" -- "$MODELS/overtime_pay.obj" "$TMP/uv_faces.json"
python "$TOOLS/fix_overtime_pay_texture.py" "$TMP/uv_faces.json" "$MODELS/overtime_pay.rgba" "$TMP/fixed.rgba" "$TMP/debug_mask.png"
"$BLENDER" --background --python "$TOOLS/blender_fill_holes.py" -- "$MODELS/overtime_pay.obj" "$TMP/fixed.rgba" "$TMP/fixed.obj"
cp "$TMP/fixed.rgba" "$MODELS/overtime_pay.rgba"
cp "$TMP/fixed.obj" "$MODELS/overtime_pay.obj"
echo "fixed model written to $MODELS (debug mask: $TMP/debug_mask.png)"
