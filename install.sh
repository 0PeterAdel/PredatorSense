#!/usr/bin/env bash
# ==============================================================================
#  PredatorSense Linux - One-Click Installer & System Setup
#  Official-grade control center for Acer Predator & Nitro gaming laptops
# ==============================================================================

set -Eeuo pipefail

RED='\033[0;31m'
GREEN='\033[0;32m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
YELLOW='\033[1;33m'
BOLD='\033[1m'
NC='\033[0m'

ROOT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
INSTALL_PREFIX="/opt/predatorsense"
BIN_DIR="/usr/local/bin"
KERNEL_RELEASE="$(uname -r)"
TARGET_USER="${SUDO_USER:-${USER}}"
BUILD_ONLY=0
SKIP_DRIVER=0

log_info() {
    printf "${CYAN}[i] %s${NC}\n" "$*"
}

log_success() {
    printf "${GREEN}[✓] %s${NC}\n" "$*"
}

log_warn() {
    printf "${YELLOW}[!] %s${NC}\n" "$*"
}

log_error() {
    printf "${RED}[✗] %s${NC}\n" "$*" >&2
}

die() {
    log_error "$*"
    exit 1
}

# Banner
print_banner() {
    cat << "EOF"
  ____                _       _               ____                      
 |  _ \ _ __ ___   __| | __ _| |_ ___  _ __  / ___|  ___ _ __  ___  ___ 
 | |_) | '__/ _ \ / _` |/ _` | __/ _ \| '__| \___ \ / _ \ '_ \/ __|/ _ \
 |  __/| | |  __/| (_| | (_| | || (_) | |     ___) |  __/ | | \__ \  __/
 |_|   |_|  \___| \__,_|\__,_|\__\___/|_|    |____/ \___|_| |_|___/\___|
                                                                        
    Acer Predator Helios & Nitro Linux Gaming Control Center
==============================================================================
EOF
}

usage() {
    cat << EOF
Usage: sudo ./install.sh [options]

Options:
    --build-only    Compile driver, daemon and GUI without installing to system
    --no-driver     Skip compiling and installing the linuwu_sense kernel module
    -h, --help      Display this help message and exit
EOF
}

# Parse arguments
while [[ $# -gt 0 ]]; do
    case "$1" in
        --build-only)
            BUILD_ONLY=1
            shift
            ;;
        --no-driver)
            SKIP_DRIVER=1
            shift
            ;;
        -h|--help)
            usage
            exit 0
            ;;
        *)
            log_error "Unknown option: $1"
            usage
            exit 1
            ;;
    esac
done

print_banner

# Require root unless build-only
if [[ "$BUILD_ONLY" == "0" && $EUID -ne 0 ]]; then
    die "This installer must be run with root privileges (sudo ./install.sh)"
fi

# Pre-flight dependency check
log_info "Checking system build dependencies..."
REQUIRED_TOOLS=(python3 make)
if [[ "$SKIP_DRIVER" == "0" ]]; then
    REQUIRED_TOOLS+=(gcc modprobe depmod)
fi

for tool in "${REQUIRED_TOOLS[@]}"; do
    command -v "$tool" >/dev/null 2>&1 || die "Missing required dependency: $tool"
done

# Check dotnet
DOTNET_CMD=""
if command -v dotnet >/dev/null 2>&1; then
    DOTNET_CMD="dotnet"
elif [[ -x "/home/${TARGET_USER}/.dotnet/dotnet" ]]; then
    DOTNET_CMD="/home/${TARGET_USER}/.dotnet/dotnet"
elif [[ -x "/usr/share/dotnet/dotnet" ]]; then
    DOTNET_CMD="/usr/share/dotnet/dotnet"
fi

[[ -n "$DOTNET_CMD" ]] || die ".NET 9 SDK/Runtime (dotnet) is required to build the GUI. Please install dotnet-sdk-9.0."
log_success "Build dependencies verified successfully."

