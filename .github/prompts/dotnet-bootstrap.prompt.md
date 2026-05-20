---
name: dotnet-bootstrap
description: "Bootstrap a dotnet solution structure. Use when: starting a dotnet project, creating projects and test layout."
agent: agent
---

Bootstrap a dotnet project structure.

## Inputs

- project_name (string): ${{ input:project_name }}
- output_path (directory path): ${{ input:output_path }}

## Output

Create:

1. Solution file
2. src and tests folders
3. baseline projects
4. project references
5. root `.editorconfig` with practical defaults for .NET/C# formatting and code style
6. root `.gitignore` for .NET repository hygiene
7. root `Directory.Packages.props` for centralized package version management
8. initial build and test command list
9. optional root `Directory.Build.props` for shared MSBuild settings
10. optional root `global.json` to pin SDK version

## Requirements

- Keep structure generic and reusable (no product-specific assumptions).
- Use SDK-style projects and wire test projects to corresponding source projects.
- Ensure package references in project files do not include `Version` attributes when managed via `Directory.Packages.props`.
- Prefer clear defaults over exhaustive configuration.
- If `Directory.Build.props` is created, keep it focused on broadly applicable shared settings.
- If `global.json` is created, pin to an SDK version appropriate for the generated solution.

## Conventions

- Place all production projects under `src/` and tests under `tests/`.
- Name projects consistently from `project_name`.
- Keep generated files minimal, readable, and ready for CI use.
