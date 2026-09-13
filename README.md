# Interview Prep API

A small real ASP.NET Core minimal API (C#) for browsing and adding interview-prep
questions, backed by Entity Framework Core + SQLite.

## Endpoints

- `GET /api/questions?category=` -- list questions, optionally filtered by category.
- `GET /api/questions/random?category=` -- one random question (optionally filtered);
  404 if nothing matches.
- `POST /api/questions` -- add a question (`category`, `question`, `difficulty`); 400 if
  a required field is blank.

The database is seeded with 4 real starter questions (System Design, Behavioral, SQL) on
first run if empty.

## Running it

```bash
cd src/InterviewPrepApi
dotnet run
```

```bash
curl http://localhost:5000/api/questions
curl -X POST http://localhost:5000/api/questions \
  -H "Content-Type: application/json" \
  -d '{"category":"Coding","question":"Reverse a linked list in place.","difficulty":"Medium"}'
```

## Testing

```bash
dotnet test
```

Real integration tests (`WebApplicationFactory<Program>` + a real Sqlite `:memory:`
connection kept open for the test host's lifetime, not a mock) covering listing,
filtering, the random-pick 404 case, creating a question, and validation.
