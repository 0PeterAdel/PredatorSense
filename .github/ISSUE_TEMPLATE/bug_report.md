---
name: "🐛 Bug Report"
about: "Report an issue or unexpected behavior in PredatorSense for Linux"
title: "[Bug]: "
labels: ["bug", "triage"]
assignees: []
---

## 📌 Description
A clear and concise description of the bug.

## 💻 System Information
- **Laptop Model:** (e.g. Acer Predator Helios 16 PH16-71 / Nitro 16 AN16-41)
- **Linux Distribution:** (e.g. Arch Linux, Fedora 40, Ubuntu 24.04)
- **Linux Kernel:** `uname -r`
- **Desktop Environment / Window Manager:** (e.g. Hyprland, GNOME 46, KDE Plasma 6, Sway)
- **Display Server:** Wayland / X11
- **Driver Module Status:** `lsmod | grep linuwu_sense`

## 🔁 Steps to Reproduce
1. Launch PredatorSense via `...`
2. Go to tab `...`
3. Click on `...`
4. Notice error `...`

## 🎯 Expected Behavior
A clear and concise description of what you expected to happen.

## ⚠️ Actual Behavior
What actually happened (e.g., error crash, lighting not applying, fan speed unresponsive).

## 📋 Daemon & System Logs
Please provide relevant output from the daemon service:
```bash
systemctl status predatorsense-daemon.service
journalctl -u predatorsense-daemon.service -n 50 --no-pager
```

## 📸 Screenshots / Recordings
If applicable, add screenshots or screen recordings to help explain your problem.

## ℹ️ Additional Context
Add any other context about the problem here.
