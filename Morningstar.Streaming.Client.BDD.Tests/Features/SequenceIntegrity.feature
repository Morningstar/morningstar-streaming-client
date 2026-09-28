Feature: Sequence integrity metrics
    In order to detect gaps, duplicates and reordering in a market-data stream
    As a Client
    I want each out-of-sequence condition classified into the right metric.

    Scenario: In-order messages record no anomalies
        Given a sequence detector
        When messages arrive with sequence numbers 1, 2, 3
        Then no sequence anomaly is recorded

    Scenario: A skipped sequence records missing by the gap size
        Given a sequence detector
        When messages arrive with sequence numbers 1, 2, 6
        Then a "Missing" anomaly is recorded
        And the reported missing count is 3

    Scenario: A repeat of the latest sequence records a duplicate
        Given a sequence detector
        When messages arrive with sequence numbers 1, 2, 2
        Then a "Duplicate" anomaly is recorded

    Scenario: A late arrival that fills a gap records recovered
        Given a sequence detector
        When messages arrive with sequence numbers 1, 2, 5, 3
        Then a "Recovered" anomaly is recorded

    Scenario: An out-of-order duplicate is both out-of-order and duplicate
        Given a sequence detector
        When messages arrive with sequence numbers 1, 2, 3, 2
        Then an "OutOfOrder" anomaly is recorded
        And a "Duplicate" anomaly is recorded

    Scenario: A message missing its sequence number is unclassified
        Given a sequence detector
        When a message arrives with no sequence number
        Then an "Unclassified" anomaly is recorded

    Scenario: A late arrival older than the tracking window is expired
        Given a sequence detector with a tracking window of 2 sequences
        When messages arrive with sequence numbers 1, 5, 2
        Then an "Expired" anomaly is recorded

    Scenario: A very large forward jump reports the full gap but bounds memory
        Given a sequence detector with a tracking window of 10 sequences
        When messages arrive with sequence numbers 1, 1000000
        Then a "Missing" anomaly is recorded
        And the reported missing count is 999998

    Scenario: Sequences are tracked independently per instrument and event type
        Given a sequence detector
        When a message arrives for instrument "A" event "Trade" with sequence 1
        And a message arrives for instrument "B" event "Trade" with sequence 1
        And a message arrives for instrument "A" event "Trade" with sequence 3
        Then a "Missing" anomaly is recorded
        And the reported missing count is 1
