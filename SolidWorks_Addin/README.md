# SolidWorks_Addin — Phase 1

The C# SOLIDWORKS add-in shell: **task-pane chat UI (RTL/LTR), read-only model
context, and a validated CAD command pipeline** (PlanScript → validator → VBA
macro generation → execution with user approval).

Status: **Phase 1 foundation** — see [`docs/fa/architecture.md`](../docs/fa/architecture.md)
for the full architecture and roadmap.

---

## Project layout

```
SolidWorks_Addin/
├── src/AiSolidWorksAssistant/        the add-in (class library, net472)
│   ├── addin/    SwAddinMain (COM entry), task pane host, swpublished COM declaration
│   ├── ui/       WinForms chat surface (RTL-aware), theme, EN/FA strings
│   ├── chat/     ChatViewModel (framework-free, unit-tested)
│   ├── core/     intent engine contract + Phase 1 echo engine, model context reader
│   ├── cad/      PlanParser, PlanValidator, PlanPipeline, macro generator/runner
│   ├── settings/ JSON settings store (%APPDATA%\AiSolidWorksAssistant)
│   ├── samples/  embedded sample plan (echo engine proposal)
│   └── Resources/icons  task pane + command icons (16/24/32)
├── tests/AiSolidWorksAssistant.Tests/  xUnit suite (~35 tests)
└── libs/interop/  (gitignored) optional local interop DLLs — NOT needed to build
```

### Build philosophy: no interop DLLs required

The add-in talks to SOLIDWORKS through **IDispatch late binding** instead of
referencing `SolidWorks.Interop.*` assemblies:

- the only COM interface it declares is `ISwAddin` (swpublished, GUID
  `{DA306A0D-EAC5-4406-8610-B1DA805D9270}` — verified against the shipped
  interop metadata);
- everything else (`ISldWorks`, `IModelDoc2`, `ICommandManager`, ...) is called
  dynamically, which is exactly how VBA macros address the API.

This keeps the repo buildable anywhere (including CI) without
redistributing Dassault's interop binaries.

## Build

```bash
dotnet build SolidWorks_Addin/src/AiSolidWorksAssistant -c Release
dotnet test  SolidWorks_Addin/tests/AiSolidWorksAssistant.Tests -c Release
```

Requires .NET SDK 8 (builds `net472` through reference assemblies) — CI does
exactly this on `windows-latest` (`.github/workflows/build.yml`). The green
"Build" workflow run on GitHub publishes the `AiSolidWorksAssistant-win64`
artifact containing the DLL + icons + register scripts.

## Install on a machine with SOLIDWORKS (2017+ recommended)

1. Get `AiSolidWorksAssistant.dll` (+ `Resources/`, `register.cmd`) — build it
   yourself or download the CI artifact.
2. Put the files in a stable folder, e.g. `%APPDATA%\AiSolidWorksAssistant\bin`.
3. Run `register.cmd <path\to\AiSolidWorksAssistant.dll>`
   (uses 64-bit RegAsm `/codebase`, writes `HKCU\SOFTWARE\SolidWorks\Addins\{597151FC-6387-4AEE-B0F7-E0844B6E9F0F}`,
   **no admin rights needed**).
4. Start SOLIDWORKS → the **AI Assistant** task pane tab appears on the right
   side (and a button in the CommandManager to re-open it after closing).

Uninstall: `unregister.cmd`.

> The registry path + GUID are chosen in `Tools/register.cmd` and must match
> `[Guid]` on `SwAddinMain`. The MSI/App-Store installer is planned for the
> public v1.0.0 release (Phase 12).

## What it does today (Phase 1 scope)

| Capability | Where |
|---|---|
| Loads inside SOLIDWORKS, opens a chat task pane | `addin/SwAddinMain.cs` |
| Persian/English UI with automatic RTL | `ui/` |
| Reads active document state (type, title, config, features, selection) | `core/modelcontext/` |
| Understands a request → proposes a sample plan (echo engine) | `core/EchoIntentEngine.cs` |
| Parses PlanScript (incl. Persian digits, units) | `cad/PlanParser.cs` |
| Validates plans (the safety gate) | `cad/PlanValidator.cs` |
| Executes approved plans via generated VBA macros | `cad/SwMacroGenerator.cs`, `cad/SwMacroRunner.cs` |
| Operations: rectangle, circle, extrude, cut, hole | `cad` (`OpTypes`) |

Operations that need the Phase 6 native command engine (fillet, chamfer,
pattern, mirror, assembly) are **parsed but rejected by validation** with a
clear message.

## Debug in Visual Studio

1. Open the folder in VS 2022 (`src/AiSolidWorksAssistant.csproj`).
2. Register a debug build: `Tools/register.cmd src\AiSolidWorksAssistant\bin\Debug\net472\AiSolidWorksAssistant.dll`.
3. Set the project's *Start Action* → *Start external program* →
   `SLDWORKS.exe` (F5). Breakpoints in the add-in hit normally.

## Logs

`%APPDATA%\AiSolidWorksAssistant\logs\addin-YYYYMMDD.log` — every connect /
disconnect / pane creation / macro run is logged with errors.
