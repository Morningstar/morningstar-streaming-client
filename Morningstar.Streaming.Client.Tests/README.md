# Morningstar.Streaming.Client.Tests

This project contains comprehensive unit tests for the Morningstar Streaming Client library.

## Testing Framework & Libraries

- **XUnit** - Primary testing framework
- **FluentAssertions** - Expressive assertion library for more readable tests
- **Moq** - Mocking framework for creating test doubles

## Test Structure

Tests are organized following a clear folder structure that mirrors the main project:

```
Morningstar.Streaming.Client.Tests/
├── ClientTests/
│   └── StreamingApiClientTests.cs (21 tests)
├── ServiceTests/
│   ├── CanaryServiceTests.cs (12 tests)
│   ├── TokenProviderTests.cs (11 tests)
│   ├── SubscriptionGroupManagerTests.cs (14 tests)
│   ├── StreamSubscriptionFactoryTests.cs (11 tests)
│   ├── WebSocketConsumerFactoryTests.cs (12 tests)
│   └── WebSocketConsumerTests.cs (17 tests)
└── (other test folders as needed)
```

## Testing Standards

All tests follow the **AAA (Arrange-Act-Assert)** pattern:

1. **Arrange**: Set up test data, mocks, and dependencies
2. **Act**: Execute the method being tested
3. **Assert**: Verify the expected behavior using FluentAssertions

## Test Class Structure

Each test class follows a consistent pattern:

```csharp
public class ServiceNameTests
{
    // Read-only mock dependencies
    private readonly Mock<IDependency1> _mockDependency1;
    private readonly Mock<IDependency2> _mockDependency2;
    
    // System Under Test (SUT)
    private readonly ServiceName _sut;

    public ServiceNameTests()
    {
        // Arrange - Initialize all mocks in constructor
        _mockDependency1 = new Mock<IDependency1>();
        _mockDependency2 = new Mock<IDependency2>();
        
        // Create the service under test with mocked dependencies
        _sut = new ServiceName(
            _mockDependency1.Object,
            _mockDependency2.Object
        );
    }

    [Fact]
    public void MethodName_Scenario_ExpectedBehavior()
    {
        // Arrange - Setup specific test data and mock behaviors
        
        // Act - Execute the method
        
        // Assert - Verify expectations
    }
}
```

## Key Benefits of This Approach

1. **Consistency**: All dependencies are initialized once in the constructor
2. **Isolation**: Each test uses the same SUT instance, but with different mock setups
3. **Readability**: Clear separation of concerns with AAA pattern
4. **Maintainability**: Easy to add new tests following the established pattern

## Running Tests

Run all tests:
```bash
dotnet test
```

Run tests with verbose output:
```bash
dotnet test --logger "console;verbosity=detailed"
```

Run specific test:
```bash
dotnet test --filter "FullyQualifiedName~CanaryServiceTests.StartLevel1SubscriptionAsync_WithSuccessfulResponse"
```

## Test Coverage

### ClientTests

**StreamingApiClientTests** - 21 comprehensive tests covering:

**HTTP Stream Creation (10 tests)**:
- ✅ Successful L1 stream creation
- ✅ Authorization header handling
- ✅ Error responses (BadRequest, Unauthorized)
- ✅ Exception handling and logging
- ✅ HTTP method validation
- ✅ Partial content responses
- ✅ Generic request type handling
- ✅ Token provider integration
- ✅ Call ordering verification

**WebSocket Subscription (11 tests)**:
- ✅ Pre-cancelled token handling (graceful exit)
- ✅ Valid parameters acceptance
- ✅ Heartbeat timeout configuration (15s timeout, 5s interval)
- ✅ Async message callback signature validation
- ✅ Token provider authentication integration
- ✅ Connection attempt logging
- ✅ Cancellation token respect
- ✅ Exponential backoff calculation validation
- ✅ Retry behavior with failed connections
- ✅ Return type validation (Task)
- ✅ CancellationToken parameter with default value
- ✅ WebSocket URL parameter acceptance

### ServiceTests

**CanaryServiceTests** - 12 tests covering:
- ✅ Subscription creation with OK and PartialContent responses
- ✅ Error handling for BadRequest responses
- ✅ Duration-based and indefinite subscriptions
- ✅ WebSocket consumer creation for multiple URLs
- ✅ Logging configuration (enabled/disabled)
- ✅ Subscription lifecycle (start, stop, list)
- ✅ Subscription manager integration
- ✅ Call ordering verification

**TokenProviderTests** - 11 tests covering:
- ✅ Bearer token creation and formatting
- ✅ Base64 credential encoding
- ✅ OAuth configuration usage from AppConfig
- ✅ HTTP POST method verification
- ✅ Exception handling with proper logging
- ✅ Call ordering (OAuthProvider → ApiHelper)
- ✅ Special character handling in credentials
- ✅ Different token types support
- ✅ Null body parameter validation

