---
name: adr
description: "Generate Architecture Decision Records. Use when discussing architecture decisions, technology choices, design trade-offs, or when the user explicitly asks to document a decision."
disable-model-invocation: false
allowed-tools: Bash, Glob, Grep, Read, Write
context: fork
argument-hint: '[decision title]'
---

Generate an Architecture Decision Record for: $ARGUMENTS

## Process

### 1. Determine the next ADR number
Find the highest-numbered ADR in `docs/adr/` and use the next sequential number. If no ADRs exist, start at 0001.

### 2. Load the template
Read the template from [templates/adr-template.md](templates/adr-template.md) and use it as the structure for the new ADR.

### 3. Fill in the Context section
**CRITICAL**: The Context section MUST reference SPECIFIC files in the codebase. Use Grep and Glob to find the relevant source files, services, configurations, or infrastructure code related to the decision. Include paths like `src/Ordering.API/...` or `src/eShop.AppHost/...` — never write a Context section without concrete file references.

### 4. Fill in all sections
- **Status**: Set to `Proposed` unless the user specifies otherwise
- **Date**: Use today's date
- **Context**: Problem description with specific file/module references
- **Decision**: What was decided and the rationale
- **Alternatives Considered**: At least 2 alternatives with reasons for rejection
- **Consequences**: Must contain ALL three subsections

### 5. Consequences — intellectual honesty requirement
The Consequences section MUST contain a **Negative** subsection. Every architectural decision has trade-offs. If you cannot identify a downside, you have not thought hard enough. This is non-negotiable.

### 6. Save the ADR
Save to `docs/adr/NNNN-title-kebab-case.md` where NNNN is the zero-padded number and the title is kebab-cased.

### 7. Validate
Run the validation script to ensure structural correctness:
```bash
bash ${CLAUDE_SKILL_DIR}/scripts/validate-adr.sh docs/adr/NNNN-title-kebab-case.md
```
If validation fails, fix the issues and re-validate.
