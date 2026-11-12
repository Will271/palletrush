# 08 - Proposed management UI

**Related docs:** [Index](./00-index.md) · [01 - Architecture](./01-architecture.md) · [02 - Event flow](./02-event-flow.md) · [03 - Cone hit lifecycle](./03-cone-hit-lifecycle.md) · [09 - Actual screens](./09-actual-screens.md) · [10 - Wireframes](./10-wireframes.md)

This document describes a better management UI for PalletRush, so organizers can create heats, register drivers, and run events without clicking “Simulate run” one by one.

## Current problem

Today the only way to start a run is the **Simulate run** button in the Angular web app. It is fine for testing, but it does not scale for a real event:

- You must remember `heatId` and `driverId` by heart.
- You can only simulate one run at a time.
- There is no heat setup screen.
- There is no driver management screen.
- There is no bulk registration.
- There is no “next driver, go” flow for a live event.

## Proposed screens

```mermaid
flowchart TD
    Login[Login / home]
    HeatMgmt[Heat management]
    DriverMgmt[Driver management]
    Reg[Registration]
    Live[Live event screen]
    Leaderboard[Leaderboard]
    OnCourse[On-course monitor]

    Login --> HeatMgmt
    Login --> DriverMgmt
    Login --> Reg
    Login --> Live
    Login --> Leaderboard
    Login --> OnCourse

    HeatMgmt --> HeatCreate[Create / edit heat]
    HeatMgmt --> HeatList[List of heats]
    DriverMgmt --> DriverCreate[Add / import driver]
    DriverMgmt --> DriverList[List of drivers]
    Reg --> RegBulk[Bulk register drivers to heat]
    Reg --> RegGrid[Slot grid view]
    Live --> LiveStart[Pick driver and start run]
    Live --> LiveNext[Auto-queue next driver]
    Live --> LiveSim[Simulate run]
    Live --> LiveManual[Record cone / finish manually]
    Live --> OnCourse
    Leaderboard --> LeaderboardHeat[Heat leaderboard]
```

## Proposed flow

```mermaid
sequenceDiagram
    actor O as Organizer
    participant AW as Angular web :4200
    participant LA as League API :5100
    participant TA as Telemetry API :5200
    participant R as Redis<br/>course-events stream
    participant DB as PostgreSQL

    O->>AW: Open heat management
    AW->>LA: GET /api/heats
    LA-->>AW: List of heats
    O->>AW: Create heat
    AW->>LA: POST /api/heats { name, max slots }
    LA->>DB: INSERT INTO heats
    LA-->>AW: New heat

    O->>AW: Open driver management
    AW->>LA: GET /api/drivers
    LA-->>AW: List of drivers
    O->>AW: Add driver
    AW->>LA: POST /api/drivers { name, car }
    LA->>DB: INSERT INTO drivers
    LA-->>AW: New driver

    O->>AW: Open registration
    AW->>LA: GET /api/heats/{id}/registrations
    LA-->>AW: Current slots
    O->>AW: Register driver to heat
    AW->>LA: POST /api/heats/{id}/register { driverId }
    LA->>DB: INSERT INTO registrations<br/>(with concurrency check)
    LA-->>AW: OK

    O->>AW: Open live event screen
    AW->>LA: GET /api/heats/{id}/registrations
    LA-->>AW: Queue of registered drivers
    O->>AW: Start run for driver
    AW->>TA: POST /simulate/run { heatId, driverId }
    TA->>R: XADD course-events ...
    LA->>R: XREADGROUP ...
    LA->>DB: Store run / penalties
    AW->>LA: GET /api/heats/{id}/leaderboard
    LA-->>AW: Latest rankings
```

## Key improvements

1. **Heat management** — a dedicated screen to create, edit, and list heats.
2. **Driver management** — add or import drivers before the event.
3. **Bulk registration** — register multiple drivers to a heat in one view, with a slot grid.
4. **Live event screen** — the central screen during the race:
   - Shows the queue of registered drivers.
   - One click to start the next run.
   - Shows on-course status.
   - Manual override for cones or finish if the real gateway is not used.
5. **Leaderboard** — per-heat ranking, auto-refreshing.

## Why not “Simulate run” one by one?

In a real race, an organizer does not want to type IDs and click around. They want:

- A queue that shows who is next.
- A single button to start the run.
- Automatic leaderboard refresh.
- Manual overrides when sensors fail.

The proposed management UI makes the application usable for an actual event, not just for integration testing.
