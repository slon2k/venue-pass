---
name: topic-explore
description: "Explore a topic through focused interview until reaching shared understanding. Use when: stress-testing a plan, validating assumptions, before generating any planning artifact."
agent: agent
---

Explore this topic through focused interview until you reach a shared understanding.

## Inputs

- topic (string — description of the plan, design, or feature to interrogate): ${{ input:topic }}

## Workflow

1. Walk down each branch of the decision tree, resolving dependencies between decisions one by one.
2. For each question, provide your recommended answer so the user can accept, reject, or refine it.
3. Ask questions **one at a time** — never batch multiple questions.
4. If a question can be answered by exploring the codebase or project docs, explore instead of asking.
5. Cover at minimum: goals, constraints, scope boundaries, risks, unknowns, and trade-offs.
6. Continue until every open branch is resolved.

## Output

When the interview is complete, produce a concise **Decision Summary** listing:

1. Decisions made (numbered)
2. Open questions (if any remain)
3. Out-of-scope items identified during the conversation
4. Recommended next step (describe the action, not a specific prompt name)
