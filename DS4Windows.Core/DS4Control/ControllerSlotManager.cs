namespace DS4Windows;

public class ControllerSlotManager
{
    public ReaderWriterLockSlim CollectionLocker { get; } = new();
    public List<DS4Device> ControllerColl { get; init; } = new ();
    public Dictionary<int, DS4Device> ControllerDict { get; } = new();
    public Dictionary<DS4Device, int> ReverseControllerDict { get; } = new();

    public void AddController(DS4Device device, int slotIdx)
    {
        using (var _ = new WriteLocker(CollectionLocker))
        {
            ControllerColl.Add(device);
            ControllerDict.Add(slotIdx, device);
            ReverseControllerDict.Add(device, slotIdx);
        }
    }

    public void RemoveController(DS4Device device, int slotIdx)
    {
        using (var _ = new WriteLocker(CollectionLocker))
        {
            ControllerColl.Remove(device);
            ControllerDict.Remove(slotIdx);
            ReverseControllerDict.Remove(device);
        }
    }

    public void ClearControllerList()
    {
        using (var _ = new WriteLocker(CollectionLocker))
        {
            ControllerColl.Clear();
            ControllerDict.Clear();
            ReverseControllerDict.Clear();
        }
    }
}