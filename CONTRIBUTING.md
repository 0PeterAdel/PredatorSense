# Contributing to PredatorSense for Linux 🎮

Thank you for your interest in contributing to **PredatorSense for Linux**! Community contributions—whether they are bug fixes, support for new Acer laptop models, feature enhancements, or documentation improvements—are warmly welcomed and deeply appreciated.

To maintain a professional, clean, and reliable codebase, all contributors and maintainers follow the engineering standards defined in the [ShiftCore Team Handbook](https://platform.shiftcore.workers.dev/docs/handbook).

---

## 📜 Open Source & Attribution Terms

PredatorSense for Linux is free open-source software licensed under the **GNU General Public License v3.0 (GPLv3)** with an **Author Attribution Requirement** (pursuant to GPLv3 Section 7(b)):

> **You are completely free to use, modify, fork, and distribute this software, provided that you clearly and prominently attribute the original author (Peter Adel / 0PeterAdel) and link back to the official repository:**  
> **https://github.com/0PeterAdel/PredatorSense**

By submitting a Pull Request, you agree that your code will be released under these exact terms.

---

## 📋 Core Git Rules

We strictly adhere to the [ShiftCore Git Workflow](https://platform.shiftcore.workers.dev/docs/handbook/docs/git/git-workflow):
1. **Never work directly on `main`** — `main` is protected and always stable.
2. **Every task must have its own branch** following the format `type/short-task-name`.
3. **No direct pushes to `main`** — all changes must go through a Pull Request.
4. **Clean commit messages** following `type(scope): short message`.
5. **No secret or temporary files** — never commit `.env`, credentials, build caches, or logs.
6. **PR review & verification** — merge only after review approval and passing CI checks.

```text
Branch → Commit → Push → Pull Request → Review → Approval → Merge
```

---

## 🌿 Branch Naming Rules

Follow the [ShiftCore Branch & Commit Rules](https://platform.shiftcore.workers.dev/docs/handbook/docs/git/branch-and-commit-rules):

Format:
```text
type/short-task-name
```

### Allowed Branch Types:
- `feat/` — New feature or hardware support (e.g., `feat/triton-fan-curve`, `feat/keyboard-audio-visualizer`)
- `fix/` — Bug fix (e.g., `fix/daemon-socket-retry`, `fix/readme-typo`)
- `docs/` — Documentation updates (e.g., `docs/align-handbook-rules`, `docs/setup-guide`)
- `style/` — Formatting or styling without logic changes (e.g., `style/format-dashboard-xaml`)
- `refactor/` — Code improvement without behavior change (e.g., `refactor/effects-engine`)
- `test/` — Adding or improving tests (e.g., `test/socket-client-unit-tests`)
- `chore/` — Build or maintenance tasks (e.g., `chore/bump-nuget-packages`)
- `perf/` — Performance optimization (e.g., `perf/dynamic-rgb-cpu-usage`)
- `ci/` — CI/CD automation changes (e.g., `ci/add-multiarch-build`)
- `build/` — Build system or dependency updates (e.g., `build/net9-upgrade`)
- `security/` — Security patches (e.g., `security/sanitize-sysfs-input`)

*Rules: Use lowercase, hyphens between words, keep it short and descriptive. Avoid vague names like `test`, `update`, `final`, or `my-work`.*

---

## 💬 Commit Message Format

Commit messages must follow Conventional Commits:

```text
type(scope): short message
```
*(Scope is optional but recommended when affecting a specific module).*

### Allowed Types:
`feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `chore`, `ci`, `build`, `security`, `revert`.

### Common Scopes:
`gui`, `driver`, `daemon`, `rgb`, `telemetry`, `installer`, `packaging`, `docs`, `ci`, `security`.

### Examples:
- `feat(gui): add realistic chiclet keyboard visualizer`
- `fix(daemon): handle missing sysfs thermal node gracefully`
- `docs(handbook): align git workflow and pr rules with engineering handbook`
- `perf(rgb): reduce dynamic effect render loop latency to 25ms`
- `chore(release): prepare v1.0.0 distribution archive`

---

## 🔀 Pull Request Guide

Follow the [ShiftCore Pull Request Guide](https://platform.shiftcore.workers.dev/docs/handbook/docs/git/pull-request-guide):

### Standard Step-by-Step Workflow:
1. **Sync with latest main:**
   ```bash
   git switch main
   git pull origin main
   ```
2. **Create your task branch:**
   ```bash
   git switch -c feat/my-new-feature
   ```
3. **Make focused changes** (only modify files relevant to your task).
4. **Verify and test locally:**
   ```bash
   dotnet build gui/PredatorSense.csproj -c Release
   ```
5. **Stage and commit:**
   ```bash
   git add <specific-files>
   git commit -m "feat(scope): concise description of change"
   ```
6. **Push branch to GitHub:**
   ```bash
   git push -u origin feat/my-new-feature
   ```
7. **Open a Pull Request:**
   - Title format: `type: short description` (e.g., `feat: add per-key rgb visualizer`).
   - Fill out our PR template thoroughly:
     - **What changed?**
     - **Why?**
     - **How to review?**
     - **Notes / Hardware Tested On**
8. **Code Review:** Address feedback with additional commits on the same branch.
9. **Merge:** Once approved and CI checks pass, merge into `main` and delete the task branch.

---

## 🛠️ Local Development & Testing

### Prerequisites
- **.NET 9 SDK:** `dotnet-sdk-9.0`
- **Python:** Python 3.10+
- **Build Tools:** `gcc`, `make`, `linux-headers` (for active kernel)

### Build Commands:
```bash
# 1. Build Avalonia GUI (.NET 9)
dotnet build gui/PredatorSense.csproj -c Release

# 2. Run GUI locally in development
dotnet run --project gui/PredatorSense.csproj

# 3. Compile Kernel Module
make -C driver KVER="$(uname -r)"

# 4. Syntax check Python daemon
python3 -m py_compile daemon/*.py
```

---

## 🏷️ Tag & Release Standards

Releases follow [Semantic Versioning](https://semver.org/) (`vMAJOR.MINOR.PATCH`):
- Tags are created only from `main` after PR merges.
- Tag format: `vX.Y.Z` (e.g., `v1.0.0`, `v1.1.0`).
- Every release includes detailed release notes categorized by `Features`, `Bug Fixes`, `Performance`, and pre-compiled distribution archives with SHA-256 checksums.

---

## 📚 Handbook References

For deep dives into team practices, consult the ShiftCore Engineering Handbook:
- [Branch and Commit Rules](https://platform.shiftcore.workers.dev/docs/handbook/docs/git/branch-and-commit-rules)
- [Git Workflow](https://platform.shiftcore.workers.dev/docs/handbook/docs/git/git-workflow)
- [Pull Request Guide](https://platform.shiftcore.workers.dev/docs/handbook/docs/git/pull-request-guide)

Thank you for building a world-class gaming experience for Linux! 🚀
