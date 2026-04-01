![Wiper logo](Wiper.wpf/Resources/Images/wiper-logo.png)

Wiper
Wiper is a comprehensive cleanup utility for .NET developers. It is designed to handle the common frustrations of cleaning build artifacts by integrating directly with Visual Studio to manage file locks, save pending changes, and automatically rebuild solutions after a deep clean.
The project consists of a modern WPF desktop application and a lightweight CLI tool.

Key Features
 * Visual Studio Integration: Automatically detects open instances, saves work, and closes the application to release file locks.
 * Deep Clean: Targets standard directories like bin, obj, .vs, and TestResults, with support for custom folders such as node_modules.
 * Ghost Process Termination: Kills background processes (e.g., VBCSCompiler, MSBuild, or vbc) that often prevent folder deletion.
 * Dry Run Mode: Provides a simulation mode to see exactly which folders will be deleted and how much space will be reclaimed before any files are removed.
 * Auto-Restart & Rebuild: Optionally reopens Visual Studio and triggers a fresh build once the workspace has been wiped.
 * Modern UI/UX: Features a clean WPF interface with dark mode support and real-time logging.
   
WPF Application
The WPF version offers a visual pipeline to track the progress of the cleanup operation.
Usage
 * Selection: Enter the path to a .sln or .slnx file or use the file browser.
 * Scan: View a detailed breakdown of matching folders and total reclaimable space.
 * Filter: Toggle specific folder types (e.g., .vs) or add custom directory names.
 * Clean: Execute the cleanup. Use the "Dry Run" toggle to simulate the process without deleting files.
   
CLI Tool
The CLI version is optimized for power users and automation scripts.

Basic Commands
# Run a simulation (Dry Run)
wiper MyApp.sln

# Perform an actual deletion
wiper MyApp.sln --force

# Include .vs and TestResults folders
wiper MyApp.sln --force --all

Configuration Management
Wiper CLI saves your preferences globally.

# Add a custom folder to the permanent scan list
wiper config --add-folder node_modules

# Add a process to be terminated before cleaning
wiper config --add-ghost node.exe

# View current settings and defaults
wiper config --list

Technical Architecture
The project is built on .NET 9 and follows a modular design:
 * Wiper.Core: The central engine containing the FileService, VisualStudioService (utilizing COM Interop/DTE), and core business logic.
 * Wiper.WPF: A desktop client built with the CommunityToolkit.Mvvm following the MVVM pattern.
 * Wiper.Cli: A lightweight console interface for rapid operations.
   
Prerequisites
 * .NET 9.0 Runtime or SDK
 * Windows (Required for Visual Studio DTE Interop and WPF components)
Configuration File
User settings are stored in the application data folder:
%AppData%\Wiper\wiper.config.json

This file manages:
 * Default target folder lists.
 * Process names to terminate.
 * Last used solution paths.
 * User interface preferences (Dark/Light mode).
   
License
This project is licensed under the MIT License.
