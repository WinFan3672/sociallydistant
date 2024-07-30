namespace SociallyDistant.Core.Missions;

/// <summary>
///     Interface for an object that can be used as a mission task.
/// </summary>
public interface IMissionTask
{
    /// <summary>
    ///     Called by the mission system when the objective is started. Waits for the task to be completed.
    /// </summary>
    /// <param name="context">Information about the mission being played, and a reference to the game context.</param>
    /// <param name="arguments">Arguments specified by the mission script for the objective.</param>
    /// <returns>A task that completes when the objective is completed by the player.</returns>
    Task WaitForCompletion(IMissionContext context, string[] arguments);
}