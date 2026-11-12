# 05 - Why Kafka?

**Related docs:** [Index](./00-index.md) · [01 - Architecture](./01-architecture.md) · [02 - Event flow](./02-event-flow.md) · [03 - Cone hit lifecycle](./03-cone-hit-lifecycle.md) · [06 - Kafka architecture](./06-kafka-architecture.md) · [07 - Kafka concepts](./07-kafka-concepts.md)

This document explains why we might replace Redis Streams with Apache Kafka, and when the switch makes sense.

## Comparison: Redis Streams, Kafka, RabbitMQ

| | Redis Streams | Kafka | RabbitMQ |
|---|---|---|---|
| **Best for** | Simple event streaming when Redis is already in the stack | High throughput, durable replayable log, multiple independent consumer groups | Task queues, request/reply, routing, worker patterns |
| **Replay / reprocess** | Limited; can read by ID range | Excellent — consumers can rewind to any offset | Hard; once a message is acked it is gone |
| **Throughput** | Moderate; single-threaded stream, one node | Very high; partitioned, horizontally scalable | High; but not designed as a streaming log |
| **Durability** | Depends on Redis persistence (RDB/AOF) configured | Designed for durable log storage by default | Durable queues, but messages removed on ack |
| **Operational complexity** | Low — already running Redis | Higher — needs Kafka cluster (KRaft or ZooKeeper) | Medium — simpler than Kafka |
| **Ordering** | Per stream, by ID | Per partition, by offset | Per queue, by delivery |
| **Consumer groups** | Basic (`XREADGROUP`, `XAUTOCLAIM`) | Mature and scalable | Available but not log-based |

## Why Kafka is a better long-term fit

The current Redis Streams setup works for the assignment, but Kafka is stronger if the system needs any of the following:

- **Higher throughput** — Kafka partitions a topic and scales horizontally.
- **Durability by design** — messages are written to disk with replication by default.
- **Replayability** — consumers can rewind to an arbitrary offset and reprocess events.
- **Multiple independent consumer groups** — e.g., one for League, one for analytics, one for audit logging, each at its own pace.
- **Better observability** — lag monitoring, consumer group rebalancing, and tooling are mature.

## When to make the switch

Consider Kafka when any of these become true:

- The Redis stream becomes a bottleneck (CPU, memory, or PEL growth).
- You need more than one consumer group.
- You need replay / audit / analytics on the full event history.
- Operations can support a Kafka cluster (or use a managed service like Confluent Cloud or AWS MSK).

For the current assignment and small scale, **Redis Streams is simpler and sufficient**. Kafka is the natural next step if the event volume or consumer variety grows.
