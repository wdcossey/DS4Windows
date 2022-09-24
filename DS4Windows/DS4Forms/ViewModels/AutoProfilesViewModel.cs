using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DS4WinWPF.DS4Forms.ViewModels;

public class AutoProfilesViewModel
{
    private readonly object colLockObj = new();
    private ProgramItem selectedItem;
    private readonly HashSet<string> existingApps = new();

    public ObservableCollection<ProgramItem> ProgramColl { get; } = new();
    public AutoProfileHolder AutoProfileHolder { get; private set; }

    public int SelectedIndex { get; set; } = -1;

    public ProgramItem SelectedItem
    {
        get => selectedItem;
        set
        {
            selectedItem = value;
            CurrentItemChange?.Invoke(this, value);
        }
    }

    private ProfileList profileList;

    public delegate void CurrentItemChangeHandler(AutoProfilesViewModel sender, ProgramItem item);
    public event CurrentItemChangeHandler CurrentItemChange;

    public event EventHandler SearchFinished;
    public delegate void AutoProfileStateHandler(AutoProfilesViewModel sender, bool state);
    public event AutoProfileStateHandler AutoProfileSystemChange;

    public bool RevertDefaultProfileOnUnknown
    {
        get => Global.AutoProfileRevertDefaultProfile;
        set => Global.AutoProfileRevertDefaultProfile = value;
    }

    public bool UsingExpandedControllers => IControlService.USING_MAX_CONTROLLERS;

    public AutoProfilesViewModel WithAutoProfileHolder(AutoProfileHolder autoProfileHolder)
    {
        AutoProfileHolder = autoProfileHolder;
        PopulateCurrentEntries();
        return this;
    }
        
    public AutoProfilesViewModel WithProfileList(ProfileList profileList)
    {
        this.profileList = profileList;
        return this;
    }
        
    public AutoProfilesViewModel Init()
    {
        BindingOperations.EnableCollectionSynchronization(ProgramColl, colLockObj);
        return this;
    }

    private void PopulateCurrentEntries()
    {
        foreach(var entry in AutoProfileHolder.AutoProfileColl)
        {
            var item = new ProgramItem(entry.Path, entry);

            ProgramColl.Add(item);
            existingApps.Add(entry.Path);
        }
    }

    public void RemoveUnchecked()
    {
        AutoProfileSystemChange?.Invoke(this, false);
        ProgramColl.Clear();
        existingApps.Clear();
        PopulateCurrentEntries();
        AutoProfileSystemChange?.Invoke(this, true);
    }

    public async void AddProgramsFromStartMenu()
    {
        AutoProfileSystemChange?.Invoke(this, false);
        await Task.Run(() =>
        {
            AddFromStartMenu(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu) + "\\Programs");
        });

