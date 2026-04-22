# Roadmap

Status: Target

This document describes the phased delivery plan for the TimeForCode platform. Each phase builds on the previous one and results in a usable, deployable increment.

---

## Principles

- Each phase delivers working, testable, deployable software — not just documentation.
- Architecture tests are required for every new bounded context before the phase is considered done.
- Documentation in `docs/current/` must be updated to match what is implemented at the end of each phase.

---

## Phase 1 — Authentication and Project Discovery (Current)

**Goal**: Users can sign in and browse registered open-source projects.

**Scope**:
- GitHub OAuth 2.0 login, JWT issuance, and token refresh. ✅
- Project registration endpoint (submit GitHub URL, fetch metadata, persist). ⚠️
- Admin approval workflow for submitted projects.
- Project listing and search API.
- Basic website UI: login, project list, project detail page.

**Definition of Done**:
- A user can log in with their GitHub account.
- A maintainer can submit a project by GitHub URL.
- An admin can approve or reject the project.
- Approved projects appear in a searchable public listing.
- All endpoints covered by integration tests.

---

## Phase 2 — Donations and Hour Tracking

**Goal**: Organisations can pledge hours and contributors can log time against those pledges.

**Scope**:
- Donor organization registration.
- Individual contributor registration and skill profile.
- Donation pledge creation and lifecycle management (Pending → Active → Completed / Cancelled).
- Hour transaction logging against active donations.
- Progress tracking (hours logged vs committed).
- Website UI: donation form, contributor dashboard, donation status view.

**Definition of Done**:
- An organisation can register and pledge hours to an approved project.
- A contributor can be assigned to a donation and log hours.
- The platform calculates and displays progress.
- A donation auto-completes when all pledged hours are logged.
- Full test coverage for the Donation bounded context.

---

## Phase 3 — Reporting and Impact

**Goal**: Donors and maintainers can see transparent, verifiable records of contributions.

**Scope**:
- Organization impact report (hours pledged vs logged, projects supported).
- Project donation report (hours received, contributors, progress).
- Contribution history export.
- Badges and milestone recognition for organisations.
- Public-facing impact statistics.

**Definition of Done**:
- An organisation can view and export their full contribution record.
- A maintainer can see all donations received and hours logged.
- Badges are awarded automatically at contribution milestones.

---

## Phase 4 — Matchmaking and Campaigns

**Goal**: The platform actively suggests the right projects to the right donors.

**Scope**:
- Skill-based and technology-based project suggestions for donors.
- Donor suggestions for project maintainers.
- Admin-curated campaigns (themed groups of projects).
- Spotlight and featured project sections on the website.

**Definition of Done**:
- The platform surfaces relevant project recommendations to authenticated donors.
- Administrators can create and manage campaigns.
- Featured projects appear prominently on the homepage.

---

## Phase 5 — Community Features

**Goal**: Build a community layer that encourages sustained engagement.

**Scope**:
- Success stories: maintainers can publish outcomes from completed donations.
- Forum or discussion area for donors and maintainers.
- Public leaderboard for top contributing organisations.
- Notification system for key events (donation activated, hours logged, milestone reached).

**Definition of Done**:
- Maintainers can publish and share success stories.
- A public leaderboard displays top donors.
- Users receive in-platform notifications for key events.
