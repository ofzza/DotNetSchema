---
name: purge
description: Searches for and deletes unwanted (cache, OS garbage, etc.) files within the repository.
arguments: [silent]
---

# Purge unwanted files

1. Search through the repository for unwanted files belonging to any of the following categories:

- `*:Zone.Identifier` in any directory or subdirectory within the project repo.

2. When done, output:

- A list of files that were deleted.
- A summary of how many files were deleted based on each of the categories.

> If $SILENT argument was set, skip this step - output nothing

3. Ask for confirmation to delete all od the found unwanted files.

> If $SILENT argument was set, skip this step - don't ask for confirmation

4. If confirmation given, delete the found unwanted files.