# 1. Driver Installation
if [[ "$SKIP_DRIVER" == "0" ]]; then
    log_info "Building linuwu_sense kernel driver for Linux ${KERNEL_RELEASE}..."
    DRIVER_DIR="${ROOT_DIR}/driver"
    
    if [[ ! -d "/lib/modules/${KERNEL_RELEASE}/build" ]]; then
        die "Kernel headers not found for ${KERNEL_RELEASE}. Please install linux-headers or kernel-devel."
    fi

    make -C "${DRIVER_DIR}" clean >/dev/null 2>&1 || true
    make -C "${DRIVER_DIR}" KVER="${KERNEL_RELEASE}" -j"$(nproc)" || die "Failed to compile linuwu_sense kernel driver."
    log_success "Kernel module linuwu_sense.ko compiled successfully."

    if [[ "$BUILD_ONLY" == "0" ]]; then
        log_info "Installing kernel module and configuring blacklists..."
        MODULE_DEST="/lib/modules/${KERNEL_RELEASE}/kernel/drivers/platform/x86"
        install -d -m 0755 "${MODULE_DEST}"
        install -m 0644 "${DRIVER_DIR}/src/linuwu_sense.ko" "${MODULE_DEST}/linuwu_sense.ko"

        # Blacklist conflicting acer_wmi
        printf "blacklist acer_wmi\n" > /etc/modprobe.d/blacklist-acer_wmi.conf
        printf "linuwu_sense\n" > /etc/modules-load.d/linuwu_sense.conf

        depmod -a "${KERNEL_RELEASE}"

        # Unload acer_wmi if loaded, load linuwu_sense
        if grep -q "^acer_wmi " /proc/modules; then
            modprobe -r acer_wmi 2>/dev/null || true
        fi
        if grep -q "^linuwu_sense " /proc/modules; then
            modprobe -r linuwu_sense 2>/dev/null || true
        fi
        modprobe linuwu_sense || die "Failed to load linuwu_sense module."
        log_success "Driver linuwu_sense is active in kernel."

        # Setup group permissions
        if ! getent group linuwu_sense >/dev/null; then
            groupadd linuwu_sense
        fi
        if [[ -n "$TARGET_USER" && "$TARGET_USER" != "root" ]]; then
            usermod -aG linuwu_sense "$TARGET_USER" || true
        fi

        # Install tmpfiles and udev rules
        install -m 0644 "${ROOT_DIR}/packaging/predatorsense.tmpfiles" /etc/tmpfiles.d/predatorsense.conf
        install -m 0644 "${ROOT_DIR}/packaging/99-predatorsense.rules" /etc/udev/rules.d/99-predatorsense.rules
        systemd-tmpfiles --create /etc/tmpfiles.d/predatorsense.conf >/dev/null 2>&1 || true
        udevadm control --reload >/dev/null 2>&1 || true
    fi
fi

# 2. Daemon Setup
log_info "Configuring PredatorSense hardware daemon..."
DAEMON_SRC="${ROOT_DIR}/daemon"
STAGE_DAEMON="${ROOT_DIR}/build/daemon"
mkdir -p "${STAGE_DAEMON}"

