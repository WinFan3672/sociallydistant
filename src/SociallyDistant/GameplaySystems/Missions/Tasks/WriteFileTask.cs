using SociallyDistant.Core.Core.Events;
using SociallyDistant.Core.Missions;

namespace SociallyDistant.GameplaySystems.Missions.Tasks;

[MissionTask("writefile")]
public class WriteFileTask : IMissionTask
{
    public async Task WaitForCompletion(IMissionContext context, string[] arguments)
    {
        var expectedPath = string.Join(" ", arguments);
        var completionSource = new TaskCompletionSource();

        using (EventBus.Listen<FileSystemEvent>((evt) =>
               {
                   if (evt.FileSystemInteraction != FileSystemEventType.WriteFile)
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