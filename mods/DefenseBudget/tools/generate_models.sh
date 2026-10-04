#!/bin/bash
# Submits item reference images (models/refs/<item>_ref.png) to the local TRELLIS.2
# container and copies the resulting GLBs into models/raw/. Usage:
#   [SEED=n] generate_models.sh [item ...]      (default: every current pack item, seed 42)
# The texture is baked on the final 8000-face topology (decimation_target), so
# blender_clean_export.py's decimation passes are no-ops and UVs stay sharp. Requires:
#  - tostui-trellis2 container running (port 3002)
#  - http server on 8765 serving models/refs (started separately)
set -u
RAW=/d/source/DefenseBudget/mods/DefenseBudget/models/raw
OUT=/d/tools/trellis-output

echo "waiting for TRELLIS.2 API..."
for i in $(seq 1 60); do
  if curl -s -o /dev/null -m 2 http://localhost:3002/; then break; fi
  sleep 10
done
echo "API reachable, submitting jobs"

ITEMS=("$@")
[ ${#ITEMS[@]} -eq 0 ] && ITEMS=(savings_bond accounts_receivable golden_parachute overtime_pay)

for item in "${ITEMS[@]}"; do
  echo "=== $item: submitting $(date +%H:%M:%S) ==="
  resp=$(curl -sS -m 1800 -X POST http://localhost:3002/runsync \
    -H "Content-Type: application/json" \
    -d "{
      \"input\": {
        \"job_id\": \"$item\",
        \"input_image\": \"http://host.docker.internal:8765/${item}_ref.png\",
        \"seed\": ${SEED:-42},
        \"resolution\": \"1024\",
        \"ss_guidance_strength\": 7.5,
        \"ss_guidance_rescale\": 0.7,
        \"ss_sampling_steps\": 12,
        \"ss_rescale_t\": 5.0,
        \"shape_slat_guidance_strength\": 7.5,
        \"shape_slat_guidance_rescale\": 0.5,
        \"shape_slat_sampling_steps\": 12,
        \"shape_slat_rescale_t\": 3.0,
        \"tex_slat_guidance_strength\": 1.0,
        \"tex_slat_guidance_rescale\": 0.0,
        \"tex_slat_sampling_steps\": 12,
        \"tex_slat_rescale_t\": 3.0,
        \"decimation_target\": 8000,
        \"texture_size\": 1024,
        \"remove_bg\": true
      }
    }")
  echo "$resp" > "$RAW/${item}_response.json"
  result=$(echo "$resp" | python -c "import sys,json
try:
    print(json.load(sys.stdin)['output']['result'])
except Exception as e:
    print('PARSE_ERROR', e)")
  if [[ "$result" == PARSE_ERROR* || -z "$result" ]]; then
    echo "FAILED $item: $resp" | head -c 800
    echo
    continue
  fi
  fname=$(basename "$result")
  cp "$OUT/$fname" "$RAW/${item}_raw.glb" && echo "saved $RAW/${item}_raw.glb"
done
echo "ALL DONE $(date +%H:%M:%S)"
