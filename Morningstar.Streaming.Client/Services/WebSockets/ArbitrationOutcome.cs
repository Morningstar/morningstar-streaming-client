namespace Morningstar.Streaming.Client.Services.WebSockets;

/// <summary>
/// Outcome of an Admin/Disconnect-triggered arbitration handover for a single WebSocket connection.
/// The library only reports the raw outcome - callers decide what (if anything) to do with it,
/// e.g. record metrics.
/// </summary>
/// <param name="Confirmed">The replacement connection delivered a message already forwarded by the retiring one before it was retired.</param>
/// <param name="ReplacementFailed">The replacement connection never came up (faulted) before the retiring one ended.</param>
public readonly record struct ArbitrationOutcome(bool Confirmed, bool ReplacementFailed);
