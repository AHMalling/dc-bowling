# Bowling scoreboard

A scoreboard for one player's game of ten-pin bowling in C# / .NET 10: a library with the rules, a small CLI to
exercise it, and tests.

## Run it

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet test                                                    # run the tests
dotnet run --project src/Bowling.Cli -- 1 4 4 5 6 4 5 5 10 0 1 7 3 6 4 10 2 8 6
echo "10, 7, 3" | dotnet run --project src/Bowling.Cli         # rolls from standard input
```

With no arguments and nothing piped in, the CLI prints its usage.

The second command scores the example game from the brief:

```
+-----+-----+-----+-----+-----+-----+-----+-----+-----+-------+
|  1  |  2  |  3  |  4  |  5  |  6  |  7  |  8  |  9  |  10   |
| 1 4 | 4 5 | 6 / | 5 / | X   | - 1 | 7 / | 6 / | X   | 2 / 6 |
|   5 |  14 |  29 |  49 |  60 |  61 |  77 |  97 | 117 |   133 |
+-----+-----+-----+-----+-----+-----+-----+-----+-----+-------+
Total: 133 (final)
```

An invalid roll stops the app with exit code 1, after showing the scoreboard for the rolls before it:

```
Error: roll 4 (5) is not valid. Only 3 pins are standing in frame 2, so 5 cannot be knocked down. The app stopped there, so the scoreboard only shows the rolls before it.
```

```
src/Bowling/           the rules: a library with no I/O; Game is the entry point
src/Bowling.Cli/       the command-line app
tests/Bowling.Tests/   xUnit tests
```

## Design

**The rolls are the raw data; everything else is derived.** `Game` stores only the list of rolls. Frames and scores
are computed from it by a pure function every time they are read, so there is no second copy of the state that
could drift, and the same rolls always give the same scoreboard. It is the principle I'd use in a data pipeline:
keep the raw data immutable, treat everything downstream as a reproducible view of it, and fix an error by
replaying the data, not by patching a total.

**Validate at the boundary and fail fast.** `Game.Roll` checks every roll before storing it (0-10 pins, no more
than the pins standing, nothing after the game is over) and throws `InvalidRollException` with a message that says
why. A `Game` is therefore always a legal game, and the scorer doesn't check again. The CLI stops at the first
invalid roll instead of skipping it, because every later roll would land in the wrong frame and give scores that
look plausible but are wrong.

**Validation and scoring share one definition of the rules.** `FrameSplitter` cuts the rolls into frames, and `FrameRolls` knows strike, spare,
finished and pins standing. Validation and scoring both use them, so they can't disagree. A strike or spare has no
score (`null`) until its bonus rolls arrive, so the scoreboard never shows a number that could still change.

**The CLI is just one way in.** The library has no I/O, so a web API or a batch job could use the same `Game`.
Everything except `Game`, its results and the `Rules` constants is `internal`, so every caller goes through its validation; the compiler
enforces that.

**Performance.** A game has at most 21 rolls, so recomputing from the rolls (O(n²) with n ≤ 21) costs next to
nothing, and there is no cache to keep in sync. At scale the unit of work is one game: run the pure scorer per game,
in parallel.

## Scope

I kept to the brief: one player, and a CLI that scores a whole game in one command, so it is easy to check against
a known score. `Game` has three public members (`Roll`, `Rolls` and `Scoreboard`), and there are no packages
beyond the xUnit test template. Further features are under Next steps.

## Tests

The brief's examples, each validation rule, games in progress, and the CLI in-process. A randomized test plays
2,000 games from a fixed seed and reconciles each one against an independent reference scorer (the classic
index-walking loop) that shares no code with the library. I broke the code on purpose (no rack reset, a wrong
strike bonus, a validation bug) to check that the tests fail.

## Next steps

- **1-4 players:** a `Match` holding one `Game` per player; the rules don't change.
- **An interactive UI:** a CLI mode that takes one roll at a time, or a web page, on top of the same `Game`.
- **Batch mode:** score a file of games, set invalid games aside and carry on, instead of stopping.
- **CSV/JSON output** and **CI** running `dotnet test` on every push.

## Use of AI

I built this with Claude Code as a pair programmer. I made the design decisions. Review rounds with other models
found real issues, such as a weakened random test and features beyond the brief, and I decided what to change. I
can explain the code and the choices behind it.
