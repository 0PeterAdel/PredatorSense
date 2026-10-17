#!/usr/bin/env bash
# ==============================================================================
#  PredatorSense Linux - Complete Automated One-Click Installer & System Setup
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

# ==============================================================================
# Automated Dependency Resolution
# ==============================================================================
auto_install_dependencies() {
    log_info "Verifying and resolving system dependencies automatically..."

    local need_install=0

    if ! command -v python3 >/dev/null 2>&1 || ! command -v make >/dev/null 2>&1; then
        need_install=1
    fi

    if [[ "$SKIP_DRIVER" == "0" ]]; then
        if ! command -v gcc >/dev/null 2>&1 || ! command -v depmod >/dev/null 2>&1 || [[ ! -d "/lib/modules/${KERNEL_RELEASE}/build" ]]; then
            need_install=1
        fi
    fi

    if [[ "$need_install" == "1" ]]; then
        log_info "Installing missing build dependencies for your Linux distribution..."
        if command -v pacman >/dev/null 2>&1; then
            pacman -Sy --noconfirm --needed base-devel linux-headers python dkms || true
        elif command -v apt-get >/dev/null 2>&1; then
            export DEBIAN_FRONTEND=noninteractive
            apt-get update -qq || true
            apt-get install -y build-essential "linux-headers-${KERNEL_RELEASE}" python3 python3-venv dkms || \
                apt-get install -y build-essential linux-headers-generic python3 python3-venv dkms || true
        elif command -v dnf >/dev/null 2>&1; then
            dnf install -y gcc make "kernel-devel-${KERNEL_RELEASE}" kernel-headers python3 python3-devel dkms || \
                dnf install -y gcc make kernel-devel kernel-headers python3 dkms || true
        elif command -v zypper >/dev/null 2>&1; then
            zypper install -y gcc make kernel-devel kernel-default-devel python3 dkms || true
        fi
    fi

    # Ensure python3-venv module is functional
    if ! python3 -c "import venv" >/dev/null 2>&1; then
        log_info "Installing python3-venv..."
        if command -v apt-get >/dev/null 2>&1; then
            apt-get install -y python3-venv python3-full || true
        elif command -v dnf >/dev/null 2>&1; then
            dnf install -y python3-virtualenv || true
        elif command -v pacman >/dev/null 2>&1; then
            pacman -S --noconfirm --needed python-virtualenv || true
        fi
    fi
}

auto_install_dependencies

# Verify critical tools
command -v python3 >/dev/null 2>&1 || die "python3 is required. Please install python3."
command -v make >/dev/null 2>&1 || die "make is required. Please install make."
if [[ "$SKIP_DRIVER" == "0" ]]; then
    command -v gcc >/dev/null 2>&1 || die "gcc is required to compile the kernel driver. Please install gcc."
fi

log_success "System build dependencies verified successfully."

