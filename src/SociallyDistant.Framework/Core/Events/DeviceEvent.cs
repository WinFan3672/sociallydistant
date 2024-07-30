using SociallyDistant.Core.OS.Devices;

namespace SociallyDistant.Core.Core.Events;

public abstract class DeviceEvent : WitnessEvent
{
    public IComputer Computer { get; }
    
    protected DeviceEvent(IComputer computer) : base(WitnessType.Device, computer.NarrativeId ?? string.Empty)
    {
        this.Computer = computer;
    }
}