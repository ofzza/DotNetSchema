---
name: publish
description: Publishes the package to NUGET
---

# Publish to NUGET

1. Run all pre-publishing checks as outlined in PUBLISH.md, especially making sure to (re)run all the tests and (re)build the package before inspecting.

2. Check if the version to be published is different from latest already published version and if not, suggest a next version, or allow me to enter the next version.
   If I agree to your next version proposal or enter my own, update the version for all the projects in the repository.

3. Ask me for a NUGET API key (which you'll need to publish)

4. Publish to NUGET

5. Report on success/failure of npm publish and if successful:

- Tag the current commit with the version number you just published as

- Ask me if I'd like to commit changes. If I do want to commit, use the `commit` skill.
