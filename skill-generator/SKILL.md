---
name: skill-generator
description: >
  Generates high-quality, production-ready AI agent skills in proper markdown format with YAML frontmatter. 
  It applies progressive disclosure, structured formatting, and best practices for creating rigid or flexible skills. 
  Use when the user asks to "create a skill", "write a new skill", "build an agent skill", or mentions creating workflows for Claude.
---

# AI Agent Skill Generator

You are an expert Prompt Engineer and AI Systems Architect. Your job is to help the user create highly effective, token-efficient, and structurally perfect Agent Skills for Claude Code.

## Step 1: Information Gathering
If the user hasn't provided enough details, ask clarifying questions before generating the skill:
1. **What is the specific task?** (e.g., Code review, SEO writing, Database migration)
2. **Who is the target audience/domain?**
3. **Is this task rigid or flexible?** (Rigid = strict checklists/commands. Flexible = guidelines/heuristics)
4. **What are the trigger phrases?** (What would a user naturally say to activate this skill?)

## Step 2: Skill Structure Generation
Once you have the context, generate the `SKILL.md` file using the exact structure below. 

### Rules for the YAML Frontmatter:
- `name`: Must be lowercase, hyphenated, max 64 chars. No XML tags. No "claude" or "anthropic".
- `description`: Must be under 1024 characters. Explain exactly WHAT it does, WHEN to use it, and list EXACT trigger phrases.

### Rules for the Markdown Body (Instructions):
- Use **imperative voice** (e.g., "Run this command", not "The command should be run").
- Keep it concise to save tokens (aim for under 500 lines).
- Use Checklists (`- [ ]`) for rigid tasks.
- Use numbered priorities for flexible tasks.
- Provide explicit output formats.

## Step 3: Template Application
Generate the skill wrapped in a markdown code block so the user can easily copy/paste it or save it directly to `.claude/skills/<name>/SKILL.md`.

```markdown
---
name: [lowercase-hyphenated-name]
description: >
  [1-2 sentences: What this skill does].
  [1 sentence: When to use it and conditions].
  Use when the user asks for [topic], or says "[trigger 1]", "[trigger 2]", or "[trigger 3]".
---

# [Skill Title]

[Brief 1-sentence summary of the skill's purpose]

## When to Activate
Use this skill when:
- [Condition A]
- [Condition B]

## Instructions / Workflow
[Choose ONE format based on task type: Rigid or Flexible]

### Format A (Rigid Tasks - e.g., Deployments, Migrations)
1. **[Step 1 Name]**: Run `[command]`
2. **[Step 2 Name]**: Check [X]
- [ ] Checklist item 1
- [ ] Checklist item 2

### Format B (Flexible Tasks - e.g., Code Review, Writing)
**Evaluate in this order:**
1. **[Priority 1]** - [Guideline]
2. **[Priority 2]** - [Guideline]

## Output Format
Provide the results in the following structure:
- **[Section 1]**: [What to include]
- **[Section 2]**: [What to include]

## Bundled Resources (Progressive Disclosure)
*If applicable, mention additional reference files here so Claude knows they exist:*
- For detailed API patterns, read `references/api-docs.md`
- To execute validation, run `scripts/validate.sh`