# Installation Guide (Planned — Phase 1+)

> This guide describes the **intended** installation flow once the SolidWorks Add-in (Phase 1) exists. It is written now, ahead of implementation, so Phase 1 has a concrete target to build toward. Nothing in this guide is installable yet — see the [Roadmap](../../README.md#-roadmap) for current status.
>
> **✅ Update (Phase 1 code shipped):** the first runnable bits now exist. The short real-world path today: build `AiSolidWorksAssistant.dll` (VS 2022 or `dotnet build -c Release`) → run `Tools/register.cmd <path\to\AiSolidWorksAssistant.dll>` (per-user, no admin) → start SOLIDWORKS. Full details in the [add-in README](../../SolidWorks_Addin/README.md). The flow below stays as the target for the final release installer.

## Step 1 — Prerequisites

- A supported SolidWorks version (to be pinned once Phase 1 development starts against a specific SDK version)
- Visual Studio 2022 (for building the Add-in from source)
- .NET Framework (version to be pinned to match the SolidWorks API SDK)
- [Ollama](https://ollama.com) installed, for offline mode
- An API key from OpenAI, Anthropic, or Google, for online mode

## Step 2 — Get the project

```bash
git clone https://github.com/aminetemadkian-stack/AI-SolidWorks-Assistant.git
cd AI-SolidWorks-Assistant
```

New to Git? [GitHub's own guide](https://docs.github.com/en/get-started/quickstart) is a good starting point.

## Step 3 — Configure AI

- Online mode: set your GPT / Claude / Gemini API key (mechanism to be defined in Phase 3 — likely an environment variable or a settings file, never committed to the repo).
- Offline mode: point the assistant at your local Ollama installation and chosen model (Qwen, DeepSeek, etc.).
- Choose a mode: **Online-first** (default, falls back to offline automatically) or **Offline-only**.

## Step 4 — Build the project

- Open the solution in Visual Studio 2022.
- Build the Add-in project.
- Register the Add-in with SolidWorks (COM registration — exact steps to be documented once `SolidWorks_Addin/` exists).

## Step 5 — First run

- Open SolidWorks with the Add-in enabled.
- Open the AI panel.
- Try a first command, e.g.:

  > "Create a 100 mm cube."

## Step 6 — Troubleshooting

See [troubleshooting.md](troubleshooting.md) once Phase 1 lands. Planned coverage: installation issues, API connection issues, SolidWorks Add-in registration issues, Ollama issues, and common errors.
