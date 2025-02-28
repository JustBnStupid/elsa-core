using System.Runtime.CompilerServices;
using Elsa.Expressions.Helpers;
using Elsa.Extensions;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Elsa.Workflows.Services;

namespace Elsa.Workflows.Activities;

/// <summary>
/// Schedule an activity for each item in parallel.
/// </summary>
/// <typeparam name="T"></typeparam>
[Activity("Elsa", "Looping", "Schedule an activity for each item in parallel.")]
public class ParallelForEach<T> : Activity
{
    private const string ScheduledTagsProperty = nameof(ScheduledTagsProperty);
    private const string CompletedTagsProperty = nameof(CompletedTagsProperty);

    /// <inheritdoc />
    public ParallelForEach([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }

    /// <summary>
    /// The items to iterate.
    /// </summary>
    [Input(Description = "The items to iterate through.")]
    public Input<ICollection<T>> Items { get; set; } = new(Array.Empty<T>());

    /// <summary>
    /// The <see cref="IActivity"/> to execute each iteration.
    /// </summary>
    [Port]
    public IActivity Body { get; set; } = default!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var items = context.Get(Items)!.ToList();
        var tags = new List<Guid>();
        var currentIndex = 0;

        if (items.Count == 0)
        {
            await context.CompleteActivityAsync();
            return;
        }

        foreach (var item in items)
        {
            // For each item, declare a new variable for the work to be scheduled.
            var currentValueVariable = new Variable<T>("CurrentValue", item)
            {
                // TODO: This should be configurable, because this won't work for e.g. file streams and other non-serializable types.
                StorageDriverType = typeof(WorkflowStorageDriver)
            };
            
            var currentIndexVariable = new Variable<int>("CurrentIndex", currentIndex++)
            {
                StorageDriverType = typeof(WorkflowStorageDriver)
            };

            var variables = new List<Variable>
            {
                currentValueVariable,
                currentIndexVariable
            };

            // Schedule a body of work for each item.
            var tag = Guid.NewGuid();
            tags.Add(tag);
            await context.ScheduleActivityAsync(Body, OnChildCompleted, tag, variables);
        }

        context.SetProperty(ScheduledTagsProperty, tags);
        context.SetProperty(CompletedTagsProperty, new List<Guid>());
    }

    private async ValueTask OnChildCompleted(ActivityCompletedContext context)
    {
        var targetContext = context.TargetContext;
        var scheduledTags = targetContext.GetProperty<List<Guid>>(ScheduledTagsProperty)!;
        var completedTag = targetContext.Tag.ConvertTo<Guid>();

        var completedTags = new HashSet<Guid>(targetContext.UpdateProperty<List<Guid>>(CompletedTagsProperty, completedTags =>
        {
            completedTags!.Add(completedTag);
            return completedTags;
        }));

        // If not all scheduled activities have completed yet, we're not done yet.
        if (!scheduledTags.IsEqualTo(completedTags))
            return;

        // We're done, so complete the activity.
        await targetContext.CompleteActivityAsync();
    }
}