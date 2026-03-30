---
description: Execute the implementation plan by processing and executing all tasks defined in tasks.md
---

## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding (if not empty).

## Pre-Execution Checks

**Check for extension hooks (before implementation)**:
- Check if `.specify/extensions.yml` exists in the project root.
- If it exists, read it and look for entries under the `hooks.before_implement` key
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

1. **Locate the feature directory**: Find the most recent or user-specified feature in `.specify/specs/`.

2. **Check checklists status** (if `<FEATURE_DIR>/checklists/` exists):
   - Scan all checklist files in the checklists/ directory
   - For each checklist, count total/completed/incomplete items
   - Display a status table
   - **If any checklist is incomplete**: Ask "Some checklists are incomplete. Do you want to proceed with implementation anyway? (yes/no)" and wait for response
   - **If all checklists are complete**: Automatically proceed

3. **Load and analyze the implementation context**:
   - **REQUIRED**: Read tasks.md for the complete task list and execution plan
   - **REQUIRED**: Read plan.md for tech stack, architecture, and file structure
   - **IF EXISTS**: Read data-model.md for entities and relationships
   - **IF EXISTS**: Read contracts/ for API specifications and test requirements
   - **IF EXISTS**: Read research.md for technical decisions and constraints

4. **Project Setup Verification**: Create/verify ignore files based on actual project setup:
   - Check if git repo → verify .gitignore has appropriate entries
   - Check if Dockerfile exists → verify .dockerignore exists
   - For C#/.NET: ensure `bin/`, `obj/`, `*.user`, `*.suo` are in .gitignore
   - For Node.js: ensure `node_modules/`, `dist/`, `.env*` are in .gitignore

5. **Parse tasks.md structure** and extract:
   - Task phases: Setup, Foundational, User Stories, Polish
   - Task dependencies: Sequential vs parallel [P] execution rules
   - Task details: ID, description, file paths, parallel markers

6. **Execute implementation** following the task plan:
   - **Phase-by-phase execution**: Complete each phase before moving to the next
   - **Respect dependencies**: Run sequential tasks in order, parallel tasks [P] can run together
   - **File-based coordination**: Tasks affecting the same files must run sequentially
   - **Validation checkpoints**: Verify each phase completion before proceeding

7. **Implementation execution rules**:
   - **Setup first**: Initialize project structure, dependencies, configuration
   - **Core development**: Implement models, services, controllers/handlers, endpoints
   - **Integration work**: Database connections, middleware, logging, external services
   - **Polish and validation**: Unit tests, performance optimization, documentation

8. **Progress tracking and error handling**:
   - Report progress after each completed task
   - Halt execution if any non-parallel task fails
   - For parallel tasks [P], continue with successful tasks, report failed ones
   - Provide clear error messages with context for debugging
   - **IMPORTANT**: For completed tasks, mark the task off as `[x]` in the tasks file

9. **Completion validation**:
   - Verify all required tasks are completed
   - Check that implemented features match the original specification
   - Confirm the implementation follows the technical plan
   - Report final status with summary of completed work

Note: This command assumes a complete task breakdown exists in tasks.md. If tasks are incomplete or missing, suggest running `/speckit.tasks` first to regenerate the task list.

10. **Check for extension hooks**: After completion validation, check `.specify/extensions.yml` for `hooks.after_implement` entries and handle per the same rules above.
