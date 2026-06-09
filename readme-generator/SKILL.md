---
name: comprehensive-readme-generator
description: >
  Analyzes project architecture, inspects core files, and generates an enterprise-grade, highly detailed README.md with Mermaid diagrams.
  Use this when starting to document a new or existing codebase comprehensively.
  Use when the user asks to "write a readme", "generate a detailed readme", "document the project", or says "create documentation".
---

# Comprehensive README Generator

This skill thoroughly scans a project's directory, extracts deep technical nuances from source files, and writes a massive, professional `README.md` file suitable for enterprise projects.

## When to Activate
Use this skill when:
- The user wants to document a completely new or complex multi-service project.
- An existing README is too short and needs an extreme level of detail (e.g., 300+ lines).
- The user asks to visualize the system architecture using Mermaid diagrams inside the README.

## Instructions / Workflow
**Evaluate in this order:**

1. **[Reconnaissance]** - Use directory listing and file viewing on core configuration files (e.g., `docker-compose.yaml`, `package.json`, `.csproj`, `requirements.txt`) to identify the foundational technology stack.
2. **[Deep Dive Inspection]** - Explicitly read into complex logic files (e.g., AI pipelines, background workers, core API routing, data crawlers) to extract actionable technical metrics (like model names, dimensions, framework versions, or database schemas).
3. **[Architecture Visualization]** - Formulate robust Mermaid.js diagrams:
   - System Architecture (Microservices, DBs, Caches).
   - Sequence or Flowchart diagrams for specific complex subsystems (e.g., AI/RAG flow, ML training pipeline, auth flow).
4. **[Drafting the README]** - Construct the document using the rigorous structure below in professional English (unless instructed otherwise). Ensure it is extremely detailed and avoids generic summaries.
5. **[Execution]** - Write the final output directly to the root `README.md` file.

## Output Format
Provide the results in the `README.md` file following this exact structure:
- **Executive Summary**: 1-2 paragraphs defining the project's core capabilities.
- **High-Level System Architecture**: Mermaid diagram showing all interconnected services.
- **Subsystem Deep Dives**: Detailed explanations of each frontend, backend, or worker, including specific technologies used and their roles.
- **Complex Feature Highlights**: (If applicable) Deep technical analysis of AI, Big Data, or unique algorithms found in the codebase. Include Mermaid flowcharts here.
- **Technology Stack Matrix**: A clean markdown table of all tools.
- **Detailed Directory Structure**: A tree-like representation of the codebase.
- **Setup & Installation Guide**: Step-by-step commands to run the project locally (e.g., Docker commands, npm installs, environment vars).

## Bundled Resources (Progressive Disclosure)
- Always check `docker-compose.yaml` or equivalent infra files to understand dependencies (Kafka, Redis, ElasticSearch).
- Always inspect dependency manifest files (`package.json`, `requirements.txt`, `.csproj`) for library versions and framework insights.
