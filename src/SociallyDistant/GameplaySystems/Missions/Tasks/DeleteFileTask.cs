using SociallyDistant.Core.Core;
using SociallyDistant.Core.Core.Events;
using SociallyDistant.Core.EventBus;
using SociallyDistant.Core.Missions;
using SociallyDistant.Core.OS.Network;

namespace SociallyDistant.GameplaySystems.Missions.Tasks;

[MissionTask("wait")]
public class WaitTask : IMissionTask
{
    public Task WaitForCompletion(IMissionContext context, string[] arguments)
    {
        var timeout = SociallyDistantUtility.ParseDurationString(string.Join(" ", arguments));
        return Task.Delay((int)timeout.TotalMilliseconds, context.AbandonmentToken);
    }
}

[MissionTask("ping")]
public class PingTask : IMissionTask
{
    public async Task WaitForCompletion(IMissionContext context, string[] arguments)
    {
        var narrativeId = arguments[0];

        uint? address = context.Game.Network.GetNarrativeAddress(narrativeId);
        if (address == null)
            throw new InvalidOperationException($"Cannot locate a network with the narrative ID {narrativeId}.");
        
        var expectedPath = string.Join(" ", arguments);
        var completionSource = new TaskCompletionSource();

        using (EventBus.Listen<PingEvent>((evt) =>
               {
                   if (evt.DestinationAddress != address.Value)
                       return;
                   
                   completionSource.TrySetResult();
               }))
        {
            using (context.AbandonmentToken.Register(() => { completionSource.TrySetCanceled(context.AbandonmentToken); }))
            {
                await completionSource.Task;
            }
        }
    }
}

[MissionTask("deletefile")]
public class DeleteFileTask : IMissionTask
{
    public async Task WaitForCompletion(IMissionContext context, string[] arguments)
    {
        var expectedPath = string.Join(" ", arguments);
        var completionSource = new TaskCompletionSource();

        using (EventBus.Listen<FileSystemEvent>((evt) =>
               {
                   if (evt.FileSystemInteraction != FileSystemEventType.DeleteFile)
                       return;
                   
                   string path = $"/{evt.NarrativeId}{evt.Path}";
                   if (path != expectedPath)
                       return;

                   completionSource.TrySetResult();
               }))
        {
            using (context.AbandonmentToken.Register(() => { completionSource.TrySetCanceled(context.AbandonmentToken); }))
            {
                await completionSource.Task;
            }
        }
    }
}