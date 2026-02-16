# Contributing to GPU-T

First off, thank you! I am genuinely blown away by the reception this project has received. 

Because I have a full-time job and other responsibilities, I want to set some clear expectations to ensure the project remains sustainable and high-quality.

### 🕒 The "1-Hour" Rule
I am a solo maintainer and currently plan to dedicate approximately **1 hour per day** to this project. 
* **Pull Request Reviews:** Please be patient. Large PRs (like adding new vendors) require significant time to review carefully.
* **Issues:** I prioritize stability and bug fixes over new features.

### 🛠️ How to Contribute
1. **Open an Issue First:** For anything beyond a simple bug fix, please open an issue to discuss the change before writing code.
2. **Quality over Quantity:** I prefer small, focused Pull Requests that address a single issue. 
3. **Architecture:** Please follow the existing patterns (e.g., using the `GpuProbeFactory` and staying within the Avalonia/C# ecosystem).

### 🚀 Pull Request Requirements
* **Branching:** Always branch off the latest `main`.
* **Dependencies:** Keep the code "lightweight" - try to avoid adding unnecessary external NuGet packages or dependencies.
* **Testing:** If you are adding support for hardware I don't own (e.g. Intel), please provide screenshots and logs as proof of functionality in the PR description.

---
*Thank you for helping make GPU-T even better utility for Linux!*