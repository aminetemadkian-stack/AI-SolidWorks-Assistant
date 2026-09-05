# CI workflow (manual activation)

GitHub App sessions in this workspace cannot push files under
`.github/workflows/` (missing `workflows` permission), so the workflow is
versioned here. To activate CI on GitHub:

```bash
mkdir -p .github/workflows
cp Tools/ci/build.yml .github/workflows/build.yml
git add .github/workflows/build.yml
git commit -m "Enable Windows CI"
git push
```

The workflow builds the net472 add-in, runs the xUnit suite on
`windows-latest`, and publishes the `AiSolidWorksAssistant-win64` artifact
(DLL + icons + register scripts).