        SearchFinished?.Invoke(this, EventArgs.Empty);
        AutoProfileSystemChange?.Invoke(this, true);
    }

    public async void AddProgramsFromSteam(string location)
    {
        AutoProfileSystemChange?.Invoke(this, false);
        await Task.Run(() => AddAppsFromLocation(location));

        SearchFinished?.Invoke(this, EventArgs.Empty);
        AutoProfileSystemChange?.Invoke(this, true);
    }

    public async void AddProgramsFromDir(string location)
    {
        AutoProfileSystemChange?.Invoke(this, false);
        await Task.Run(() => AddAppsFromLocation(location));

        SearchFinished?.Invoke(this, EventArgs.Empty);
        AutoProfileSystemChange?.Invoke(this, true);
    }

    public async void AddProgramExeLocation(string location)
    {
        AutoProfileSystemChange?.Invoke(this, false);
        await Task.Run(() => AddAppExeLocation(location));

        SearchFinished?.Invoke(this, EventArgs.Empty);
        AutoProfileSystemChange?.Invoke(this, true);
    }

    private void AddFromStartMenu(string path)
    {
        var lnkPaths = new List<string>();
        lnkPaths.AddRange(Directory.GetFiles(path, "*.lnk", SearchOption.AllDirectories));
        lnkPaths.AddRange(Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu) + "\\Programs", "*.lnk", SearchOption.AllDirectories));
        var exePaths = lnkPaths.Select(GetTargetPath).ToList();
        ScanApps(exePaths);
    }

    private void AddAppsFromLocation(string path)
    {
        var exePaths = new List<string>();
        exePaths.AddRange(Directory.GetFiles(path, "*.exe", SearchOption.AllDirectories));
        ScanApps(exePaths);
    }

    private void AddAppExeLocation(string path)
    {
        var exePaths = new List<string> { path };
        ScanApps(exePaths, checkExisting: false, skipSetupApps: false);
    }

    private void ScanApps(List<string> exePaths, bool checkExisting = true, bool skipSetupApps = true)
    {
        foreach (var target in exePaths)
        {
            var skip = !File.Exists(target) || Path.GetExtension(target).ToLower() != ".exe";
            skip = skip || (skipSetupApps && (target.Contains("etup") || target.Contains("dotnet") || target.Contains("SETUP")
                                              || target.Contains("edist") || target.Contains("nstall") || string.IsNullOrEmpty(target)));
            skip = skip || (checkExisting && existingApps.Contains(target));
            if (!skip)
            {
                var item = new ProgramItem(target);
                /*if (autoProfileHolder.AutoProfileDict.TryGetValue(target, out AutoProfileEntity autoEntity))
                {
                    item.MatchedAutoProfile = autoEntity;
                }
                */

                ProgramColl.Add(item);
                existingApps.Add(target);
            }
        }
    }

    public void CreateAutoProfileEntry(ProgramItem item)
    {
        if (item.MatchedAutoProfile != null) 
            return;
            
        var tempEntry = new AutoProfileEntity(item.Path, item.Title);
        tempEntry.Turnoff = item.Turnoff;
        var tempIndex = item.SelectedIndexCon1;
        tempEntry.ProfileNames[0] = tempIndex > 0 ? profileList.ProfileListCol[tempIndex - 1].Name :
            AutoProfileEntity.NONE_STRING;

        tempIndex = item.SelectedIndexCon2;
        tempEntry.ProfileNames[1] = tempIndex > 0 ? profileList.ProfileListCol[tempIndex - 1].Name :
            AutoProfileEntity.NONE_STRING;

        tempIndex = item.SelectedIndexCon3;
        tempEntry.ProfileNames[2] = tempIndex > 0 ? profileList.ProfileListCol[tempIndex - 1].Name :
            AutoProfileEntity.NONE_STRING;

        tempIndex = item.SelectedIndexCon4;
        tempEntry.ProfileNames[3] = tempIndex > 0 ? profileList.ProfileListCol[tempIndex - 1].Name :
            AutoProfileEntity.NONE_STRING;

        if (UsingExpandedControllers)
        {
            tempIndex = item.SelectedIndexCon5;
            tempEntry.ProfileNames[4] = tempIndex > 0 ? profileList.ProfileListCol[tempIndex - 1].Name :
                AutoProfileEntity.NONE_STRING;

            tempIndex = item.SelectedIndexCon6;
            tempEntry.ProfileNames[5] = tempIndex > 0 ? profileList.ProfileListCol[tempIndex - 1].Name :
                AutoProfileEntity.NONE_STRING;

            tempIndex = item.SelectedIndexCon7;
            tempEntry.ProfileNames[6] = tempIndex > 0 ? profileList.ProfileListCol[tempIndex - 1].Name :
                AutoProfileEntity.NONE_STRING;

            tempIndex = item.SelectedIndexCon8;
            tempEntry.ProfileNames[7] = tempIndex > 0 ? profileList.ProfileListCol[tempIndex - 1].Name :
                AutoProfileEntity.NONE_STRING;
        }

        item.MatchedAutoProfile = tempEntry;
        AutoProfileHolder.AutoProfileColl.Add(item.MatchedAutoProfile);
    }

    public void PersistAutoProfileEntry(ProgramItem item)
    {
        if (item.MatchedAutoProfile == null)
            return;
            
        var tempEntry = item.MatchedAutoProfile;
        var tempIndex = item.SelectedIndexCon1;
        tempEntry.ProfileNames[0] = tempIndex > 0 ? profileList.ProfileListCol[tempIndex - 1].Name :
            AutoProfileEntity.NONE_STRING;

        tempIndex = item.SelectedIndexCon2;
        tempEntry.ProfileNames[1] = tempIndex > 0 ? profileList.ProfileListCol[tempIndex - 1].Name :
            AutoProfileEntity.NONE_STRING;

        tempIndex = item.SelectedIndexCon3;
        tempEntry.ProfileNames[2] = tempIndex > 0 ? profileList.ProfileListCol[tempIndex - 1].Name :
            AutoProfileEntity.NONE_STRING;

        tempIndex = item.SelectedIndexCon4;
        tempEntry.ProfileNames[3] = tempIndex > 0 ? profileList.ProfileListCol[tempIndex - 1].Name :
            AutoProfileEntity.NONE_STRING;

        if (UsingExpandedControllers)
        {
            tempIndex = item.SelectedIndexCon5;
            tempEntry.ProfileNames[4] = tempIndex > 0 ? profileList.ProfileListCol[tempIndex - 1].Name :
                AutoProfileEntity.NONE_STRING;

            tempIndex = item.SelectedIndexCon6;
            tempEntry.ProfileNames[5] = tempIndex > 0 ? profileList.ProfileListCol[tempIndex - 1].Name :
                AutoProfileEntity.NONE_STRING;

            tempIndex = item.SelectedIndexCon7;
            tempEntry.ProfileNames[6] = tempIndex > 0 ? profileList.ProfileListCol[tempIndex - 1].Name :
                AutoProfileEntity.NONE_STRING;

            tempIndex = item.SelectedIndexCon8;
            tempEntry.ProfileNames[7] = tempIndex > 0 ? profileList.ProfileListCol[tempIndex - 1].Name :
                AutoProfileEntity.NONE_STRING;
        }
    }

    public void RemoveAutoProfileEntry(ProgramItem item)
    {
        AutoProfileHolder.AutoProfileColl.Remove(item.MatchedAutoProfile);
        item.MatchedAutoProfile = null;
    }

    private string GetTargetPath(string filePath) => 
        ResolveMsiShortcut(filePath) ?? ResolveShortcut(filePath);

    public string ResolveShortcutAndArgument(string filePath)
    {
        var typeFromClsid = Type.GetTypeFromCLSID(new Guid("72C24DD5-D70A-438B-8A42-98424B88AFB8")); // Windows Script Host Shell Object
        dynamic shell = Activator.CreateInstance(typeFromClsid!);
        string result;

        try
        {
            var shortcut = shell!.CreateShortcut(filePath);
            result = $"{shortcut.TargetPath} {shortcut.Arguments}";
            Marshal.FinalReleaseComObject(shortcut);
        }
        catch (COMException)
        {
            // A COMException is thrown if the file is not a valid shortcut (.lnk) file 
            result = null;
        }
        finally
        {
            Marshal.FinalReleaseComObject(shell!);
        }

        return result;
    }

    private static string ResolveMsiShortcut(string file)
    {
        var product = new StringBuilder(Msi.MaxGuidLength + 1);
        var feature = new StringBuilder(Msi.MaxFeatureLength + 1);
        var component = new StringBuilder(Msi.MaxGuidLength + 1);

        Msi.MsiGetShortcutTarget(file, product, feature, component);

        var pathLength = Msi.MaxPathLength;
        var path = new StringBuilder(pathLength);

        var installState = Msi.MsiGetComponentPath(product.ToString(), component.ToString(), path, ref pathLength);
        return installState == Msi.InstallState.Local ? path.ToString() : null;
    }

    private static string ResolveShortcut(string filePath)
    {
        var typeFromClsid = Type.GetTypeFromCLSID(new Guid("72C24DD5-D70A-438B-8A42-98424B88AFB8")); // Windows Script Host Shell Object
        dynamic shell = Activator.CreateInstance(typeFromClsid!);
        string result;

        try
        {
            var shortcut = shell!.CreateShortcut(filePath);
            result = shortcut.TargetPath;
            Marshal.FinalReleaseComObject(shortcut);
        }
        catch (COMException)
        {
            // A COMException is thrown if the file is not a valid shortcut (.lnk) file 
            result = null;
        }
        finally
        {
            Marshal.FinalReleaseComObject(shell!);
        }

        return result;
    }

    public bool MoveItemUpDown(ProgramItem item, int moveDirection)
    {
        // Move autoprofile item up (-1) or down (1) both in listView (programColl) and in autoProfileHolder data structure (will be written into AutoProfiles.xml file)
        var itemMoved = true;
        var oldIdx = ProgramColl.IndexOf(item);

        if (moveDirection == -1 && oldIdx > 0 && oldIdx < AutoProfileHolder.AutoProfileColl.Count)
        {
            ProgramColl.Move(oldIdx, oldIdx - 1);
            AutoProfileHolder.AutoProfileColl.Move(oldIdx, oldIdx - 1);
        }
        else if (moveDirection == 1 && oldIdx >= 0 && oldIdx < ProgramColl.Count - 1 && oldIdx < AutoProfileHolder.AutoProfileColl.Count - 1)
        {    
            ProgramColl.Move(oldIdx, oldIdx + 1);
            AutoProfileHolder.AutoProfileColl.Move(oldIdx, oldIdx + 1);
        }
        else
            itemMoved = false;

        return itemMoved;
    }

    public void AddExeToHIDHideWhenSaving(ProgramItem autoProf, bool addExe)
    {
        if (autoProf.Path.Substring((autoProf.Path.Length) - 4, 4) == ".exe") //Filter out autoprofiles that do not lead to EXEs.
        {
            Program.rootHub.CheckHidHidePresence(autoProf.Path, autoProf.FileName, addExe);
        }
    }
}

