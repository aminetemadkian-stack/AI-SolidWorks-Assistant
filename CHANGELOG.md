# Changelog

All notable changes to this project are documented here.

## [Unreleased] — Phase 1: SolidWorks Add-in Foundation

### Added
- `SolidWorks_Addin/src/AiSolidWorksAssistant` — the C# add-in skeleton:
  - COM entry point `SwAddinMain` implementing `ISwAddin` (swpublished GUID verified), ProgId `Ai.SolidWorksAssistant.SwAddinMain`.
  - Task pane chat UI (WinForms) with Persian/English strings and automatic RTL.
  - Read-only model context reader (active doc, configuration, feature tree, selection) via IDispatch late binding — no interop DLL dependency at build time.
  - `PlanScript` format + parser (Persian/Arabic digits, units, markdown-tolerant).
  - `PlanValidator` safety gate (unknown ops, non-positive/huge dims, Phase 6+ ops rejected).
  - `PlanPipeline` — confirm-before-execute, stop-on-first-failure, progress reporting.
  - Macro backend: deterministic VBA `.swb` generation + `RunMacro2` execution (rectangle, circle, extrude, cut, hole).
  - Echo intent engine proposing the embedded sample plan (real AI = Phases 2–4).
  - JSON settings store + file logger under `%APPDATA%\AiSolidWorksAssistant`.
- `Tools/register.cmd` / `Tools/unregister.cmd` — per-user RegAsm + HKCU SOLIDWORKS registration (no admin).
- `.github/workflows/build.yml` — Windows CI: builds net472, runs tests, publishes `AiSolidWorksAssistant-win64` artifact.
- `docs/fa/architecture.md` — full architecture reference (project brain).
- `Samples/Plans/` — ready-to-run PlanScript examples.

### Tests
- xUnit suite (~35 tests): parser, validator, pipeline, macro generation, echo engine, chat view model (incl. blocking confirmation flow), settings store.

### Notes
- Phase 0 documentation work is untouched; root README status lines updated.
- Undo granularity in Phase 1 = one SOLIDWORKS undo step per executed macro; plan-level grouping arrives with the Phase 6 native command engine.

## [0.0.0] — Phase 0: Project Foundation

### Added
- Repository structure: `SolidWorks_Addin/`, `AI_Core/`, `AI_Providers/`, `CAD_Commands/`, `Localization/`, `Tests/`, `Examples/`, `docs/<lang>/` for 10 languages.
- `README.md` (English) and `README_FA.md` (Persian) with full architecture diagram and roadmap.
- `LICENSE` (MIT), `CONTRIBUTING.md`, `SECURITY.md`.
- Issues, Wiki, and Discussions enabled.
