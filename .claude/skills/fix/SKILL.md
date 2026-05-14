# SKILL: Fix Issue

## Overview

This skill is used to fix a specified issue based on a local issue document.

## Usage

```bash
/fix <issue-key>
```

Example:

```bash
/fix issue-1
```

Where:

- `<issue-key>` is the issue identifier provided by the user.
- The corresponding issue document must exist under the `issues/` directory.

For example:

```text
issues/issue-1.md
```

---

## Workflow

### 1. Read Issue Document

Read the issue description document from the local repository:

```text
issues/<issue-key>.md
```

Example:

```text
issues/issue-1.md
```

If the document does not exist:

- Stop execution immediately.
- Return an error indicating that the issue document was not found.

---

### 2. Read Development Documentation

Attempt to read the development guide:

```text
DEVELOPMENT.md
```

Behavior:

- If the file exists, follow the development conventions and instructions.
- If the file does not exist, continue execution without error.

---

### 3. Create Fix Branch

Create a new git branch for the fix.

Recommended branch naming format:

```text
fix/<issue-key>
```

Example:

```bash
git checkout -b fix/issue-1
```

---

### 4. Implement the Fix

Complete the required changes based on:

- The issue document
- Existing project structure
- Development guidelines (if available)

Requirements:

- Ensure the project builds successfully if applicable.
- Ensure all unit tests pass.

---

### 5. Commit the Changes

Create a git commit for the completed fix.

Example:

```bash
git add <changed-files>
git commit -m "<description>"
```

---

## Failure Conditions

Execution should stop if:

- The specified issue document does not exist.
- Required project files are missing and prevent implementation.
- The fix cannot be completed safely.

---

## Notes

- Only modify files related to the issue.
- Preserve existing coding style and conventions.
- Prefer small, reviewable commits.
- Do not push changes automatically unless explicitly requested.