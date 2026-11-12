# PalletRush documentation index

**Related docs:** [01 - Architecture](./01-architecture.md) · [02 - Event flow](./02-event-flow.md) · [03 - Cone hit lifecycle](./03-cone-hit-lifecycle.md)

A short sequence of diagrams and explanations, from the high-level architecture down to the Redis and PostgreSQL details.

1. [01 - PalletRush architecture](./01-architecture.md) — the end-to-end user flow through the Angular web, Telemetry API, Redis, and League API.
2. [02 - What happens when an event reaches Redis](./02-event-flow.md) — how `XREADGROUP`, `XACK`, and `XAUTOCLAIM` make event processing reliable.
3. [03 - Cone hit and run lifecycle](./03-cone-hit-lifecycle.md) — how `ConeHit` and `RunStarted` are handled in PostgreSQL, including the advisory-lock concurrency control and pending-hit reconciliation.
4. [05 - Why Kafka?](./05-kafka-rationale.md) — why Kafka might replace Redis Streams and how the three brokers compare.
5. [06 - Kafka architecture](./06-kafka-architecture.md) — the proposed Kafka-based design: topic, partitions, producers, consumers, and offsets.
6. [07 - Kafka core concepts](./07-kafka-concepts.md) — a short glossary of Kafka terms.
7. [08 - Proposed management UI](./08-ui-management.md) — a better organizer-facing UI for heats, drivers, registration, and live event control.
8. [09 - Actual screens](./09-actual-screens.md) — the screens that currently exist in the Angular app, their routes, and the endpoints they call.
9. [10 - Wireframes](./10-wireframes.md) — ASCII layout sketches for the proposed management UI.
