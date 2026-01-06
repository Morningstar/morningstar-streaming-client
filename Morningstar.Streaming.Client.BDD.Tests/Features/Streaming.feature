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