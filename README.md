# Interview Prep API

A real ASP.NET Core minimal API (C#) for interview-prep questions, backed by Entity
Framework Core + SQLite, with real spaced-repetition scheduling -- not just a CRUD
demo.

## Spaced repetition (SM-2)

Every question tracks its own scheduling state (`EaseFactor`, `IntervalDays`,
`Repetitions`, `NextReviewAt`) and gets rescheduled after each practice attempt using
SM-2, the same real algorithm behind Anki and most production spaced-repetition tools
(Piotr Wozniak, 1987). A self-rating of 1-2 out of 5 counts as a lapse and resets the
interval; 3+ grows it, with the growth rate itself increasing the more consistently a
question is recalled well. See `SpacedRepetition.cs` for the pure, directly
unit-tested implementation.

## Endpoints

- `GET /api/questions?category=` -- list questions, optionally filtered by category.
- `GET /api/questions/random?category=` -- one random question (optionally filtered);
  404 if nothing matches.
- `GET /api/questions/due` -- questions due for practice right now (never-practiced
  questions are always due).
- `POST /api/questions` -- add a question (`category`, `question`, `difficulty`); 400 if
  a required field is blank.
- `POST /api/questions/{id}/attempts` -- record a real practice attempt (`selfRating`
  1-5), reschedules the question via SM-2, returns the updated question. 404 for an
  unknown question, 400 for a rating outside 1-5.
- `GET /api/questions/{id}/attempts` -- real practice history for one question.
- `GET /api/stats` -- real aggregate progress: total attempts, questions mastered
  (3+ repetitions at a healthy ease factor), and a real current streak computed from
  actual distinct practice days, not a placeholder.

The database is seeded with 4 real starter questions (System Design, Behavioral, SQL) on
first run if empty.

## Running it

```bash
cd src/InterviewPrepApi
dotnet run
```

```bash
curl http://localhost:5000/api/questions/due
curl -X POST http://localhost:5000/api/questions/1/attempts \
  -H "Content-Type: application/json" \
  -d '{"selfRating": 4}'
curl http://localhost:5000/api/stats
```

## Testing

```bash
dotnet test
```

Real integration tests (`WebApplicationFactory<Program>` + a real Sqlite `:memory:`
connection kept open for the test host's lifetime, not a mock) covering listing,
filtering, the random-pick 404 case, creating a question, validation, the due-question
gate, attempt posting and history, and stats/streak accuracy -- plus a separate,
dependency-free unit-test suite for the SM-2 scheduling algorithm itself (first-review
interval, second-review interval, ease-factor growth on a good recall, the real 1.3
floor on repeated lapses, out-of-range rating validation).
