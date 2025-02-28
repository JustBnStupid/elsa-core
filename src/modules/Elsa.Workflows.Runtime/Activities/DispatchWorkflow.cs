using System.Runtime.CompilerServices;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.UIHints;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Bookmarks;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Requests;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Activities;

/// <summary>
/// Creates a new workflow instance of the specified workflow and dispatches it for execution.
/// </summary>
[Activity("Elsa", "Composition", "Create a new workflow instance of the specified workflow and dispatch it for execution.")]
[UsedImplicitly]
public class DispatchWorkflow : Activity<object>
{
    /// <inheritdoc />
    public DispatchWorkflow([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }

    /// <summary>
    /// The definition ID of the workflow to dispatch. 
    /// </summary>
    [Input(
        Description = "The definition ID of the workflow to dispatch.",
        UIHint = InputUIHints.WorkflowDefinitionPicker
    )]
    public Input<string> WorkflowDefinitionId { get; set; } = default!;

    /// <summary>
    /// The correlation ID to associate the workflow with. 
    /// </summary>
    [Input(Description = "The correlation ID to associate the workflow with.")]
    public Input<string?> CorrelationId { get; set; } = default!;

    /// <summary>
    /// The input to send to the workflow.
    /// </summary>
    [Input(Description = "The input to send to the workflow.")]
    public Input<IDictionary<string, object>?> Input { get; set; } = default!;

    /// <summary>
    /// True to wait for the child workflow to complete before completing this activity, false to "fire and forget".
    /// </summary>
    [Input(Description = "Wait for the child workflow to complete before completing this activity.")]
    public Input<bool> WaitForCompletion { get; set; } = default!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var waitForCompletion = WaitForCompletion.GetOrDefault(context);

        // Dispatch the child workflow.
        var instanceId = await DispatchChildWorkflowAsync(context);

        // If we need to wait for the child workflow to complete, create a bookmark.
        if (waitForCompletion)
        {
            var bookmarkOptions = new CreateBookmarkArgs
            {
                Callback = OnChildWorkflowCompletedAsync,
                Payload = new DispatchWorkflowBookmark(instanceId),
                IncludeActivityInstanceId = false
            };
            context.CreateBookmark(bookmarkOptions);
        }
        else
        {
            // Otherwise, we can complete immediately.
            await context.CompleteActivityAsync();
        }
    }

    private async ValueTask<string> DispatchChildWorkflowAsync(ActivityExecutionContext context)
    {
        var workflowDefinitionId = WorkflowDefinitionId.Get(context);
        var input = Input.GetOrDefault(context) ?? new Dictionary<string, object>();

        input["ParentInstanceId"] = context.WorkflowExecutionContext.Id;

        var correlationId = CorrelationId.GetOrDefault(context);
        var workflowDispatcher = context.GetRequiredService<IWorkflowDispatcher>();
        var identityGenerator = context.GetRequiredService<IIdentityGenerator>();
        var instanceId = identityGenerator.GenerateId();
        var request = new DispatchWorkflowDefinitionRequest
        {
            DefinitionId = workflowDefinitionId,
            VersionOptions = VersionOptions.Published,
            Input = input,
            CorrelationId = correlationId,
            InstanceId = instanceId
        };

        // Dispatch the child workflow.
        await workflowDispatcher.DispatchAsync(request, context.CancellationToken);

        return instanceId;
    }

    private async ValueTask OnChildWorkflowCompletedAsync(ActivityExecutionContext context)
    {
        var input = context.WorkflowInput;
        context.Set(Result, input);
        await context.CompleteActivityAsync();
    }
}