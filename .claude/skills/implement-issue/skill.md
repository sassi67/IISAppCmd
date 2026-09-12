---
name: implement-issue
description: Implement a GitHub issue end-to-end — branch, code, tests, commit, push, PR.
---

# Implement Issue

Given a GitHub issue number or URL:

1. Fetch the issue with `gh issue view <number>`.
2. Create and switch to a new branch (see CLAUDE.md naming convention if present, otherwise `issue-<number>-<slug>`).
3. Implement exactly what the issue describes.
4. Write unit tests for the new/changed behavior.
5. Run the project's test suite locally. If anything fails, fix it and re-run until green.
6. Commit the changes with a message referencing `number`.
7. Push the branch to origin.
8. Open a pull request with `gh pr create`, summarizing the change and linking the issue.

Report back with the PR URL when done.