**SubscriptionGroupManagerTests** - 14 tests covering:
- ✅ Adding new subscriptions
- ✅ Duplicate GUID prevention
- ✅ Retrieving subscriptions by GUID
- ✅ Retrieving all subscriptions
- ✅ Removing subscriptions
- ✅ Exception handling for non-existent GUIDs
- ✅ Thread-safe concurrent dictionary operations
- ✅ Multiple subscription management
- ✅ List instance independence
- ✅ Object reference integrity

**StreamSubscriptionFactoryTests** - 11 tests covering:
- ✅ Successful stream subscription creation
- ✅ Cancellation token with duration-based timeout
- ✅ Cancellation token without timeout (indefinite)
- ✅ Combining realtime and delayed WebSocket URLs
- ✅ Handling delayed-only or realtime-only URLs
- ✅ Empty URL handling for failed requests
- ✅ Endpoint URL construction from configuration
- ✅ Custom base address configuration
- ✅ Custom endpoint path configuration
- ✅ Stream request parameter passing
- ✅ Partial content response handling

**WebSocketConsumerFactoryTests** - 12 tests covering:
- ✅ Consumer creation with valid parameters
- ✅ LogToFile flag handling (true/false)
- ✅ Multiple consumers with different URLs
- ✅ Dependency injection verification
- ✅ Factory pattern (new instance per call)
- ✅ URL with GUID extraction
- ✅ Short URL handling
- ✅ Logger integration
- ✅ CounterLogger integration
- ✅ WebSocketLoggerFactory integration
- ✅ StreamingApiClient integration

**WebSocketConsumerTests** - 17 tests covering:
- ✅ Constructor with valid parameters
- ✅ GUID extraction from WebSocket URL
- ✅ Invalid GUID handling (defaults to Guid.Empty)
- ✅ Subscription registration with CounterLogger
- ✅ Subscription unregistration after completion
- ✅ Client SubscribeAsync invocation with correct URL
- ✅ Cancellation token passing
- ✅ Message callback increments counter
- ✅ LogToFile true - messages passed to channel
- ✅ LogToFile false - no logging occurs
- ✅ Cancellation handling with warning log
- ✅ Pre-cancelled token respect
- ✅ Multiple messages increment counter multiple times
- ✅ Execution order: Register before Subscribe
- ✅ Execution order: Unregister after completion

**CanaryServiceTests** - 12 comprehensive tests covering:
- ✅ Successful subscription scenarios (OK and PartialContent responses)
- ✅ Error handling (BadRequest and other error responses)
- ✅ Duration handling (with and without expiry)
- ✅ WebSocket consumer creation and lifecycle
- ✅ Logging configuration
- ✅ Subscription management (start, stop, list)
- ✅ Edge cases and null handling

**TokenProviderTests** - 11 comprehensive tests covering:
- ✅ Bearer token creation and formatting
- ✅ Base64 credential encoding
- ✅ OAuth configuration usage
- ✅ HTTP POST method verification
- ✅ Error handling and logging
- ✅ Call ordering (OAuthProvider → ApiHelper)
- ✅ Special character handling in credentials
- ✅ Null body parameter validation
- ✅ Custom token types

**SubscriptionGroupManagerTests** - 14 comprehensive tests covering:
- ✅ Adding new subscriptions
- ✅ Duplicate GUID handling
- ✅ Retrieving subscriptions by GUID
- ✅ Retrieving all subscriptions
- ✅ Removing subscriptions
- ✅ Exception handling for non-existent GUIDs
- ✅ Concurrent dictionary behavior
- ✅ Multiple subscription management
- ✅ List instance independence
- ✅ Object reference integrity

**StreamSubscriptionFactoryTests** - 11 comprehensive tests covering:
- ✅ Successful stream subscription creation
- ✅ Cancellation token with timeout (duration-based)
- ✅ Cancellation token without timeout
- ✅ Combining realtime and delayed WebSocket URLs
- ✅ Handling delayed-only URLs
- ✅ Empty URL handling
- ✅ Endpoint URL construction
- ✅ Custom base address configuration
- ✅ Custom endpoint path configuration
- ✅ Stream request parameter passing
- ✅ Partial content responses

## Test Summary

**Total Tests: 75** (increased from 68)
- ClientTests: 21 tests
- ServiceTests: 54 tests
- **New WebSocket Consumer Tests Added** ⭐
- All tests passing ✅
- Execution time: ~580ms

## Best Practices

1. **One assertion per test** (when practical) - Makes it clear what failed
2. **Descriptive test names** - Follow pattern: `MethodName_Scenario_ExpectedBehavior`
3. **Mock verification** - Always verify important mock interactions
4. **Async test handling** - Properly await async operations
5. **Test isolation** - Each test should be independent and repeatable

## Future Enhancements

As new services are added to the main project, corresponding test classes should be created in the `ServiceTests` folder following the same patterns demonstrated in `CanaryServiceTests`.
