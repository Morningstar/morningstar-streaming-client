Feature: Morningstar Streaming Client
    In order to stream data from Morningstar
    As a Client
    I want to be able to subscribe and stream data over a websocket.

    Scenario: Create subscription and subscribe.
        Given I have a valid subscribe request
        When I create a subscription
        Then I receive a successful response
        And messages are successfully being received

    Scenario: Create subscription and report partial success.
        Given I have a partially valid subscribe request
        When I create a subscription
        Then I receive a partial successful response
        And messages are successfully being received


    Scenario: Unexpected disconnection, reconnect where I left off.
        Given I have a valid subscribe request
        When I create a subscription
            And messages are successfully being received
            And I get an unexpected disconnect                   
        Then I am able to reconnect 
        And messages are successfully being received


    Scenario: Controlled reconnect where message loss is acceptable and unavoidable.
        Given I have a valid subscribe request
        When I create a subscription
            And messages are successfully being received
            And I get an expected disconnect                   
        Then I am able to reconnect 
        And messages are successfully being received


    Scenario: Controlled reconnect avoiding message loss.
        Given I have a valid subscribe request
        When I create a subscription
            And messages are successfully being received
            And I get an unexpected disconnect                   
        Then I am able to reconnect 
        And messages are successfully being received from where I left off


    Scenario: Admin disconnect notice with arbitration confirmed hands over cleanly
        Given I have a valid subscribe request
            And the WebSocket consumer supports arbitration
        When I create a subscription
            And messages are successfully being received
            And an admin disconnect notice with arbitration enabled is received
            And the replacement connection delivers a duplicate message
            And the original connection is closed by the server
        Then the arbitration outcome is reported as "ConfirmedHandover"
        And the subscription is still active


    Scenario: Admin disconnect notice hands over on natural expiry without confirmation
        Given I have a valid subscribe request
            And the WebSocket consumer supports arbitration
        When I create a subscription
            And messages are successfully being received
            And an admin disconnect notice with arbitration enabled is received
            And the original connection is closed by the server
        Then the arbitration outcome is reported as "UnconfirmedHandover"
        And the subscription is still active


    Scenario: Admin disconnect notice handover ends the subscription when the replacement fails
        Given I have a valid subscribe request
            And the WebSocket consumer supports arbitration
        When I create a subscription
            And messages are successfully being received
            And an admin disconnect notice with arbitration enabled is received
            And the replacement connection fails to establish
            And the original connection is closed by the server
        Then the arbitration outcome is reported as "ReplacementConnectionFailed"
        And the subscription is no longer active


    Scenario: Admin disconnect notice without arbitration enabled does not trigger a replacement connection
        Given I have a valid subscribe request
            And the WebSocket consumer supports arbitration
        When I create a subscription
            And messages are successfully being received
            And an admin disconnect notice with arbitration disabled is received
            And the original connection is closed by the server
        Then no replacement connection is created
        And the subscription is no longer active