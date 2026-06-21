# Contributing to TimeForCode

We welcome contributions to **TimeForCode**! Please take a moment to review `
these guidelines to ensure a smooth and collaborative contribution process.

---

## Code of Conduct

By participating in this project, you agree to abide by our
 [Code of Conduct](CODE_OF_CONDUCT.md). Please be respectful and constructive
 in your interactions with others.

---

## How to Contribute

### 1. Reporting Issues

If you encounter a bug or have a feature request, please
 [open an issue](https://github.com/wouterfennis/TimeForCode/issues) and
provide as much detail as possible, including:

- Steps to reproduce the issue (if applicable).
- Expected and actual behavior.
- Screenshots or logs (if relevant).

### 2. Submitting Code Changes

1. **Fork the repository**: Click the "Fork" button at the top of
 the repository page.
2. **Clone your fork**:

   ```bash
   git clone https://github.com/wouterfennis/TimeForCode.git
   ```

3. **Create a new feature branch**:

   ```bash
   git checkout -b feature/AmazingFeature
   ```

4. **Commit your changes**:

   ```bash
   git commit -m 'Add some AmazingFeature'
   ```

5. **Push to the branch**:

   ```bash
   git push origin feature/AmazingFeature
   ```

6. **Open a Pull Request**: Go to the repository on GitHub and click
 the "New Pull Request" button.

### 3. Before Submitting Your Pull Request

- [x] Your branch passes the automated tests and checks.
- [x] Your code adheres to the project's style guide.
- [x] You have added or updated documentation for any new functionality or
 endpoints.
- [x] You have added or updated tests for your changes.

### 4. Development Setup

Install the following tools before starting:

| Tool | Version | Purpose |
| --- | --- | --- |
| [.NET SDK](https://dotnet.microsoft.com/download/dotnet/10) | 10.x | Build and test |
| [Podman](https://podman.io/docs/installation) | 4.x+ | Run the full stack locally |

> **Why Podman?** Docker Desktop requires a paid licence in many organisations. Podman is a free, daemonless alternative that is fully compatible with Docker Compose files. The Dockerfiles in this project already use a non-root `appuser`, which is best-practice for Podman rootless mode.

**Start the full stack:**

```powershell
.\scripts\start-local.ps1
```

> On Windows this script automatically starts the Podman machine if it is not already running, so you do not need to run `podman machine start` manually before each session.

**Verify everything works:**

```powershell
.\scripts\smoke-test.ps1
```

Expected output: `Result: PASSED (17/17)`

### 5. Development Guidelines

Code style

- Follow the style guide for consistent code formatting.
- Use meaningful commit messages.

Testing

- Write unit tests for new features or bug fixes.
- Run tests locally before pushing changes:

```powershell
dotnet test
```

Documentation

- Update relevant documentation in the `docs/` folder for any new
 features or changes.
- Use clear and concise language.

### 6. How to Run Tests Locally

**Run all tests (including E2E / browser tests):**

> Note: This runs `TimeForCode.Website.Specifications` (@E2E) and requires the full Docker Compose stack + Playwright (see below).

```powershell
# From the repository root
dotnet test TimeForCode.sln
```

**Run tests for a single project:**

```powershell
# Replace the path with the project you want to target
dotnet test tst/Donation/TimeForCode.Donation.Specifications/TimeForCode.Donation.Specifications.csproj
```

**Run only non-E2E tests (as CI does):**

```powershell
dotnet test TimeForCode.sln --filter "TestCategory!=E2E"
```

**Run E2E / browser tests (Website.Specifications):**

E2E tests require the full Docker Compose stack. Start it first, then run the tests:

```powershell
# 1. Start the full stack
.\scripts\start-local.ps1

# 2. Install the Playwright browser binaries (first time only)
pwsh tst/Website/TimeForCode.Website.Specifications/bin/Debug/net10.0/playwright.ps1 install chromium

# 3. Run the E2E suite
dotnet test tst/Website/TimeForCode.Website.Specifications/
```

See `tst/Website/TimeForCode.Website.Specifications/README.md` for full setup details.

## Need Help?

If you have any questions or need assistance, feel free to reach out by
opening an issue or joining our community discussions.

Thank you for contributing to **TimeForCode**!
