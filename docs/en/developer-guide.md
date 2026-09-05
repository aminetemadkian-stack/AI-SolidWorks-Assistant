# Developer Guide (Phase 0 — architecture reference)

This describes the intended module boundaries so early contributors build toward the same architecture. See `README.md` for the full diagram.

| Folder | Responsibility | Phase |
|---|---|---|
| `SolidWorks_Addin/` | C# Add-in shell, AI chat panel (RTL/LTR), model context reader | 1 |
| `AI_Core/` | Intent detection, planning, context, validation, safety | 2 |
| `AI_Providers/` | Cloud provider adapters (OpenAI/Anthropic/Google) + Ollama adapter, online-first router | 3–4 |
| `CAD_Commands/` | Structured command schema + Command Engine (Sketch, Extrude, Cut, Hole, Fillet, Chamfer, Pattern, Assembly, Drawing) | 6 |
| `Localization/` | Persian/English (and later, other language) strings and NLP phrasing | 5 |
| `Tests/` | Unit and integration tests | ongoing |
| `Examples/` | Example commands and expected CAD outcomes | ongoing |

## The one rule that shouldn't be broken

`AI_Core` and `AI_Providers` must never call the SolidWorks API directly. Every CAD-affecting action goes through `CAD_Commands`' validated command schema. This is what keeps the AI layer swappable and the SolidWorks integration testable in isolation.
