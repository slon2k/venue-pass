# Templates

Output templates used by core prompts. Each file matches a prompt's expected output location.

| Template | Prompt | Target path |
|----------|--------|-------------|
| project-scope.md | project-plan | `docs/project-scope.md` |
| architecture-outline.md | architecture-plan | `docs/architecture-outline.md` |
| roadmap.md | roadmap-plan | `docs/roadmap.md` |
| milestone-plan.md | active milestone pointer | `docs/milestones/<name>.md` |
| feature-plan.md | feature-plan | Parent capability issue body |

Templates contain section headers with guidance comments. Prompts fill them in; you can also use them directly as blank starting points.
