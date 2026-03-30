---
description: Execute the implementation planning workflow using the plan template to generate design artifacts.
handoffs:
  - label: Create Tasks
    agent: speckit.tasks
    prompt: Break the plan into tasks
    send: true
---

## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding (if not empty).

## Pre-Execution Checks

**Check for extension hooks (before planning)**:
- Check if `.specify/extensions.yml` exists in the project root.
- If it exists, read it and look for entries under the `hooks.before_plan` key
- If the YAML cannot be parsed or is invalid, skip hook checking silently and continue normally
- Filter out hooks where `enabled` is explicitly `false`. Treat hooks without an `enabled` field as enabled by default.
- For each remaining hook, do **not** attempt to interpret or evaluate hook `condition` expressions:
  - If the hook has no `condition` field, or it is null/empty, treat the hook as executable
  - If the hook defines a non-empty `condition`, skip the hook and leave condition evaluation to the HookExecutor implementation
- For each executable hook, output the following based on its `optional` flag:
  - **Optional hook** (`optional: true`):
    ```
    ## Extension Hooks

    **Optional Pre-Hook**: {extension}
    Command: `/{command}`
    Description: {description}

    Prompt: {prompt}
    To execute: `/{command}`
    ```
  - **Mandatory hook** (`optional: false`):
    ```
    ## Extension Hooks

    **Automatic Pre-Hook**: {extension}
    Executing: `/{command}`
    EXECUTE_COMMAND: {command}

    Wait for the result of the hook command before proceeding to the Outline.
    ```
- If no hooks are registered or `.specify/extensions.yml` does not exist, skip silently

## Outline

1. **Locate the feature spec**: Find the most recent or user-specified spec in `.specify/specs/`. Read `spec.md` from that feature directory. If user input names a feature, use that to locate the correct directory.

2. **Load context**: Read the feature spec.md and `.specify/memory/constitution.md`. Read the existing codebase structure to understand the tech stack, architecture patterns, and conventions already in use.

3. **Execute plan workflow**: Create `plan.md` in the feature directory following this structure:

   ```markdown
   # Implementation Plan: [Feature Name]

   ## Technical Context

   ### Tech Stack
   - [List technologies, frameworks, languages]

   ### Architecture Approach
   - [How this fits into the existing architecture]
   - [Key architectural decisions]

   ### Constitution Check
   - [ ] Principle 1: [How this plan upholds it]
   - [ ] Principle 2: [How this plan upholds it]

   ## Phase 0: Research

   ### Unknowns to Resolve
   - [List any NEEDS CLARIFICATION items]

   ### Decisions Made
   - Decision: [What was chosen]
   - Rationale: [Why]
   - Alternatives: [What else was considered]

   ## Phase 1: Design

   ### Data Model
   [Key entities and their relationships]

   ### Interface Contracts
   [API endpoints, event schemas, or other external interfaces]

   ### Project Structure
   [New files/directories to create, organized by layer]

   ## Dependencies
   [External packages or services needed]

   ## Risks & Mitigations
   [Known risks and how to handle them]
   ```

4. **Generate design artifacts** in the feature directory:
   - `data-model.md`: Entity definitions, fields, relationships, validation rules
   - `contracts/`: Interface contracts (API endpoints, event schemas, etc.) if applicable

5. **Re-evaluate Constitution Check** after design to ensure no violations.

6. **Report**: List the feature directory path, all generated artifacts, and suggest running `/speckit.tasks` to generate the task breakdown.

7. **Check for extension hooks**: After reporting, check `.specify/extensions.yml` for `hooks.after_plan` entries and handle per the same rules above.

## Key rules

- Use absolute paths
- ERROR on gate failures or unresolved clarifications
