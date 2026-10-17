## What changed?
<!-- Explain what you changed clearly and concisely. -->

## Why?
<!-- Explain why this change was needed. Link related issues with 'Fixes #issue' if applicable. -->

## How to review?
<!-- Explain how the reviewer should test and check your work. -->

## Notes
<!-- Add any important notes, limitations, follow-up work, or hardware specifics. -->

---

## 🏷️ Type of Change
- [ ] `feat:` New feature or capability
- [ ] `fix:` Bug fix
- [ ] `docs:` Documentation changes
- [ ] `style:` Formatting or cosmetic changes
- [ ] `refactor:` Code restructuring without behavior changes
- [ ] `perf:` Performance optimization
- [ ] `test:` Adding or updating tests
- [ ] `chore:` Maintenance tasks
- [ ] `ci:` CI/CD pipeline changes
- [ ] `build:` Build system or dependencies
- [ ] `security:` Security vulnerability patch

---

## 💻 Hardware Tested On
- **Laptop Model:** (e.g. Acer Predator Helios 16 PH16-71 / Nitro 16 AN16-41)
- **Linux Distribution & Kernel:** (e.g. Arch Linux, Kernel 6.10.x)
- **Desktop Environment:** (e.g. Hyprland / GNOME / KDE Plasma)

---

## ✅ ShiftCore Engineering Checklist
- [ ] **Branch Naming:** Branch follows `type/short-task-name` (e.g. `feat/concentric-zoom-effect`, `fix/daemon-socket-retry`).
- [ ] **Commit Messages:** Commits follow `type(scope): short message` in lowercase.
- [ ] **Focused Scope:** PR represents one logical task and changes only relevant files.
- [ ] **Local Build:** Project builds cleanly without errors or warnings (`dotnet build gui/PredatorSense.csproj -c Release`).
- [ ] **No Secrets:** Verified that no tokens, passwords, `.env`, or sensitive logs are included.
- [ ] **Attribution:** Verified that author attribution headers and licenses are preserved.