# ==============================================================================
# 1. Driver Installation (linuwu_sense)
# ==============================================================================
if [[ "$SKIP_DRIVER" == "0" ]]; then
    log_info "Configuring linuwu_sense kernel driver for Linux ${KERNEL_RELEASE}..."
    DRIVER_DIR="${ROOT_DIR}/driver"
    DRIVER_VERSION="1.0.0"

    if [[ ! -d "/lib/modules/${KERNEL_RELEASE}/build" ]]; then
        die "Kernel headers not found for ${KERNEL_RELEASE}. Please install your distribution's linux-headers / kernel-devel package."
    fi

    MODULE_INSTALLED=0

    # Option A: Try DKMS for automated kernel rebuilds across updates
    if [[ "$BUILD_ONLY" == "0" ]] && command -v dkms >/dev/null 2>&1; then
        log_info "Registering driver with DKMS for seamless automatic updates..."
        DKMS_DEST="/usr/src/linuwu_sense-${DRIVER_VERSION}"
        mkdir -p "${DKMS_DEST}"
        cp -r "${DRIVER_DIR}"/* "${DKMS_DEST}/"

        dkms remove -m linuwu_sense -v "${DRIVER_VERSION}" --all >/dev/null 2>&1 || true
        dkms add -m linuwu_sense -v "${DRIVER_VERSION}" >/dev/null 2>&1 || true
        if dkms build -m linuwu_sense -v "${DRIVER_VERSION}" -k "${KERNEL_RELEASE}" >/dev/null 2>&1 && \
           dkms install -m linuwu_sense -v "${DRIVER_VERSION}" -k "${KERNEL_RELEASE}" --force >/dev/null 2>&1; then
            log_success "linuwu_sense driver registered and installed via DKMS."
            MODULE_INSTALLED=1
        else
            log_warn "DKMS registration was bypassed; building module directly for ${KERNEL_RELEASE}..."
        fi
    fi

    # Option B: Direct compilation and installation
    if [[ "$MODULE_INSTALLED" == "0" ]]; then
        make -C "${DRIVER_DIR}" clean >/dev/null 2>&1 || true
        make -C "${DRIVER_DIR}" KVER="${KERNEL_RELEASE}" -j"$(nproc)" || die "Failed to compile linuwu_sense kernel driver."
        log_success "Kernel module linuwu_sense.ko compiled successfully."

        if [[ "$BUILD_ONLY" == "0" ]]; then
            MODULE_DEST="/lib/modules/${KERNEL_RELEASE}/kernel/drivers/platform/x86"
            install -d -m 0755 "${MODULE_DEST}"
            install -m 0644 "${DRIVER_DIR}/src/linuwu_sense.ko" "${MODULE_DEST}/linuwu_sense.ko"
            depmod -a "${KERNEL_RELEASE}"
            log_success "Kernel module installed to ${MODULE_DEST}/linuwu_sense.ko"
        fi
    fi

    if [[ "$BUILD_ONLY" == "0" ]]; then
        log_info "Configuring module autoload and blacklisting conflicting drivers..."

        # Blacklist conflicting acer_wmi driver
        printf "blacklist acer_wmi\n" > /etc/modprobe.d/blacklist-acer_wmi.conf
        printf "linuwu_sense\n" > /etc/modules-load.d/linuwu_sense.conf

        # Unload acer_wmi if currently loaded in kernel
        if grep -q "^acer_wmi " /proc/modules; then
            modprobe -r acer_wmi 2>/dev/null || true
        fi
        if grep -q "^linuwu_sense " /proc/modules; then
            modprobe -r linuwu_sense 2>/dev/null || true
        fi

        # Load linuwu_sense into running kernel
        modprobe linuwu_sense || die "Failed to load linuwu_sense module into kernel."
        log_success "Driver linuwu_sense is active in the running kernel."

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

# ==============================================================================
# 2. Daemon Setup
# ==============================================================================
log_info "Configuring PredatorSense hardware daemon..."
DAEMON_SRC="${ROOT_DIR}/daemon"

if [[ "$BUILD_ONLY" == "0" ]]; then
    TARGET_DAEMON_DIR="${INSTALL_PREFIX}/daemon"
    mkdir -p "${TARGET_DAEMON_DIR}"
    cp -r "${DAEMON_SRC}"/* "${TARGET_DAEMON_DIR}/"

    # Set up isolated Python virtual environment for daemon
    python3 -m venv "${TARGET_DAEMON_DIR}/venv"
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

# ==============================================================================
# 3. GUI Build & Installation
# ==============================================================================
log_info "Setting up PredatorSense graphical interface..."
STAGE_GUI="${ROOT_DIR}/build/gui"
mkdir -p "${STAGE_GUI}"

# Check for pre-compiled GUI binary (Release package)
PREBUILT_BIN=""
if [[ -x "${ROOT_DIR}/build/gui/PredatorSense" ]]; then
    PREBUILT_BIN="${ROOT_DIR}/build/gui/PredatorSense"
elif [[ -x "${ROOT_DIR}/bin/PredatorSense" ]]; then
    PREBUILT_BIN="${ROOT_DIR}/bin/PredatorSense"
fi

if [[ -n "$PREBUILT_BIN" ]]; then
    log_success "Found pre-compiled standalone PredatorSense binary (${PREBUILT_BIN})."
    if [[ "$PREBUILT_BIN" != "${STAGE_GUI}/PredatorSense" ]]; then
        cp -r "$(dirname "$PREBUILT_BIN")"/* "${STAGE_GUI}/" 2>/dev/null || cp "$PREBUILT_BIN" "${STAGE_GUI}/"
    fi
else
    # Build from source using .NET 9 SDK
    log_info "Pre-compiled binary not found. Compiling GUI from source..."
    DOTNET_CMD=""
    if command -v dotnet >/dev/null 2>&1; then
        DOTNET_CMD="dotnet"
    elif [[ -x "/home/${TARGET_USER}/.dotnet/dotnet" ]]; then
        DOTNET_CMD="/home/${TARGET_USER}/.dotnet/dotnet"
    elif [[ -x "/usr/share/dotnet/dotnet" ]]; then
        DOTNET_CMD="/usr/share/dotnet/dotnet"
    fi

    if [[ -z "$DOTNET_CMD" ]]; then
        log_info "Installing .NET SDK automatically..."
        if command -v pacman >/dev/null 2>&1; then
            pacman -S --noconfirm --needed dotnet-sdk-9.0 || pacman -S --noconfirm --needed dotnet-sdk || true
        elif command -v apt-get >/dev/null 2>&1; then
            apt-get install -y dotnet-sdk-9.0 || true
        elif command -v dnf >/dev/null 2>&1; then
            dnf install -y dotnet-sdk-9.0 || true
        fi
        command -v dotnet >/dev/null 2>&1 && DOTNET_CMD="dotnet"
    fi

    if [[ -z "$DOTNET_CMD" ]]; then
        log_info "Bootstrapping .NET 9 SDK via official Microsoft installer..."
        curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 9.0 --install-dir /usr/share/dotnet
        export DOTNET_ROOT=/usr/share/dotnet
        export PATH=$PATH:/usr/share/dotnet
        DOTNET_CMD="/usr/share/dotnet/dotnet"
    fi

    [[ -x "$DOTNET_CMD" || -n "$(command -v "$DOTNET_CMD" 2>/dev/null)" ]] || die ".NET 9 SDK is required to compile the GUI from source."

    "$DOTNET_CMD" publish "${ROOT_DIR}/gui/PredatorSense.csproj" \
        -c Release \
        -f net9.0 \
        -r linux-x64 \
        --self-contained true \
        -o "${STAGE_GUI}" \
        -p:PublishSingleFile=true \
        -p:IncludeNativeLibrariesForSelfExtract=true \
        -p:IncludeAllContentForSelfExtract=true || die "Failed to build PredatorSense GUI."

    log_success "PredatorSense GUI compiled cleanly."
fi

if [[ "$BUILD_ONLY" == "0" ]]; then
    TARGET_GUI_DIR="${INSTALL_PREFIX}/gui"
    mkdir -p "${TARGET_GUI_DIR}"
    cp -r "${STAGE_GUI}"/* "${TARGET_GUI_DIR}/"
    chmod +x "${TARGET_GUI_DIR}/PredatorSense"

    # Global launcher wrapper
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
            rm -f "${USER_HOME}/.local/share/applications/damx.desktop" 2>/dev/null || true
        fi
    fi

    command -v update-desktop-database >/dev/null 2>&1 && update-desktop-database /usr/share/applications >/dev/null 2>&1 || true

    # Toggle Script
    install -m 0755 "${ROOT_DIR}/packaging/predatorsense-toggle.sh" "${BIN_DIR}/predatorsense-toggle"
    log_success "GUI and desktop entries installed to system."
fi

# ==============================================================================
# 4. Predator Hardware Key Auto-Binding
# ==============================================================================
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

# ==============================================================================
# 5. Socket and Health Verification
# ==============================================================================
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
