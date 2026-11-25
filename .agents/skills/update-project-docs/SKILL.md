




---
name: update-project-docs
description: >-
  Keeps PROJECT_DOCUMENTATION.md continuously synchronized with the codebase and
  conversation decisions. Use this skill whenever code changes are implemented,
  a bug is fixed, an open audit item is resolved, a new feature/integration/config
  is added, or a new technical issue/architectural decision is discussed in this
  repository.
---

# Maintain `PROJECT_DOCUMENTATION.md`

Use this procedure to keep `PROJECT_DOCUMENTATION.md` at the repository root accurate and up to date as the project evolves.

## When to Update

Update `PROJECT_DOCUMENTATION.md` at the end of a task (before replying to the user) whenever any of the following occurred:

1. **An open issue from Section 7 was resolved** (Security, OOP, React, or Scalability).
2. **A bug was fixed or a workaround/diagnosis was completed** (even if it wasn't previously listed in Section 7).
3. **A new feature, endpoint, migration, UI behavior, integration, or configuration key was added or changed**.
4. **A new technical problem, risk, or architectural decision was identified** during discussion or code review.

Skip updating the file for pure read-only Q&A turns where nothing changed and no new architectural decision or issue was uncovered.

## Update Rules by Section

Read only the relevant sections of `PROJECT_DOCUMENTATION.md` before editing, then apply targeted edits:

1. **Resolving an Open Issue (Section 7 → Section 6)**
   - **Remove** the resolved row from the corresponding table in **Section 7 (`Auditoria Técnica — Problemas Ainda em Aberto e Melhorias Recomendadas`)**.
   - If all rows in a subsection table are resolved, replace the table with a short note stating there are no open items in that category.
   - **Add** a numbered entry to **Section 6 (`Problemas Encontrados e Soluções Já Implementadas`)** following the existing format:
     - Bold title summarizing the issue/requirement.
     - `**Problema:**` concise description of what was wrong or missing.
     - `**Solução:**` concrete summary of what was changed (referencing the real files/classes/migrations involved).

2. **Adding New Features, Flows, or Integrations**
   - **Section 2 (`Estrutura do Projeto`):** add any new key files, services, controllers, views, or EF Core migrations.
   - **Section 3 (`Modelo de Domínio e Papéis`):** update enums, roles, or domain rules if changed.
   - **Section 4 (`Fluxo Completo da Aplicação`):** update the text and the Mermaid flowchart if authentication, campaign states, or background lifecycle rules changed.
   - **Section 5 (`Integrações Externas e Configuração`):** add or update required/optional configuration keys (never write real secret values or tokens into the doc).
   - **Section 8 (`Execução Local e Deploy com Base de Dados Partilhada`):** update commands or deployment instructions if setup requirements changed.

3. **Recording Newly Discovered Problems**
   - Add a new row to the appropriate table in **Section 7** (`7.1. Segurança`, `7.2. Programação Orientada a Objetos`, `7.3. Boas Práticas de React`, or `7.4. Escalabilidade`) with **Problema**, **Onde está**, and **Solução Recomendada**.

## Quality Checklist

Before finishing the update:
- Write in **Portuguese (PT-PT)** matching the tone and structure of `PROJECT_DOCUMENTATION.md`.
- Never include real passwords, API tokens, or connection string secrets.
- Only document changes that are actually present in the workspace or explicitly agreed in the conversation.
- Mention briefly in your response to the user that `PROJECT_DOCUMENTATION.md` was updated.
