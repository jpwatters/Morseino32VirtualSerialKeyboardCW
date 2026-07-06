# Morserino32 Serial Keyboard for Windows

A Windows port of the macOS Morserino32 Serial Keyboard app. Same purpose as
the Mac version: send typed text to a [Morserino-32](https://morserino.info)
over USB serial so it plays that text as audible Morse (via the device's
"M32 Protocol" v1.3), from a device dropdown, a live compose field, and
sixteen renamable quick-macro buttons.

**This code has not been compiled or run.** It was written in a Linux
sandbox with no .NET SDK or Windows runtime available, so nothing here has
been build-tested. It's a careful, from-source port of the working Mac
app's logic (the C# is new, but the protocol and persistence behavior it
implements are copied line-for-line from the Mac app's `SerialKeyboard.m`
and `AppDelegate.m`), but you should expect to spend a few minutes fixing
any small compile errors the first time you build it on an actual Windows
machine.

## Project layout

| Path | Purpose |
|---|---|
| `MorserinoWinKeyboard/MorserinoWinKeyboard.csproj` | .NET 8 WinForms project file |
| `MorserinoWinKeyboard/Program.cs` | Entry point |
| `MorserinoWinKeyboard/MainForm.cs` | The whole UI: device picker, quick-macro grid, compose field, ENTER/DELETE |
| `MorserinoWinKeyboard/SerialKeyboard.cs` | USB serial transport speaking the M32 protocol (`System.IO.Ports.SerialPort`) |
| `MorserinoWinKeyboard/IKeyboardTransport.cs` | Transport interface + event-args type, the Windows counterpart of the Mac app's `KeyboardTransport` protocol |
| `MorserinoWinKeyboard/QuickMacroStore.cs` | Loads/saves the 16 button labels to `QuickMacroButtons.json` next to the .exe |
| `MorserinoWinKeyboard/PromptForm.cs` | Small "rename this button" dialog (WinForms has no built-in input box) |
| `MorserinoWinKeyboard/Resources/app.ico` | Multi-resolution app icon (same M32-over-keyboard artwork as the Mac app) |

## How it maps from the Mac app

| Mac (Objective-C / AppKit) | Windows (C# / WinForms) |
|---|---|
| `SerialKeyboard.m` — POSIX `open()`/`termios`, `/dev/cu.*` | `SerialKeyboard.cs` — `System.IO.Ports.SerialPort`, `COM*` ports |
| `KeyboardTransport` protocol | `IKeyboardTransport` interface |
| `NSNotificationCenter` connect/disconnect/fail notifications | C# events (`Connected`, `Disconnected`, `Failed`) |
| `AppDelegate` + `MainMenu.xib` | `MainForm.cs` (UI built in code, no designer file) |
| Sibling `QuickMacroButtons.plist` next to the `.app`, with `NSUserDefaults` migration | Sibling `QuickMacroButtons.json` next to the `.exe` (no migration needed — new app) |
| `NSAlert` with an accessory text field (rename dialog) | `PromptForm` (custom `Form`) |
| `.icns` / Images.xcassets `AppIcon.appiconset` | `Resources/app.ico` (7 sizes: 16–256px) |
| Gatekeeper / ad-hoc signing, right-click → Open on first launch | SmartScreen may warn on first launch of an unsigned .exe — see below |

The M32 protocol itself is unchanged: 115200 baud, 8N1, commands are
`PUT device/protocol/on`, `PUT cw/play/<text>`, `PUT cw/stop`,
`PUT device/protocol/off`, each terminated with a single `\n` (not `\r\n`).

## Prerequisites

- Windows 10 or 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download) (the WinForms + `System.IO.Ports` bits used here are part of the SDK — no extra NuGet packages required)
- A Morserino-32 connected via USB, in **CW Keyer** or **Morse Trx** mode

## Building and running

### Option A: `dotnet` CLI

```
cd MorserinoWinKeyboard
dotnet build
dotnet run
```

### Option B: Visual Studio

1. Open `MorserinoWinKeyboard/MorserinoWinKeyboard.csproj` directly (File → Open → Project/Solution — a loose `.csproj` opens fine without a `.sln`).
2. Set the startup project if prompted, then Build → Build Solution, then Debug → Start Without Debugging.

### First run

1. Plug in the Morserino-32 and make sure it's awake (a sleeping device won't show up as a COM port).
2. Put it into CW Keyer or Morse Trx mode.
3. In the app, click **Refresh** if the port isn't listed yet, pick the right `COM*` port, and click **Connect**.
4. Type into the compose field and press Enter, or click any quick-macro button, to have it played as Morse.

## Producing a standalone .exe

For a single-file build that doesn't need the .NET runtime pre-installed on
the target machine:

```
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

The output lands under `bin/Release/net8.0-windows/win-x64/publish/`.

## A note on Windows SmartScreen

This project isn't code-signed. The first time you run a freshly built
`.exe` (especially one copied to another machine, or downloaded), Windows
SmartScreen may show "Windows protected your PC." Click **More info** →
**Run anyway**. This is the direct Windows equivalent of the Mac app's
Gatekeeper right-click → Open step for its own ad-hoc-signed build — neither
app is signed with a purchased code-signing certificate, so both trigger a
first-run warning that's safe to dismiss for a build you made yourself.

## Known limitations (inherited from the Mac app's design)

- Write-only implementation of the M32 protocol: it sends `cw/play` and
  `cw/stop` but doesn't parse the JSON status responses the Morserino sends
  back.
- No automatic reconnect if the device is unplugged or goes to sleep
  mid-session — reconnect manually via the device dropdown.
- ENTER and DELETE both send `PUT cw/stop`; the M32 protocol has no
  separate concept for either one.
