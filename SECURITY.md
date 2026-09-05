# Security Policy

## Reporting a vulnerability

If you find a security issue (e.g. a way for an AI-generated command to bypass validation and execute unintended operations, or a way an API key could leak), please **do not open a public issue**.

Instead, use GitHub's private vulnerability reporting: go to the **Security** tab of this repository → **Report a vulnerability**. If that's unavailable, open a Discussion asking a maintainer to contact you privately.

## Scope

Once implementation begins, particular areas of concern will be:

- **Command validation bypass**: any path where a natural-language request could reach the SolidWorks API without passing through the Command Engine's validation.
- **API key handling**: cloud AI provider keys (OpenAI/Anthropic/Google) and any local credentials must never be logged, committed, or exposed in error messages.
- **Offline model safety**: local Ollama models should not be able to execute anything beyond producing structured command suggestions.
- **Sandboxing**: destructive operations (deleting features, overwriting files) should require explicit user confirmation.

## Supported versions

This project has not reached a `v1.0.0` release yet. Until then, only the `main` branch is supported.
