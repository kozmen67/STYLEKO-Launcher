# Code signing policy

Free code signing provided by SignPath.io, certificate by SignPath Foundation.

## Scope

This policy applies to official STYLEKO Launcher release binaries built from this repository.

Only binaries produced by the repository's automated GitHub Actions workflow are eligible for signing. The previously distributed closed-source `Launcher.exe` is not a signing input and is not covered by this policy.

## Team roles

- Authors / committers: [@kozmen67](https://github.com/kozmen67)
- Reviewers: [@kozmen67](https://github.com/kozmen67)
- Signing approver: [@kozmen67](https://github.com/kozmen67)

Contributions from users who are not committers require review before merge. Each release signing request requires manual approval by the signing approver.

## Build origin

Official release binaries must be built from source using GitHub-hosted GitHub Actions runners.

The build workflow and all source files used to produce the launcher are maintained in this repository. Signing requests must reference the GitHub artifact created by that workflow so SignPath can verify origin metadata.

## Artifact identity

The signed application is:

- Product name: STYLEKO Launcher
- Platform: Windows x86
- Release binary: `Launcher.exe`
- Version: defined consistently in the project metadata for each release

Third-party or proprietary game binaries are not signed using this project's SignPath Foundation subscription.

## Privacy

See [PRIVACY.md](PRIVACY.md).

## Security requirements

Maintainers responsible for source changes, reviews, and signing approvals must use multi-factor authentication for GitHub and SignPath accounts.

## Release approval

Every production signing request requires manual approval. Test builds and unsigned validation artifacts are not represented as signed releases.
