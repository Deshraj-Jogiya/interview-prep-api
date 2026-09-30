# Interview Prep API

[![ci](https://github.com/Deshraj-Jogiya/interview-prep-api/actions/workflows/ci.yml/badge.svg)](https://github.com/Deshraj-Jogiya/interview-prep-api/actions/workflows/ci.yml) ![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)

An ASP.NET Core minimal API for practising interview questions with spaced repetition: each question is rescheduled with the SM-2
algorithm after every self-rated attempt, backed by Entity Framework Core and SQLite.

## Highlights

- **SM-2 scheduling** (`SpacedRepetition.cs`): ease factor, interval and repetition count per question; a rating of 1–2 resets the
  interval, 3+ grows it. Unit-tested on its own.
- **Endpoints:** list and filter questions, pick a random one, list the ones due now, add a question, record an attempt, read a question's
  history, and `GET /api/stats` (attempts, questions mastered, current streak from distinct practice days).
- **Integration tests** with `WebApplicationFactory` and an in-memory SQLite connection held open for the test host, plus a curl check
  against the running app.

## Architecture

```mermaid
flowchart LR
    C[client / mobile app] -- HTTP --> API[ASP.NET Core minimal API]
    API --> SR[SpacedRepetition (SM-2)]
    API --> EF[EF Core DbContext]
    EF --> DB[(SQLite)]
```

## Stack

C#, .NET, ASP.NET Core, Entity Framework Core, SQLite, xUnit, GitHub Actions.

## Run it

```bash
cd src/InterviewPrepApi && dotnet run
curl http://localhost:5000/api/questions/due
curl -X POST http://localhost:5000/api/questions/1/attempts -H "Content-Type: application/json" -d '{"selfRating": 4}'
curl http://localhost:5000/api/stats
dotnet test            # from the repository root
```

Companion app: [interview-prep-mobile](https://github.com/Deshraj-Jogiya/interview-prep-mobile).
