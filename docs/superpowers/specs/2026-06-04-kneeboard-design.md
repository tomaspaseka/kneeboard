# Kneeboard App — Design Spec
Date: 2026-06-04

## Overview

A pilot kneeboard app that displays a series of images and PDFs in a fullscreen, tabbed interface. Designed for touch screens, targeting Windows 11 tablet as primary platform and Android as secondary.

## Technology

- **Framework:** .NET MAUI (net9.0)
- **Language:** C#
- **Pattern:** MVVM
- **Target platforms:** Windows 11 (net9.0-windows10.0.19041.0), Android (minimum API 24)
- **Excluded platforms:** iOS, macOS

## Architecture

MVVM separates UI from logic and data:

- **Views** — XAML pages (tab bar, fullscreen document viewer)
- **ViewModels** — page state and commands (current tab, current document page)
- **Models** — data shapes (KneeboardTab, KneeboardDocument)
- **Services** — file access and any future integrations (OneDrive, etc.)

## Project Structure

```
Kneeboard/
├── Platforms/              # Auto-generated platform-specific code (Windows, Android)
├── Resources/              # Icons, fonts, images, splash screen
├── Views/                  # XAML pages
├── ViewModels/             # ViewModel classes
├── Models/                 # Data models
├── Services/               # File access and future integrations
└── MauiProgram.cs          # App entry point and DI container
```

## Dependencies

| Package | Purpose |
|---|---|
| `CommunityToolkit.Maui` | Extra controls, touch behaviors, tab bar helpers |
| `CommunityToolkit.Mvvm` | MVVM source generators, cleaner ViewModels |

## PDF and Image Rendering

PDF and image files are rendered via MAUI's built-in `WebView` pointed at a local file URI. This uses the OS-native renderer (Edge/WebView2 on Windows, system WebView on Android) with no additional PDF library required.

## Touch Support

Touch is handled natively by MAUI on both platforms. `CommunityToolkit.Maui` provides additional touch gesture recognizers for swipe navigation between tabs and pages.

## Out of Scope (this phase)

- OneDrive / Microsoft Graph integration
- Any business logic beyond scaffolding
- iOS and macOS targets
