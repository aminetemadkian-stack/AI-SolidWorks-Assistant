# AI SolidWorks Assistant

An AI-powered assistant embedded inside **SolidWorks** that turns natural language (Persian & English, more languages planned) into real CAD operations — sketches, features, assemblies, and drawings — through a safe, auditable command pipeline.

> 🚧 **Status: Phase 1 — SolidWorks Add-in Foundation (in progress).** Phase 0 delivered the skeleton, architecture and docs. The Phase 1 C# add-in is now being built in `SolidWorks_Addin/` — task-pane chat UI, model context reader, validated PlanScript command pipeline (unit-tested, CI-built on Windows). See the [add-in README](SolidWorks_Addin/README.md) and [Roadmap](#-roadmap).

## 🌍 Documentation

- [English](docs/en/) (this page)
- [فارسی](README_FA.md)
- [العربية](README_AR.md)
- [Türkçe](README_TR.md)
- [Deutsch](README_DE.md)
- [Français](README_FR.md)
- [Español](README_ES.md)
- [Italiano](README_IT.md)
- [Português](README_PT.md)
- [Русский](README_RU.md)
- [中文](README_ZH.md)

## What this is

You describe what you want in plain language, e.g.:

> "Create a 100×50 mm bracket with two 10 mm holes."

The assistant understands the request, checks the current SolidWorks model state, plans a sequence of CAD operations, validates them, and executes them through the SolidWorks API — never by running arbitrary AI-generated code directly against your model.

## Architecture

```
                         ┌─────────────────────────────┐
                         │            USER              │
                         │   Persian / English / ...    │
                         └──────────────┬───────────────┘
                                        ▼
                    ┌────────────────────────────────────┐
                    │      SOLIDWORKS AI ASSISTANT        │
                    │        C# SolidWorks Add-in          │
                    └──────────────────┬───────────────────┘
                 ┌─────────────────────┼─────────────────────┐
                 ▼                     ▼                     ▼
        ┌────────────────┐    ┌────────────────┐    ┌────────────────┐
        │   AI Chat UI   │    │ Model Context  │    │ Command Center │
        │  RTL / LTR     │    │  CAD state     │    │   Settings     │
        └───────┬────────┘    └───────┬────────┘    └────────────────┘
                └──────────┬──────────┘
                           ▼
                 ┌──────────────────────┐
                 │        AI Core        │
                 │  intent · planning ·  │
                 │  context · validation │
                 │       · safety        │
                 └──────────┬───────────┘
                            ▼
                 ┌──────────────────────┐
                 │       AI Router       │
                 │   Online-first with   │
                 │   offline fallback    │
                 └──────────┬───────────┘
                  ┌─────────┴─────────┐
                  ▼                   ▼
         ┌─────────────────┐ ┌─────────────────┐
         │    Cloud AI     │ │    Local AI      │
         │ GPT / Claude /  │ │ Ollama: Qwen,    │
         │     Gemini      │ │    DeepSeek, ...  │
         └────────┬────────┘ └────────┬─────────┘
                  └──────────┬────────┘
                             ▼
                 ┌──────────────────────┐
                 │   CAD Interpreter     │
                 │ natural language  →   │
                 │  structured commands  │
                 └──────────┬───────────┘
                            ▼
                 ┌──────────────────────┐
                 │    Command Engine     │
                 │ Sketch · Extrude ·    │
                 │ Cut · Hole · Fillet · │
                 │ Chamfer · Pattern ·   │
                 │ Assembly · Drawing    │
                 └──────────┬───────────┘
                            ▼
                 ┌──────────────────────┐
                 │   SolidWorks API      │
                 └──────────┬───────────┘
                            ▼
                 ┌──────────────────────┐
                 │      SolidWorks       │
                 │ Part · Assembly ·     │
                 │       Drawing         │
                 └──────────────────────┘
```

### Key design principle

The AI **never** executes arbitrary code against your SolidWorks model. It only ever produces a structured command (e.g. `operation=hole, diameter=10mm, position=center`), which passes through validation before the Command Engine — the only layer allowed to touch the SolidWorks API — carries it out.

```
AI  →  decides & plans
 │
 ▼
CAD Interpreter  →  natural language → structured command
 │
 ▼
Command Engine  →  validated, standard CAD operations
 │
 ▼
SolidWorks API  →  the only layer that touches SolidWorks
 │
 ▼
SolidWorks
```

This separation means the AI model, the cloud provider, or the local model can all be swapped later without rewriting the SolidWorks integration itself.

### AI routing: online-first, offline fallback

```
Cloud AI
   │
   ├── succeeds → used
   │
   └── fails
        ↓
     Ollama (local)
```

The user never has to manually switch — if there's no internet, or the cloud call fails, the assistant falls back to a local Ollama model automatically.

## 🗺️ Roadmap

| Phase | Goal | Milestone | Status |
|---|---|---|---|
| 0 | Project foundation — repo, docs, architecture | This repository exists and is documented | ✅ Done |
| 1 | SolidWorks Add-in foundation | Add-in loads inside SolidWorks with an AI panel | 🚧 Code shipped — [chat task pane, model context, validated PlanScript pipeline (~35 tests)](SolidWorks_Addin/README.md). Awaiting first run on a real SOLIDWORKS machine |
| 2 | AI Core (controller, router, context manager, validator) | Core pipeline exists, not yet CAD-connected | — |
| 3 | Online AI (first cloud provider) | A cloud request produces a structured command | — |
| 4 | Offline AI (Ollama) | Assistant works with no internet | — |
| 5 | Persian engineering AI | Persian commands (formal & colloquial), units, mixed-language sentences | — |
| 6 | CAD command engine | Sketch, Extrude, Cut, Hole, Fillet, Chamfer, Pattern, Mirror | — |
| 7 | Intelligent modeling | Assistant proposes changes for approval, not just executes commands | — |
| 8 | Assembly intelligence | Insert components, mate, interference/collision awareness | — |
| 9 | Drawing & manufacturing | Drawings, BOM, dimensioning, manufacturing constraints | — |
| 10 | Engineering intelligence | Optimization suggestions — never presented as validated analysis without real calculation | — |
| 11 | Security & reliability | Permissions, command validation, sandboxing, logging, API key security | — |
| 12 | Public v1.0.0 release | Full source, docs, examples, tests, installation guides in 10 languages | — |

> Each completed phase is merged into `main` — `main` always reflects the latest state of the project. See [CHANGELOG.md](CHANGELOG.md) for the per-phase history.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md). Issues and Discussions are open — this is an early-stage project and design feedback is welcome.

## Security

See [SECURITY.md](SECURITY.md) for how to report a vulnerability.

## License

[MIT](LICENSE)
