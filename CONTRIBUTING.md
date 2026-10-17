# Contributing to PredatorSense for Linux 🎮

Thank you for your interest in contributing to **PredatorSense for Linux**! Community contributions—whether they are bug fixes, support for new Acer laptop models, feature enhancements, or documentation improvements—are warmly welcomed and deeply appreciated.

---

## 📜 Open Source & Attribution Terms

PredatorSense for Linux is free open-source software licensed under the **GNU General Public License v3.0 (GPLv3)** with an **Author Attribution Requirement** (pursuant to GPLv3 Section 7(b)):

> **You are completely free to use, modify, fork, and distribute this software, provided that you clearly and prominently attribute the original author (Peter Adel / 0PeterAdel) and link back to the official repository:**  
> **https://github.com/0PeterAdel/PredatorSense**

By submitting a Pull Request, you agree that your code will be released under these exact same terms.

---

## 🚀 How to Contribute

### 1. Reporting Bugs & Issues
If you encounter a bug or unexpected behavior:
1. Search existing [Issues](https://github.com/0PeterAdel/PredatorSense/issues) to ensure it hasn't already been reported.
2. If not found, open a new issue using our **🐛 Bug Report** template.
3. Include your laptop model, Linux distribution, kernel version (`uname -r`), and daemon logs (`journalctl -u predatorsense-daemon.service -n 50`).

### 2. Suggesting New Features
Have an idea to make PredatorSense even better?
1. Open a new issue using our **🚀 Feature Request** template.
2. Describe your idea, its intended use-case, and why it benefits Acer Linux gaming users.
3. Feel free to include UI sketches or mockups!

### 3. Reporting Laptop Compatibility
Help us expand our list of verified hardware! If you test PredatorSense on your Acer laptop, open a **💻 Hardware Compatibility Report** and tell us which features work.

---

## 🛠️ Local Development Setup

### Prerequisites
Ensure you have the following build tools installed on your Linux machine:
- **.NET 9 SDK:** `dotnet-sdk-9.0`
- **Python:** Python 3.10+
- **C Compiler & Headers:** `gcc`, `make`, `linux-headers` (for your active kernel)

### Building the Project

#### 1. Graphical User Interface (Avalonia UI / .NET 9)
```bash
# Debug Build
dotnet build gui/PredatorSense.csproj

# Run locally in development
dotnet run --project gui/PredatorSense.csproj

# Release Build
dotnet build gui/PredatorSense.csproj -c Release
```

#### 2. Kernel Driver (`linuwu_sense`)
```bash
cd driver
make clean
make KVER="$(uname -r)"
```

#### 3. Daemon Service (Python)
```bash
cd daemon
python3 -m py_compile *.py
```

---

## 📝 Commit Conventions

We follow [Conventional Commits](https://www.conventionalcommits.org/) for clear and structured git history:

- `feat:` A new user-facing feature or enhancement (e.g. `feat(gui): add concentric zoom lighting animation`)
- `fix:` A bug fix (e.g. `fix(driver): handle missing WMI thermal GUID gracefully`)
- `docs:` Documentation changes only (e.g. `docs: update installation instructions for Fedora`)
- `style:` Changes that do not affect the meaning of code (formatting, whitespace, etc.)
- `refactor:` Code restructuring without adding features or fixing bugs
- `perf:` Performance improvements
- `ci:` Changes to CI configuration or build scripts

---

## 🔀 Submitting a Pull Request (PR)

1. **Fork** the repository on GitHub.
2. **Clone** your fork locally:
   ```bash
   git clone https://github.com/<your-username>/PredatorSense.git
   cd PredatorSense
   ```
3. **Create a branch** for your work:
   ```bash
   git checkout -b feat/my-awesome-feature
   ```
4. **Make your changes** cleanly and test thoroughly on your machine.
5. **Verify the build**:
   ```bash
   dotnet build gui/PredatorSense.csproj -c Release
   ```
6. **Commit** your changes following conventional commit syntax.
7. **Push** to your fork:
   ```bash
   git push origin feat/my-awesome-feature
   ```
8. **Open a Pull Request** to the `main` branch of `0PeterAdel/PredatorSense`.
9. Fill out our PR template thoroughly. Maintainers will review your PR and provide constructive feedback!

---

## 💡 Code & UI Guidelines

- **Cyber Dark Glass Theme:** UI additions must respect the Acer Predator cyberpunk aesthetic: dark glass panels (`#080f14`), glowing cyber-teal borders (`#00ff9d` / `#00d4ff`), and crisp typography.
- **Documentation Integrity:** Preserve all existing architectural comments, docstrings, and author headers.
- **Safety First:** Driver and ACPI calls interact with laptop motherboard firmware (EC). Always sanitize inputs and prevent potential hardware lockups.

---

## 🤝 Community & Support

- **Repository:** [https://github.com/0PeterAdel/PredatorSense](https://github.com/0PeterAdel/PredatorSense)
- **Discussions:** [https://github.com/0PeterAdel/PredatorSense/discussions](https://github.com/0PeterAdel/PredatorSense/discussions)

Thank you for helping make gaming on Linux incredible! 🚀
