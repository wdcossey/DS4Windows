namespace DS4WinWPF;

public interface IProfileListService
{
    ProfileList ProfileList { get; }
}

public class ProfileListService : IProfileListService
{
    public ProfileListService()
    {
        ProfileList.Refresh();
    }

    public ProfileList ProfileList { get; } = new();
}