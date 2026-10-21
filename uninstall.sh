#!/usr/bin/env bash
# ==============================================================================
#  PredatorSense Linux - Clean Uninstaller
# ==============================================================================

set -Eeuo pipefail

RED='\033[0;31m'
GREEN='\033[0;32m'
CYAN='\033[0;36m'
YELLOW='\033[1;33m'
BOLD='\033[1m'
NC='\033[0m'

if [[ $EUID -ne 0 ]]; then
    printf "${RED}[✗] Please run as root: sudo ./uninstall.sh${NC}\n" >&2
    exit 1
fi

printf "${CYAN}[i] Stopping and disabling PredatorSense services...${NC}\n"
systemctl stop predatorsense-daemon.service 2>/dev/null || true
systemctl disable predatorsense-daemon.service 2>/dev/null || true
systemctl stop damx-daemon.service 2>/dev/null || true
systemctl disable damx-daemon.service 2>/dev/null || true
systemctl stop linuwu_sense.service 2>/dev/null || true
systemctl disable linuwu_sense.service 2>/dev/null || true

printf "${CYAN}[i] Removing systemd units and tmpfiles...${NC}\n"
rm -f /etc/systemd/system/predatorsense-daemon.service
rm -f /etc/systemd/system/linuwu_sense.service
rm -f /etc/tmpfiles.d/predatorsense.conf
rm -f /etc/udev/rules.d/99-predatorsense.rules
systemctl daemon-reload

printf "${CYAN}[i] Restoring default kernel drivers...${NC}\n"
rm -f /etc/modprobe.d/blacklist-acer_wmi.conf
rm -f /etc/modules-load.d/linuwu_sense.conf
modprobe -r linuwu_sense 2>/dev/null || true
modprobe acer_wmi 2>/dev/null || true

# Remove driver ko
KERNEL_RELEASE="$(uname -r)"
rm -f "/lib/modules/${KERNEL_RELEASE}/kernel/drivers/platform/x86/linuwu_sense.ko"
depmod -a "${KERNEL_RELEASE}" 2>/dev/null || true

printf "${CYAN}[i] Removing binaries and desktop launchers...${NC}\n"
rm -rf /opt/predatorsense
rm -f /usr/local/bin/predatorsense
rm -f /usr/local/bin/predatorsense-toggle
rm -f /usr/share/applications/predatorsense.desktop
rm -f /usr/share/icons/hicolor/256x256/apps/predatorsense.png
rm -f /var/run/predatorsense.sock
rm -f /var/run/predatorsense-daemon.pid

# Update desktop cache
command -v update-desktop-database >/dev/null 2>&1 && update-desktop-database /usr/share/applications >/dev/null 2>&1 || true

printf "${GREEN}${BOLD}[✓] PredatorSense has been completely uninstalled from your system.${NC}\n"
