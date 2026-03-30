---
description: Create a feature specification from a natural language description. Generates a spec.md in .specify/specs/<feature-name>/ directory.
handoffs:
  - label: Create Implementation Plan
    agent: speckit.plan
    prompt: Create an implementation plan for this spec
    send: true
---

## User Input

```text
$ARGUMENTS
```

You **MUST** use the user input as the feature description to specify.

## Outline

1. **Parse the feature description** from user input:
   - Extract the core feature name (2-4 words, kebab-case for directory)
   - Identify the primary user need being addressed
   - Note any constraints or requirements mentioned

2. **Check for constitution**: Read `.specify/memory/constitution.md` if it exists to ensure the spec aligns with project principles.

3. **Ask clarifying questions** if needed (maximum 3):
   - Prioritize by: scope > security/privacy > user experience > technical details
   - Only ask what is truly necessary to write a complete spec
   - Wait for answers before proceeding

4. **Create the feature directory**: `.specify/specs/<feature-name>/`

5. **Write `spec.md`** with the following structure:

   ```markdown
   # Feature: [Feature Name]

   ## Overview
   [1-2 paragraph description of the feature and its value]

   ## User Stories

   ### US1 - [Story Name] (P1)
   **As a** [role]
   **I want to** [action]
   **So that** [benefit]

   **Acceptance Criteria:**
   - [ ] [Measurable, testable criterion]
   - [ ] [Measurable, testable criterion]

   ### US2 - [Story Name] (P2)
   [repeat pattern]

   ## Out of Scope
   - [Explicit exclusions to prevent scope creep]

   ## Success Metrics
   - [Measurable, technology-agnostic metric]
   - [User-focused outcome, not implementation detail]

   ## Open Questions
   - [Any remaining unknowns]
   ```

6. **Quality validation** before writing:
   - No implementation details (languages, frameworks, APIs) in requirements
   - All acceptance criteria are testable and measurable
   - Success metrics are technology-agnostic and user-focused
   - User stories follow the "As a / I want / So that" format

7. **Report**: Output the spec file path, list all user stories with priorities, and suggest running `/speckit.plan` to create the implementation plan.
