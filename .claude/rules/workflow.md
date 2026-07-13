# Workflow

- Make minimal, focused changes; do not refactor unrelated code in the same change.
- One logical change per commit, with an imperative-mood message explaining *why*.
- When unsure between two designs, present both with trade-offs and ask before implementing.
- Never commit or push directly to the default branch (`master`/`main`). All work happens on a `feature/graph-library-x.y.z` branch where x.y.z is next version's semantic versioning.
- Milestones are used to track the progress of the project.
- Each new functionality is built milestone by milestone. The first milestone in a new feature branch has the number following last commit's last milestone in the master branch.
- The branch lands on the default branch only through a reviewed pull request. Enforce this with branch protection (require a PR, disallow direct pushes, require the squash-merge strategy) so it can't be bypassed by accident.
- Merge by squash so each feature arrives as a single logical commit on the default branch, consistent with the "one logical change per commit" rule above; the squash commit message keeps the imperative, why-focused form.
- Update this file when a new convention or correction is established.
