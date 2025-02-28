namespace Elsa.Workflows.Models;

/// <summary>
/// Represents a workflow execution log entry.
/// </summary>
public record WorkflowExecutionLogEntry(
    string ActivityInstanceId,
    string? ParentActivityInstanceId,
    string ActivityId,
    string ActivityType,
    int ActivityTypeVersion,
    string? ActivityName,
    string NodeId,
    IDictionary<string, object>? ActivityState,
    DateTimeOffset Timestamp,
    long Sequence,
    string? EventName,
    string? Message,
    string? Source,
    object? Payload);