# Contributing

Thanks for considering contributing — this project is at an early stage (Phase 0/1), so there's a lot of room to shape it.

## Ways to help right now

- **Design feedback**: open a Discussion about the architecture, the command schema, or the AI routing approach before code exists to lock in bad decisions.
- **Translations**: the 10-language documentation plan needs native speakers. See `docs/<lang>/` and `README_<LANG>.md` stubs.
- **SolidWorks API expertise**: if you've built SolidWorks add-ins before, your input on Phase 1 (`SolidWorks_Addin/`) is especially valuable.
- **Persian NLP for engineering terms**: Phase 5 needs real coverage of formal/colloquial Persian engineering phrasing, units, and mixed-language sentences.

## Workflow

1. Open an issue describing what you want to work on before submitting a large PR — this avoids duplicated effort while the architecture is still settling.
2. Fork, branch, make focused commits.
3. Open a PR against `main` with a clear description of what changed and why.

## Code guidelines (once implementation starts)

- The AI layer must never execute arbitrary code against a SolidWorks model — only structured, validated commands may reach the Command Engine (see architecture in `README.md`).
- No feature that talks to SolidWorks directly may bypass the `SolidWorks_Addin` → `CAD_Commands` → SolidWorks API layering.
- Don't claim engineering analysis results (Phase 10) as validated without a real calculation/simulation behind them.

## Code of conduct

Be respectful. Assume good faith. Disagreements about architecture are welcome as Discussions; personal attacks are not.