public class ProgramItem
{
    private string path;
    private string pathLowerCase;
    private string title;
    private string titleLowerCase;
    private AutoProfileEntity matchedAutoProfile;
    private bool turnoff;

    public string Path { get => path;
        set
        {
            if (path == value) return;
            path = value;
            if (matchedAutoProfile != null)
            {
                matchedAutoProfile.Path = value;
            }
        }
    }

    public string Title { get => title;
        set
        {
            if (title == value) return;
            title = value;
            if (matchedAutoProfile != null)
            {
                matchedAutoProfile.Title = value;
            }

            TitleChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler TitleChanged;
    public AutoProfileEntity MatchedAutoProfile
    {
        get => matchedAutoProfile;
        set
        {
            matchedAutoProfile = value;
            if (matchedAutoProfile != null)
            {
                title = matchedAutoProfile.Title ?? string.Empty;
                titleLowerCase = title.ToLower();
            }

            MatchedAutoProfileChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public event EventHandler MatchedAutoProfileChanged;
    //public delegate void AutoProfileHandler(ProgramItem sender, bool added);
    //public event AutoProfileHandler AutoProfileAction;
    public string FileName { get; }
    public ImageSource ExeIcon { get; }

    public bool Turnoff
    {
        get
        {
            var result = turnoff;
            if (matchedAutoProfile != null)
            {
                result = matchedAutoProfile.Turnoff;
            }

            return result;
        }
        set
        {
            turnoff = value;
            if (matchedAutoProfile != null)
            {
                matchedAutoProfile.Turnoff = value;
            }

            TurnoffChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public event EventHandler TurnoffChanged;

    public bool Exists
    {
        get => matchedAutoProfile != null;
    }
    public event EventHandler ExistsChanged;

    private int selectedIndexCon1;
    private int selectedIndexCon2;
    private int selectedIndexCon3;
    private int selectedIndexCon4;
    private int selectedIndexCon5;
    private int selectedIndexCon6;
    private int selectedIndexCon7;
    private int selectedIndexCon8;

    public int SelectedIndexCon1
    {
        get => selectedIndexCon1;
        set
        {
            if (selectedIndexCon1 == value) return;
            selectedIndexCon1 = value;
        }
    }

    public int SelectedIndexCon2
    {
        get => selectedIndexCon2;
        set
        {
            if (selectedIndexCon2 == value) return;
            selectedIndexCon2 = value;
        }
    }
    
    public int SelectedIndexCon3
    {
        get => selectedIndexCon3;
        set
        {
            if (selectedIndexCon3 == value) return;
            selectedIndexCon3 = value;
        }
    }
    
    public int SelectedIndexCon4
    {
        get => selectedIndexCon4;
        set
        {
            if (selectedIndexCon4 == value) return;
            selectedIndexCon4 = value;
        }
    }
    
    public int SelectedIndexCon5
    {
        get => selectedIndexCon5;
        set
        {
            if (selectedIndexCon5 == value) return;
            selectedIndexCon5 = value;
        }
    }
    
    public int SelectedIndexCon6
    {
        get => selectedIndexCon6;
        set
        {
            if (selectedIndexCon6 == value) return;
            selectedIndexCon6 = value;
        }
    }
    
    public int SelectedIndexCon7
    {
        get => selectedIndexCon7;
        set
        {
            if (selectedIndexCon7 == value) return;
            selectedIndexCon7 = value;
        }
    }

    public int SelectedIndexCon8
    {
        get => selectedIndexCon8;
        set
        {
            if (selectedIndexCon8 == value) return;
            selectedIndexCon8 = value;
        }
    }

    public Visibility ExpandedControllersVisible
    {
        get
        {
            var temp = Visibility.Visible;
            if (!IControlService.USING_MAX_CONTROLLERS)
            {
                temp = Visibility.Collapsed;
            }

            return temp;
        }
    }

    public ProgramItem(string path, AutoProfileEntity autoProfileEntity = null)
    {
        this.path = path;
        this.pathLowerCase = path.ToLower();
        FileName = System.IO.Path.GetFileNameWithoutExtension(path);
        this.matchedAutoProfile = autoProfileEntity;
        if (autoProfileEntity != null)
        {
            title = autoProfileEntity.Title;
            titleLowerCase = title.ToLower();
            turnoff = autoProfileEntity.Turnoff;
        }

        if (File.Exists(path))
        {
            using (var ico = Icon.ExtractAssociatedIcon(path))
            {
                ExeIcon = Imaging.CreateBitmapSourceFromHIcon(ico!.Handle, Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
                ExeIcon.Freeze();
            }
        }

        MatchedAutoProfileChanged += ProgramItem_MatchedAutoProfileChanged;
    }

    private void ProgramItem_MatchedAutoProfileChanged(object sender, EventArgs e)
    {
        if (matchedAutoProfile == null)
        {
            selectedIndexCon1 = 0;
            selectedIndexCon2 = 0;
            selectedIndexCon3 = 0;
            selectedIndexCon4 = 0;
            selectedIndexCon5 = 0;
            selectedIndexCon6 = 0;
            selectedIndexCon7 = 0;
            selectedIndexCon8 = 0;
        }

        ExistsChanged?.Invoke(this, EventArgs.Empty);
    }
}