#!/usr/bin/env bash
# PredatorSense Toggle Script
# Launches PredatorSense if closed, or toggles/brings to focus if running.

APP_BIN="/opt/predatorsense/gui/PredatorSense"
FALLBACK_BIN="/usr/local/bin/predatorsense"
BIN=""

if [[ -x "$APP_BIN" ]]; then
    BIN="$APP_BIN"
elif [[ -x "$FALLBACK_BIN" ]]; then
    BIN="$FALLBACK_BIN"
elif command -v predatorsense >/dev/null 2>&1; then
    BIN="$(command -v predatorsense)"
fi

if [[ -z "$BIN" ]]; then
    notify-send "PredatorSense" "PredatorSense executable not found!" -u critical 2>/dev/null || true
    exit 1
fi

# 1. Hyprland toggle
if command -v hyprctl >/dev/null 2>&1 && [[ -n "${HYPRLAND_INSTANCE_SIGNATURE:-}" ]]; then
    if hyprctl clients -j | grep -qiE '"class":\s*"(PredatorSense)"'; then
        pkill -f "PredatorSense" || true
        exit 0
    fi
fi

# 2. Generic process check
if pgrep -fa "PredatorSense" >/dev/null 2>&1; then
    pkill -f "PredatorSense" || true
    exit 0
fi

# Launch PredatorSense in the background
nohup "$BIN" >/tmp/predatorsense.log 2>&1 &
