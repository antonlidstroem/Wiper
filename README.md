![Wiper logo](Wiper.wpf/Resources/Images/wiper-logo.png)


This is a solid utility. As a senior dev, I’ve seen many "clean" scripts, but integrating Visual Studio Interop (DTE) to handle the "Save-Close-Wipe-Restart-Rebuild" cycle makes this actually useful for professional workflows.
Here is a professional, high-quality README.md tailored for your repository.

🧹 Wiper
The ultimate deep-clean utility for .NET Developers.
Wiper is a specialized tool designed to solve the "I need to reset my environment" problem. Unlike a simple git clean, Wiper understands the Visual Studio lifecycle. It doesn't just delete folders; it saves your work, shuts down the IDE, kills zombie compiler processes, wipes the bloat, and brings everything back to life with a fresh rebuild.

🚀 Why use Wiper?
Every .NET developer has faced the "Access Denied" error when trying to delete bin or obj folders. Wiper fixes this by:
 * VS Interop: Automatically saves all files and closes Visual Studio gracefully.
 * Zombie Hunting: Kills VBCSCompiler and MSBuild processes that frequently lock your files.
 * Deep Cleaning: Targets bin, obj, .vs, and TestResults.
 * Dual-Flavor: Use the CLI for speed or the WPF App for visual control and folder-by-folder selection.
 * Safety First: Supports Dry Run mode to see exactly what will be deleted and how much space you'll save.

🛠 Installation
Prerequisites
 * Windows OS (for WPF and VS Interop)
 * .NET 9.0 SDK
 * Visual Studio 2022 (for automated rebuilding)
Building from source
git clone https://github.com/yourusername/wiper.git
cd wiper
dotnet build -c Release

🖥 Usage
1. Wiper CLI
Perfect for quick runs or integration into your own scripts.
# Perform a dry run (safe simulation)
wiper "C:\Projects\MySolution.sln"

# Force the cleaning (closes VS and deletes files)
wiper "C:\Projects\MySolution.sln" --force

# Deep clean including .vs and TestResults
wiper "C:\Projects\MySolution.sln" --force --all

CLI Arguments:
| Argument | Description |
| :--- | :--- |
| --force | Executes the actual deletion. Without this, it stays in Dry Run mode. |
| --all | Includes .vs and TestResults folders in the scan. |
| -h, --help | Shows the help menu. |
2. Wiper GUI (WPF)
A modern interface for those who want granular control.
 * Paste your Solution path or browse for it.
 * Scan: See a breakdown of every bin/obj folder and how much disk space they occupy.
 * Select: Uncheck specific projects you don't want to wipe.
 * Execute: Watch the real-time log as Wiper orchestrates the Visual Studio cleanup.

🏗 Architecture
Wiper is built with a clean, modular architecture:
 * Wiper.Core: The engine. Contains logic for file system scanning, ByteSize formatting, and the VisualStudioService that handles DTE automation.
 * Wiper.Cli: A lightweight console interface for power users.
 * Wiper.Wpf: A rich desktop client built using MVVM (via CommunityToolkit.Mvvm).
The Clean Cycle
 * DTE Hook: Connects to the running instance of the provided solution.
 * Pre-Cleanup: Saves all documents and triggers a native Solution.Clean.
 * Process Kill: Terminates ghost processes that hold file locks.
 * Wipe: Recursively deletes target directories with retry logic.
 * Restart: Re-opens the solution and triggers a Rebuild Solution.

🤝 Contributing
 * Fork the Project
 * Create your Feature Branch (git checkout -b feature/AmazingFeature)
 * Commit your Changes (git commit -m 'Add some AmazingFeature')
 * Push to the Branch (git push origin feature/AmazingFeature)
 * Open a Pull Request

📄 License
Distributed under the MIT License. See LICENSE for more information.
Developed with ❤️ for the .NET Community.
Would you like me to help you generate a LICENSE file or a .gitignore to go along with this?
  