# Prepare python venv for daemon
if [[ "$BUILD_ONLY" == "0" ]]; then
    TARGET_DAEMON_DIR="${INSTALL_PREFIX}/daemon"
    mkdir -p "${TARGET_DAEMON_DIR}"
    cp -r "${DAEMON_SRC}"/* "${TARGET_DAEMON_DIR}/"

    python3 -m venv "${TARGET_DAEMON_DIR}/venv"
    # Create daemon executable wrapper
    cat > "${TARGET_DAEMON_DIR}/predatorsense-daemon" << EOF
#!/usr/bin/env bash
exec "${TARGET_DAEMON_DIR}/venv/bin/python" "${TARGET_DAEMON_DIR}/predatorsense_daemon.py" "\$@"
EOF
    chmod +x "${TARGET_DAEMON_DIR}/predatorsense-daemon"

    # Install systemd service
    install -m 0644 "${ROOT_DIR}/packaging/predatorsense-daemon.service" /etc/systemd/system/predatorsense-daemon.service
    systemctl daemon-reload
    systemctl enable predatorsense-daemon.service >/dev/null 2>&1 || true
    systemctl restart predatorsense-daemon.service || log_warn "Daemon service started with warnings (check journalctl -u predatorsense-daemon)"
    log_success "PredatorSense daemon installed and active."
fi

# 3. GUI Build & Installation
log_info "Publishing PredatorSense graphical interface..."
GUI_SRC="${ROOT_DIR}/gui"
STAGE_GUI="${ROOT_DIR}/build/gui"
mkdir -p "${STAGE_GUI}"

"$DOTNET_CMD" publish "${GUI_SRC}/PredatorSense.csproj" \
    -c Release \
    -f net9.0 \
    -r linux-x64 \
    --self-contained true \
    -o "${STAGE_GUI}" \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:IncludeAllContentForSelfExtract=true || die "Failed to build PredatorSense GUI."

log_success "PredatorSense GUI compiled cleanly."

if [[ "$BUILD_ONLY" == "0" ]]; then
    TARGET_GUI_DIR="${INSTALL_PREFIX}/gui"
    mkdir -p "${TARGET_GUI_DIR}"
    cp -r "${STAGE_GUI}"/* "${TARGET_GUI_DIR}/"
    chmod +x "${TARGET_GUI_DIR}/PredatorSense"

    # Launcher wrapper
    cat > "${BIN_DIR}/predatorsense" << EOF
#!/usr/bin/env bash
exec "${TARGET_GUI_DIR}/PredatorSense" "\$@"
EOF
    chmod +x "${BIN_DIR}/predatorsense"

    # Desktop shortcut and icons
    install -d -m 0755 /usr/share/icons/hicolor/256x256/apps
    install -m 0644 "${ROOT_DIR}/packaging/predatorsense.png" /usr/share/icons/hicolor/256x256/apps/predatorsense.png
    install -m 0644 "${ROOT_DIR}/packaging/predatorsense.desktop" /usr/share/applications/predatorsense.desktop

    # Install user desktop shortcut as well if user home exists
    if [[ -n "$TARGET_USER" && "$TARGET_USER" != "root" ]]; then
        USER_HOME="$(getent passwd "$TARGET_USER" | cut -d: -f6)"
        if [[ -d "$USER_HOME" ]]; then
            install -d -m 0755 "${USER_HOME}/.local/share/applications"
            install -d -m 0755 "${USER_HOME}/.local/share/icons/hicolor/256x256/apps"
            install -m 0644 "${ROOT_DIR}/packaging/predatorsense.png" "${USER_HOME}/.local/share/icons/hicolor/256x256/apps/predatorsense.png"
            install -m 0644 "${ROOT_DIR}/packaging/predatorsense.desktop" "${USER_HOME}/.local/share/applications/predatorsense.desktop"
            chown -R "${TARGET_USER}:${TARGET_USER}" "${USER_HOME}/.local/share/applications/predatorsense.desktop" "${USER_HOME}/.local/share/icons/hicolor/256x256/apps/predatorsense.png" || true
        fi
    fi

    command -v update-desktop-database >/dev/null 2>&1 && update-desktop-database /usr/share/applications >/dev/null 2>&1 || true

    # Toggle Script
    install -m 0755 "${ROOT_DIR}/packaging/predatorsense-toggle.sh" "${BIN_DIR}/predatorsense-toggle"
    log_success "GUI and desktop entries installed to system."
fi

# 4. Predator Hardware Key Auto-Binding
if [[ "$BUILD_ONLY" == "0" ]]; then
    log_info "Configuring hardware Predator key shortcut..."
    USER_HOME="$(getent passwd "${TARGET_USER:-root}" | cut -d: -f6)"

    # Hyprland setup
    if [[ -d "${USER_HOME}/.config/hypr" ]]; then
        BINDINGS_LUA="${USER_HOME}/.config/hypr/bindings.lua"
        HYPR_CONF="${USER_HOME}/.config/hypr/hyprland.conf"
        
        if [[ -f "$BINDINGS_LUA" ]]; then
            if grep -q "XF86Launch7" "$BINDINGS_LUA"; then
                sed -i 's|.*XF86Launch7.*|o.bind("XF86Launch7", "PredatorSense", "/usr/local/bin/predatorsense-toggle")|g' "$BINDINGS_LUA"
                log_success "Configured Predator key in Hyprland (${BINDINGS_LUA})."
            else
                printf '\no.bind("XF86Launch7", "PredatorSense", "/usr/local/bin/predatorsense-toggle")\n' >> "$BINDINGS_LUA"
                log_success "Added Predator key binding to Hyprland (${BINDINGS_LUA})."
            fi
        elif [[ -f "$HYPR_CONF" ]]; then
            if ! grep -q "predatorsense-toggle" "$HYPR_CONF"; then
                printf '\nbind = , XF86Launch7, exec, /usr/local/bin/predatorsense-toggle\n' >> "$HYPR_CONF"
                log_success "Added Predator key binding to Hyprland (${HYPR_CONF})."
            fi
        fi
        chown -R "${TARGET_USER}:${TARGET_USER}" "${USER_HOME}/.config/hypr" 2>/dev/null || true
    fi

    # GNOME custom shortcut setup (if gnome-shell detected)
    if command -v gsettings >/dev/null 2>&1 && [[ -n "${TARGET_USER}" ]]; then
        sudo -u "$TARGET_USER" dbus-launch gsettings set org.gnome.settings-daemon.plugins.media-keys.custom-keybinding:/org/gnome/settings-daemon/plugins/media-keys/custom-keybindings/predatorsense/ name "PredatorSense" 2>/dev/null || true
        sudo -u "$TARGET_USER" dbus-launch gsettings set org.gnome.settings-daemon.plugins.media-keys.custom-keybinding:/org/gnome/settings-daemon/plugins/media-keys/custom-keybindings/predatorsense/ command "/usr/local/bin/predatorsense-toggle" 2>/dev/null || true
        sudo -u "$TARGET_USER" dbus-launch gsettings set org.gnome.settings-daemon.plugins.media-keys.custom-keybinding:/org/gnome/settings-daemon/plugins/media-keys/custom-keybindings/predatorsense/ binding "XF86Launch7" 2>/dev/null || true
    fi

    # User local toggle script symlink for convenience
    if [[ -d "${USER_HOME}/.local/bin" ]]; then
        ln -sf "${BIN_DIR}/predatorsense-toggle" "${USER_HOME}/.local/bin/predatorsense-toggle"
        chown -h "${TARGET_USER}:${TARGET_USER}" "${USER_HOME}/.local/bin/predatorsense-toggle" || true
    fi
    log_success "Predator key mapped to /usr/local/bin/predatorsense-toggle."
fi

# 5. Socket and Health Verification
if [[ "$BUILD_ONLY" == "0" ]]; then
    log_info "Verifying daemon communication..."
    for _ in {1..20}; do
        [[ -S /var/run/predatorsense.sock ]] && break
        sleep 0.25
    done

    python3 - << 'PY'
import json
import socket
import sys

sock_path = "/var/run/predatorsense.sock"
try:
    s = socket.socket(socket.AF_UNIX, socket.SOCK_STREAM)
    s.settimeout(3)
    s.connect(sock_path)
    s.sendall(json.dumps({"command": "get_supported_features", "params": {}}).encode("utf-8"))
    res = json.loads(s.recv(65536).decode("utf-8"))
    s.close()
    if res.get("success"):
        features = res.get("data", {}).get("available_features", [])
        print(f"\033[0;32m[✓] Daemon online! Supported hardware features ({len(features)}): {', '.join(features)}\033[0m")
    else:
        print("\033[1;33m[!] Daemon responded with non-success:\033[0m", res)
except Exception as e:
    print(f"\033[1;33m[!] Could not connect to daemon socket ({e}). Daemon is restarting.\033[0m")
PY
fi

# Finished
echo ""
echo "=============================================================================="
printf "${GREEN}${BOLD}   PredatorSense Linux installation completed successfully!${NC}\n"
echo "=============================================================================="
printf "  • GUI executable:      ${CYAN}%s${NC}\n" "${BIN_DIR}/predatorsense"
printf "  • Predator Key Toggle: ${CYAN}%s${NC}\n" "${BIN_DIR}/predatorsense-toggle"
printf "  • Systemd Service:     ${CYAN}systemctl status predatorsense-daemon.service${NC}\n"
printf "  • Desktop Launcher:    Available in your application menu as ${BOLD}Predator Sense${NC}\n"
echo "=============================================================================="
EOF
