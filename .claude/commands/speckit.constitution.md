---
description: Create or update the project constitution in .specify/memory/constitution.md with project principles, standards, and governance rules.
---

## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding (if not empty).

## Outline

1. **Load existing constitution**: Check if `.specify/memory/constitution.md` exists. If it does, read it. If not, you will create it from scratch.

2. **Gather project context**:
   - Read the README.md if it exists for project overview
   - Scan the repository structure to understand the tech stack, languages, and frameworks in use
   - Consider user input for any specific principles or requirements

3. **Draft the constitution** with these sections:

   ### PROJECT IDENTITY
   - Project name and purpose
   - Core mission statement (1-2 sentences)

   ### PRINCIPLES
   Define 4-6 core principles that guide all decisions. Each principle should be:
   - Declarative and testable (not vague)
   - Actionable (describes behavior, not aspirations)
   - Specific to this project's context

   Example structure:
   ```
   **[PRINCIPLE_NAME]**: [Concrete rule that can be verified]
   ```

   ### CODING STANDARDS
   - Language-specific conventions (naming, formatting)
   - Architecture patterns to follow
   - Patterns to avoid

   ### TESTING STANDARDS
   - Testing approach (unit, integration, e2e)
   - Coverage expectations
   - Test naming conventions

   ### QUALITY GATES
   - Definition of "done" for features
   - Required checks before merging
   - Performance/security requirements

   ### CONSTITUTION VERSION
   - Version: MAJOR.MINOR.PATCH (start at 1.0.0 for new, increment appropriately)
   - Ratification date: today's date (ISO format YYYY-MM-DD)
   - Last amended: today's date

4. **Validate the constitution**:
   - No bracketed placeholder tokens `[LIKE_THIS]` remain
   - All principles are concrete and testable
   - Dates are in ISO format (YYYY-MM-DD)

5. **Write the file** to `.specify/memory/constitution.md`

6. **Report**: Summarize what was created/changed, list all principles defined, and suggest running `/speckit.specify` to start a feature specification.
