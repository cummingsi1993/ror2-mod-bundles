#!/bin/bash
# Submits the four item reference images to the local TRELLIS.2 container and copies
# the resulting GLBs into models/raw/. Requires:
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

for item in savings_bond accounts_receivable golden_parachute final_notice; do
  echo "=== $item: submitting $(date +%H:%M:%S) ==="
  resp=$(curl -sS -m 1800 -X POST http://localhost:3002/runsync \
    -H "Content-Type: application/json" \
    -d "{
      \"input\": {
        \"job_id\": \"$item\",
        \"input_image\": \"http://host.docker.internal:8765/${item}_ref.png\",
        \"seed\": 42,
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
