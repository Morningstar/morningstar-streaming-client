Feature: Morningstar Snapshot Client
    In order to retrieve market data from Morningstar
    As a client
    I want to request snapshots of instrument data via the Snapshot API

    Scenario: Requesting a snapshot for a valid instrument returns data
        Given I have a snapshot request for "0P0000038R" with event type "LastPrice"
        When I request a snapshot
        Then I receive a 200 OK response
        And the response contains data for "0P0000038R"

    Scenario: Requesting a snapshot with a partially invalid investment returns warnings
        Given I have a snapshot request for "INVALID_ID" with event type "LastPrice"
        And the API returns a warning for "INVALID_ID"
        When I request a snapshot
        Then I receive a 200 OK response
        And the response contains a warning for "INVALID_ID"

    Scenario: Requesting a snapshot for multiple event types returns all event data
        Given I have a snapshot request for "0P0000038R" with event types "LastPrice" and "TopOfBook"
        When I request a snapshot
        Then I receive a 200 OK response
        And the response contains "LastPrice" data for "0P0000038R"
        And the response contains "TopOfBook" data for "0P0000038R"

    Scenario: Requesting a snapshot returns unauthorized when credentials are invalid
        Given I have a snapshot request for "0P0000038R" with event type "LastPrice"
        And the API returns an unauthorized response
        When I request a snapshot
        Then I receive a 401 Unauthorized response