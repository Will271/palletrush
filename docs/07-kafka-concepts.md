# 07 - Kafka core concepts

**Related docs:** [Index](./00-index.md) · [05 - Why Kafka?](./05-kafka-rationale.md) · [06 - Kafka architecture](./06-kafka-architecture.md)

This is a short glossary of the main Kafka terms used in the PalletRush architecture discussion.

## Topic

A category or feed of records. In our case there would be one topic: `course-events`.

## Partition

A topic is split into partitions. Each partition is an ordered, immutable sequence of records. Records in a partition have a strict order; records across partitions have no guaranteed order. For PalletRush, we would partition by `runId`.

## Broker

A Kafka server. A Kafka cluster is made of one or more brokers. Producers and consumers talk to brokers.

## Producer

A client that publishes records to a topic. In PalletRush, the Telemetry API is the producer.

## Consumer

A client that subscribes to a topic and reads records.

## Consumer group

A set of consumers that work together to read a topic. Kafka assigns each partition to one consumer in the group, so messages are load-balanced and no two consumers in the same group read the same partition. The League API would be in the `league` consumer group.

## Offset

A unique identifier for a record within a partition. Consumers track how far they have read by storing their offsets. Committing an offset tells Kafka: "I have processed everything up to here."

## Commit

The act of saving the consumer's offset to Kafka. If the consumer commits after a handler succeeds, the record will not be replayed. If it does not commit, the record will be delivered again.

## Replication

Partitions can be replicated across multiple brokers for fault tolerance. If one broker fails, another broker that holds a replica can take over.

## ZooKeeper / KRaft

Older Kafka versions use ZooKeeper to manage cluster metadata. Newer versions use KRaft (Kafka Raft), which removes the ZooKeeper dependency.

## What these mean for PalletRush

- **Topic:** `course-events`.
- **Partitions:** split by `runId` so each run is ordered.
- **Producer:** Telemetry API.
- **Consumer group:** `league` (League API).
- **Offset commit:** done after a handler succeeds, giving at-least-once delivery.
