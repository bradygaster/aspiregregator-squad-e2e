# Meet Your Squad 🕵️

**Universe:** The Usual Suspects

Your Squad for **aspiregregator-squad-e2e** — Aspiregregator, an RSS reader built with .NET Aspire, Blazor, and Microsoft Orleans.

## The Team

| Name | Role | Specialty | How to talk to them |
|------|------|-----------|----------------------|
| Keaton | Lead / Architect | Aspire orchestration, Orleans clustering topology, shared contracts (`Abstractions/`) | `Keaton, review the architecture for X` |
| McManus | Backend Engineer | Orleans grains (`Grains/`), feed ingestion worker (`FeedUpdater/`) | `McManus, fix the grain state issue` |
| Fenster | Frontend Engineer | Blazor components & view models (`Frontend/`) | `Fenster, update the entry list UI` |
| Hockney | Test Engineer | Test coverage & test infra (none exists yet — first job) | `Hockney, write tests for X` |
| Kint | DevOps / Platform | CI/CD (`.github/workflows/`), `azure.yaml` deployment | `Kint, fix the build pipeline` |

## Always-On Support

| Name | Role | What they do |
|------|------|---------------|
| Scribe | Session Logger | Records decisions and orchestration history — never blocks work. |
| Ralph | Work Monitor | Keeps the work queue moving; say "Ralph, go" to run continuously. |
| Rai | RAI Reviewer | Responsible AI review — flags safety/ethics issues before ship. |
| Fact Checker | Verifier / Devil's Advocate | Verifies claims and challenges risky assumptions before ship. |

## How to Work With Your Squad

Assign work by labeling GitHub issues `squad:{name}` (e.g. `squad:keaton`, `squad:mcmanus`) — label color `9B8FCC`. The Lead (Keaton) triages new `squad` labeled issues and assigns the right specialist.

Useful commands:
- `/squad status` — check team & lifecycle status
- `/squad research` — kick off research on an issue
- `/squad plan` — produce a plan
- `/squad implement` — dispatch implementation work

See `.squad/routing.md` for the full domain → agent routing table.

## What Happened Here

Squad analyzed the repository and found:
- **Languages/Stack:** C# / .NET, Blazor (Frontend), Microsoft Orleans (Grains), .NET Aspire (AppHost orchestration), Azure Storage-backed clustering, `azd`/`azure.yaml` deployment.
- **Structure:** Solution split into `AppHost` (Aspire orchestration), `Abstractions` (shared contracts), `Grains` (Orleans actors), `FeedUpdater` (background worker), `Frontend` (Blazor UI), `Defaults` (shared build extensions).
- **CI/CD:** GitHub Actions workflows present (`build.yml`, `pr-validation.yml`) — routed to Kint.
- **Tests:** No test project currently exists — Hockney's first task is establishing a testing foundation.
- **Rationale:** A 5-specialist team was composed (Lead + Backend/Orleans + Frontend/Blazor + Test + DevOps) to match the distinct architectural layers of the Aspire solution. The Usual Suspects universe (capacity 6) fits a 5-agent roster with minimal waste.

---
_Cast on 2026-08-19._
