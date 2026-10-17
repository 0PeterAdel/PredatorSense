---
name: "💻 Hardware Compatibility Report"
about: "Report compatibility testing results on your Acer Predator or Nitro laptop model"
title: "[Hardware Report]: "
labels: ["hardware-report", "compatibility"]
assignees: []
---

## 💻 Laptop Model & Hardware Details
- **Exact Laptop Model:** (e.g. Acer Predator Helios 16 PH16-72-91Q7)
- **CPU:** (e.g. Intel Core i9-14900HX / AMD Ryzen 9 7945HX)
- **GPU:** (e.g. NVIDIA GeForce RTX 4080 Laptop GPU)
- **Linux Distribution & Kernel:** (e.g. Fedora 40, Kernel 6.10.x)
- **PredatorSense Version:**

## ✅ Working Features Checklist
Please mark `[x]` for features you have verified working:
- [ ] Driver module compiles & loads cleanly (`linuwu_sense`)
- [ ] Hardware Predator key toggles application (`XF86Launch7`)
- [ ] Real-time CPU / GPU temperature and usage gauges
- [ ] CPU / GPU fan tachometer RPM readings
- [ ] 4-Zone RGB keyboard static colors
- [ ] 4-Zone RGB keyboard dynamic hardware & procedural effects
- [ ] Rear lid Logo illumination & dynamic modes
- [ ] Thermal Performance Profiles (Eco / Quiet / Balanced / Performance / Turbo)
- [ ] Fan control (Auto / Max / Custom RPM sliders)
- [ ] Battery charge limit (80% AC protection)
- [ ] USB Power-Off delivery settings
- [ ] LCD Overdrive & Boot sound toggles

## ⚠️ Issues or Deviations (if any)
Describe any features that didn't work as expected or required special workarounds on your specific model.

## 📋 DMI Information Output
Run `cat /sys/class/dmi/id/product_name` and `cat /sys/class/dmi/id/sys_vendor`:
```text
(paste output here)
```
