# Bowling scoreboard

A scoreboard for one player's game of ten-pin bowling, in C# / .NET 10: a small library with the rules
and the scoring, a command-line app to use it, and tests.

## Run it

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet test                                              # 55 tests
dotnet run --project src/Bowling.Cli                     # enter rolls one at a time
dotnet run --project src/Bowling.Cli -- 1 4 4 5 6 4 5 5 10 0 1 7 3 6 4 10 2 8 6
echo "10, 7, 3, 9, 0" | dotnet run --project src/Bowling.Cli   # rolls from standard input
```

The third command scores the example game from the brief:

```
+-----+-----+-----+-----+-----+-----+-----+-----+-----+-------+
|  1  |  2  |  3  |  4  |  5  |  6  |  7  |  8  |  9  |  10   |
| 1 4 | 4 5 | 6 / | 5 / | X   | - 1 | 7 / | 6 / | X   | 2 / 6 |
|   5 |  14 |  29 |  49 |  60 |  61 |  77 |  97 | 117 |   133 |
+-----+-----+-----+-----+-----+-----+-----+-----+-----+-------+
Total: 133 (final)
```

An invalid roll stops the app with exit code 1, after showing the scoreboard for the valid rolls before it:

```
Input: 3 4 7 [5] 2 1
             ^ roll 4
Error: roll 4 (5) is not valid. Only 3 pins are standing in frame 2, so 5 cannot be knocked down.
The app stopped. The scoreboard above shows rolls 1-3. Also not scored: rolls 5-6, because each roll after an invalid one could end up in the wrong frame.
Fix roll 4 and run again.
```

## Structure

```
src/Bowling/          the rules, no I/O
  Game.cs             validates and stores rolls; the public entry point
  FrameSplitter.cs    the one place that knows where a frame ends
  FrameRolls.cs       one frame's rolls: strike, spare, finished, pins standing
  Scorer.cs           pure function: rolls -> Scoreboard
  Scoreboard.cs       the result: frames with score and running total
src/Bowling.Cli/      the command-line app: parsing input and drawing the scoreboard
tests/Bowling.Tests/  xUnit tests for both
```

## Design decisions

**1. The rolls are the raw data; everything else is derived.** `Game` stores only the list of rolls. Frames,
scores and totals are computed from that list by a pure function (`Scorer`) every time they are read. There is no
second copy of the state (a "current frame" counter, a cached score) that could drift from the rolls, and the
same rolls always give the same scoreboard, so recomputing is always safe. This is the same principle I'd use in a
data pipeline: keep the raw data immutable and treat everything downstream as a reproducible view of it. A
correction means replaying the rolls, not patching a total.

**2. Validate at the boundary and fail fast.** Every roll is checked before it is stored: 0-10 pins, no more than
the pins standing, and nothing after the game is over. An invalid roll throws `InvalidRollException` and leaves the
game unchanged, so a `Game` always describes a legal game, possibly unfinished. Because of that guarantee, `Scorer` is
internal and doesn't re-check anything; the only way in is through `Game`. The messages name the frame and the pins
standing, because an error you can act on is worth more than "invalid data". I chose an exception over a result type
because it is idiomatic C#, and failing on the first bad roll is right here: the meaning of every later roll depends
on the earlier ones.

**3. One place knows where frames end.** `FrameSplitter` turns the flat list of rolls into frames (a strike is one
roll, otherwise two, and the 10th frame takes the rest). Validation and scoring both use it, so they can't disagree
about the rules. The 10th frame needed no special cases for its bonus rolls: `FrameRolls.PinsStanding` works like a
real lane (subtract each roll, reset the rack when all pins are down), and that covers 10-10-x, 10-3-7 and 5-5-10.

**4. A frame has no score until it is settled.** A strike or a spare waits for its bonus rolls, so its score and
running total are `null` until they arrive, exactly as in the brief's example table. The scoreboard never shows a
number that could still change. `Total` is the total of the settled frames.

**5. Two CLI modes, one set of rules.** Rolls given as arguments (or piped in) are scored, and the first invalid roll
stops the run with exit code 1, because a script needs a clear failure. It deliberately doesn't skip the bad roll and
carry on: every later roll would land in the wrong frame and give scores that look plausible but are wrong. The
report says what was scored, what wasn't, and points at the bad roll. In interactive mode, an invalid roll is
reported and you just try again. The modes differ only in how they *react* to an invalid roll; what counts as
invalid lives in `Game`. `App.Run` takes its input and output as parameters, so the tests run the whole CLI,
exit codes included, without starting a process.

**6. Performance: recomputing is the fast and simple choice.** A game has at most 21 rolls, so splitting and scoring
is a single pass over a tiny list. Caching would add state to keep in sync for no measurable gain.

## Testing

- **Examples from the brief** (gutter game, perfect game, the example table, 10, 10, 10 and 1, 9, 10 in the 10th
  frame) and the basic strike and spare rules.
- **Validation**: each rule, including the 10th frame's bonus rolls and "the game is unchanged after a rejected roll".
- **Games in progress**: strikes and spares waiting for their bonus show no score.
- **Randomized invariants**: 2,000 random valid games from a fixed seed (reproducible). After every roll it checks
  that the frames hold exactly the rolls, settled frames come first, running totals add up, and the total never
  decreases or exceeds 300. A failure prints the game's rolls.
- **CLI**: both modes, exit codes and the bowling notation.

To check that the tests can actually fail, I broke the code on purpose a few times (no rack reset in the 10th frame,
a strike bonus of one roll, an unsettled frame counted as 0) and confirmed that tests failed. The last one showed
that the invariant test missed a rule (unsettled frames must have no running total), so I added it.

## Alternatives I considered

- **A players x 21 matrix** of rolls (my first sketch). It needs empty slots after strikes and a special width for
  the 10th frame; a flat list stores only what actually happened.
- **Tracking the current frame and roll as fields.** Faster to read, but it is a second source of truth next to the
  rolls; see decision 1.
- **The classic kata shape** (`Roll` + `Score()` returning an int). Simpler, but it can't show a frame-by-frame
  scoreboard, which the brief's table suggests.
- **A property-testing library** (FsCheck). It shrinks failures to the smallest case, but a seeded loop with
  hand-written invariants did the job without another dependency.

## Out of scope, and what I would do next

The brief asks for one player's scoreboard, so I kept to that and spent the time on correctness instead of features.

- **1-4 players.** The design allows it without touching the rules: a `Match` holds one `Game` per player and
  decides whose turn it is (each player rolls a whole frame, then the next player). The scoreboard gets one row per
  player.
- **Batch mode.** Score many games from a file, report the invalid ones and carry on with the rest, instead of
  stopping at the first error. It is the same stop-or-warn decision as in the CLI, made per game.
- **Machine-readable output** (CSV or JSON frame by frame) if another system needed the scores.
- **CI**: a GitHub Actions workflow running `dotnet test` on every push.

## Use of AI

I built this with Claude Code as a pair programmer, which was agreed in advance. I made the design decisions
above, reviewed the code and can walk through any part of it.
