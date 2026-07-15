---
name: Milestone
about: Track a single feature milestone (M<n>) from red-test to merged
title: "M<n>: <feature in imperative mood>"
labels: milestone
---

## Milestone
**M<n>** · target version **x.y.z** · branch `feature/graph-library-x.y.z`

## Why
<The motivation — what capability or gap this closes. The "why" the
CHANGELOG deliberately doesn't carry.>

## Scope
<What's in. One coherent, squash-mergeable logical change.>

- [ ] <deliverable>
- [ ] <deliverable>

Out of scope: <what's deliberately deferred, to keep the change focused.>

## Public API surface
<New public members, or "none / internal only".>

- [ ] `PublicAPI.Unshipped.txt` updated for every new public member

## Test plan (TDD — red first)
<The behaviors to specify before implementing.>

- [ ] Happy path: <…>
- [ ] Edge cases: null/empty, boundary values, invalid state
- [ ] Failure paths: exceptions / cancellation (`CancellationToken`)
- [ ] Added to the relevant contract suite(s) if this is a new graph type
      (`GraphContractTests` / `SimpleGraphContractTests` / `MultigraphContractTests`)

## Definition of done
- [ ] `dotnet build Graph1x.sln` — zero warnings (`TreatWarningsAsErrors`)
- [ ] `dotnet test Graph1x.sln` — green on net8.0 + net10.0
- [ ] `dotnet format` clean
- [ ] CHANGELOG.md updated under the target version
- [ ] README.md + README.nuget.md updated (feature reflected)
- [ ] Closed by PR via `Closes #<this>`
