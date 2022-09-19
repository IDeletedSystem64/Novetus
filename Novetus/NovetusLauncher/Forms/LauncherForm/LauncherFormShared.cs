﻿#region Usings
using Mono.Nat;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
#endregion

namespace NovetusLauncher
{
    #region Special Names Definition
    public class SpecialName
    {
        public SpecialName(string text)
        {
            if (text.Contains('|'))
            {
                string[] subs = text.Split('|');
                NameText = subs[0];
                NameID = Convert.ToInt32(subs[1]);
            }
        }

        //text
        public string NameText { get; set; }
        //id
        public int NameID { get; set; }
    }
    #endregion

    #region LauncherForm - Shared
    public class LauncherFormShared
    {
        #region Variables
        public List<TreeNode> CurrentNodeMatches = new List<TreeNode>();
        public int LastNodeIndex = 0;
        public string LastSearchText;
        public bool HideMasterAddressWarning;

        //CONTROLS
        public Form Parent = null;
        public Settings.Style FormStyle = Settings.Style.None;
        public RichTextBox ChangelogBox, ReadmeBox = null;
        public TabControl Tabs = null;
        public TextBox MapDescBox, ServerInfo, SearchBar, PlayerIDTextBox, PlayerNameTextBox, ClientDescriptionBox, IPBox,
            ServerBrowserNameBox, ServerBrowserAddressBox = null;
        public TreeView Tree, _TreeCache = null;
        public ListBox ServerBox, PortBox, ClientBox = null;
        public Label SplashLabel, ProductVersionLabel, NovetusVersionLabel, PlayerTripcodeLabel, IPLabel, PortLabel,
            SelectedClientLabel, SelectedMapLabel, ClientWarningLabel = null;
        public ComboBox StyleSelectorBox = null;
        public CheckBox CloseOnLaunchCheckbox, DiscordPresenceCheckbox, uPnPCheckBox, ShowServerNotifsCheckBox, LocalPlayCheckBox = null;
        public Button RegeneratePlayerIDButton = null;
        public NumericUpDown PlayerLimitBox, HostPortBox = null;
        public string TabPageHost, TabPageMaps, TabPageClients, TabPageSaved, OldIP = "";
        private ToolTip contextToolTip;
        #endregion

        #region Form Event Functions
        public void InitForm()
        {
            HideMasterAddressWarning = false;

            if (FormStyle != Settings.Style.Stylish)
            {
                Parent.Text = "Novetus " + GlobalVars.ProgramInformation.Version;
            }

            if (FormStyle != Settings.Style.Stylish)
            {
                if (File.Exists(GlobalPaths.RootPath + "\\changelog.txt"))
                {
                    ChangelogBox.Text = File.ReadAllText(GlobalPaths.RootPath + "\\changelog.txt");
                }
                else
                {
                    Util.ConsolePrint("ERROR - " + GlobalPaths.RootPath + "\\changelog.txt not found.", 2);
                }

                if (File.Exists(GlobalPaths.RootPath + "\\README-AND-CREDITS.TXT"))
                {
                    ReadmeBox.Text = File.ReadAllText(GlobalPaths.RootPath + "\\README-AND-CREDITS.TXT");
                }
                else
                {
                    Util.ConsolePrint("ERROR - " + GlobalPaths.RootPath + "\\README-AND-CREDITS.TXT not found.", 2);
                }
            }

            if (FormStyle == Settings.Style.Stylish)
            {
                Parent.Text = "Novetus " + GlobalVars.ProgramInformation.Version + " [CLIENT: " + 
                    GlobalVars.UserConfiguration.SelectedClient + " | MAP: " + 
                    GlobalVars.UserConfiguration.Map + "]";
            }

            Splash splash = SplashReader.GetSplash();

            SplashLabel.Text = splash.SplashText;

            if (!string.IsNullOrWhiteSpace(splash.SplashContext))
            {
                contextToolTip = new ToolTip();
                contextToolTip.ToolTipIcon = ToolTipIcon.Info;
                contextToolTip.ToolTipTitle = "Context";
                contextToolTip.SetToolTip(SplashLabel, splash.SplashContext);
            }

            if (FormStyle != Settings.Style.Stylish)
            {
                ProductVersionLabel.Text = Application.ProductVersion;
                NovetusVersionLabel.Text = GlobalVars.ProgramInformation.Version;
                
                ReadConfigValues(true);
            }

            if (FormStyle != Settings.Style.Stylish)
            {
                LocalVars.launcherInitState = false;
            }
        }

        public void CloseEvent(CancelEventArgs e)
        {
            if (GlobalVars.GameOpened != ScriptType.None)
            {
                switch (GlobalVars.GameOpened)
                {
                    case ScriptType.Server:
                        ShowCloseError("A server is open.", "Server", e);
                        break;
                    default:
                        ShowCloseError("A game is open.", "Game", e);
                        break;
                }
            }
            else
            {
                if (GlobalVars.AdminMode)
                {
                    DialogResult closeNovetus = MessageBox.Show("You are in Admin Mode.\nAre you sure you want to quit Novetus?", "Novetus - Admin Mode Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (closeNovetus == DialogResult.No)
                    {
                        e.Cancel = true;
                    }
                    else
                    {
                        CloseEventInternal();
                    }
                }
                else
                {
                    CloseEventInternal();
                }
            }
        }

        public void ShowCloseError(string text, string title, CancelEventArgs e)
        {
            DialogResult closeNovetus = MessageBox.Show(text + "\nYou must close the game before closing Novetus.", "Novetus - " + title + " is Open Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            if (closeNovetus == DialogResult.OK)
            {
                e.Cancel = true;
            }
        }

        public void CloseEventInternal()
        {
            if (!GlobalVars.LocalPlayMode)
            {
                WriteConfigValues();
            }
            if (GlobalVars.UserConfiguration.DiscordPresence)
            {
                DiscordRPC.Shutdown();
            }

            if (!GlobalVars.AppClosed)
            {
                GlobalVars.AppClosed = true;
            }
        }

        public void ChangeTabs()
        {
            switch (Tabs.SelectedTab)
            {
                case TabPage pg2 when pg2 == Tabs.TabPages[TabPageHost]:
                    Tree.Nodes.Clear();
                    _TreeCache.Nodes.Clear();
                    MapDescBox.Text = "";
                    ClientBox.Items.Clear();
                    ServerBox.Items.Clear();
                    PortBox.Items.Clear();
                    string[] text = NovetusFuncs.LoadServerInformation();
                    foreach (string str in text)
                    {
                        if (!string.IsNullOrWhiteSpace(str))
                        {
                            ServerInfo.AppendText(str + Environment.NewLine);
                        }
                    }

                    ServerInfo.SelectionStart = 0;
                    ServerInfo.ScrollToCaret();
                    break;
                case TabPage pg3 when pg3 == Tabs.TabPages[TabPageClients]:
                    string clientdir = GlobalPaths.ClientDir;
                    DirectoryInfo dinfo = new DirectoryInfo(clientdir);
                    DirectoryInfo[] Dirs = dinfo.GetDirectories();
                    foreach (DirectoryInfo dir in Dirs)
                    {
                        ClientBox.Items.Add(dir.Name);
                    }
                    ClientBox.SelectedItem = GlobalVars.UserConfiguration.SelectedClient;
                    Tree.Nodes.Clear();
                    _TreeCache.Nodes.Clear();
                    MapDescBox.Text = "";
                    ServerInfo.Text = "";
                    ServerBox.Items.Clear();
                    PortBox.Items.Clear();
                    break;
                case TabPage pg4 when pg4 == Tabs.TabPages[TabPageMaps]:
                    RefreshMaps();
                    ServerInfo.Text = "";
                    ClientBox.Items.Clear();
                    ServerBox.Items.Clear();
                    PortBox.Items.Clear();
                    break;
                case TabPage pg6 when pg6 == Tabs.TabPages[TabPageSaved]:
                    string[] lines_server = File.ReadAllLines(GlobalPaths.ConfigDir + "\\servers.txt");
                    string[] lines_ports = File.ReadAllLines(GlobalPaths.ConfigDir + "\\ports.txt");
                    ServerBox.Items.AddRange(lines_server);
                    PortBox.Items.AddRange(lines_ports);
                    Tree.Nodes.Clear();
                    _TreeCache.Nodes.Clear();
                    MapDescBox.Text = "";
                    ServerInfo.Text = "";
                    ClientBox.Items.Clear();
                    break;
                default:
                    Tree.Nodes.Clear();
                    _TreeCache.Nodes.Clear();
                    MapDescBox.Text = "";
                    ServerInfo.Text = "";
                    ClientBox.Items.Clear();
                    ServerBox.Items.Clear();
                    PortBox.Items.Clear();
                    break;
            }
        }

        public void StartGame(ScriptType gameType, bool no3d = false, bool nomap = false, bool console = false)
        {
            if (!console)
            {
                if (gameType == ScriptType.Studio)
                {
                    DialogResult result = MessageBox.Show("If you want to test out your place, you will have to save your place in Novetus's map folder, then launch your place in Play Solo." +
                        "\n\nPress Yes to launch Studio with a map, or No to launch Studio without a map.", "Novetus - Launch Roblox Studio", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Information);
                    bool nomapLegacy = false;

                    switch (result)
                    {
                        case DialogResult.Cancel:
                            return;
                        case DialogResult.No:
                            nomapLegacy = true;
                            nomap = nomapLegacy;
                            break;
                        default:
                            break;
                    }
                }

                if (gameType == ScriptType.Server)
                {
                    if (FormStyle == Settings.Style.Stylish)
                    {
                        DialogResult result = MessageBox.Show("You have the option to launch your server with or without graphics. Launching the server without graphics enables better performance.\n" +
                            "However, launching the server with no graphics may cause some elements in later clients may be disabled, such as Dialog boxes. This feature may also make your server unstable.\n\n" +
                            "Press Yes to launch a server with graphics, or No to launch a Server in No3D Mode.", "Novetus - Launch Server", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Information);
                        bool no3dLegacy = false;

                        switch (result)
                        {
                            case DialogResult.Cancel:
                                return;
                            case DialogResult.No:
                                no3dLegacy = true;
                                no3d = no3dLegacy;
                                break;
                            default:
                                break;
                        }
                    }
                    else if (FormStyle != Settings.Style.Stylish && no3d)
                    {
                        DialogResult result = MessageBox.Show("Launching the server without graphics enables better performance.\n" +
                                "However, launching the server with no graphics may cause some elements in later clients may be disabled, such as Dialog boxes. " +
                                "This feature may also make your server unstable.",
                                "Novetus - No3D Warning", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);

                        switch (result)
                        {
                            case DialogResult.Cancel:
                                return;
                            default:
                                break;
                        }
                    }
                }
            }

            if (gameType == ScriptType.Client && GlobalVars.LocalPlayMode && FormStyle != Settings.Style.Stylish)
            {
                GeneratePlayerID();
            }
            else
            {
                WriteConfigValues();
            }

            switch (gameType)
            {
                case ScriptType.Client:
                    ClientManagement.LaunchRBXClient(ScriptType.Client, false, true, new EventHandler(ClientExited));
                    break;
                case ScriptType.Server:
                    ClientManagement.LaunchRBXClient(ScriptType.Server, no3d, false, new EventHandler(ServerExited));
                    break;
                case ScriptType.Solo:
                    ClientManagement.LaunchRBXClient(ScriptType.Solo, false, false, new EventHandler(SoloExited));
                    break;
                case ScriptType.Studio:
                    ClientManagement.LaunchRBXClient(ScriptType.Studio, false, nomap, new EventHandler(ClientExitedBase));
                    break;
                case ScriptType.EasterEgg:
                    ClientManagement.LaunchRBXClient(ScriptType.EasterEgg, false, false, new EventHandler(EasterEggExited));
                    break;
                case ScriptType.None:
                default:
                    break;
            }

            if (GlobalVars.UserConfiguration.CloseOnLaunch && !GlobalVars.isConsoleOnly)
            {
                Parent.Visible = false;
            }
        }

        public void EasterEggLogic()
        {
            if (LocalVars.Clicks < 10)
            {
                LocalVars.Clicks += 1;

                switch (LocalVars.Clicks)
                {
                    case 1:
                        SplashLabel.Text = "Hi " + GlobalVars.UserConfiguration.PlayerName + "!";
                        break;
                    case 3:
                        SplashLabel.Text = "How are you doing today?";
                        break;
                    case 6:
                        SplashLabel.Text = "I just wanted to say something.";
                        break;
                    case 9:
                        SplashLabel.Text = "Just wait a little on the last click, OK?";
                        break;
                    case 10:
                        SplashLabel.Text = "Thank you. <3";
                        StartGame(ScriptType.EasterEgg);
                        break;
                    default:
                        break;
                }
            }
        }

        void ClientExited(object sender, EventArgs e)
        {
            if (!GlobalVars.LocalPlayMode && GlobalVars.GameOpened != ScriptType.Server)
            {
                GlobalVars.GameOpened = ScriptType.None;
            }
            ClientExitedBase(sender, e);
        }

        void SoloExited(object sender, EventArgs e)
        {
            if (GlobalVars.GameOpened != ScriptType.Studio)
            {
                GlobalVars.GameOpened = ScriptType.None;
            }
            ClientExitedBase(sender, e);
        }

        void ServerExited(object sender, EventArgs e)
        {
            GlobalVars.GameOpened = ScriptType.None;
            NovetusFuncs.PingMasterServer(false, "The server has removed itself from the master server list.");
            ClientExitedBase(sender, e);
        }

        void EasterEggExited(object sender, EventArgs e)
        {
            GlobalVars.GameOpened = ScriptType.None;
            SplashLabel.Text = LocalVars.prevsplash;
            if (GlobalVars.AdminMode)
            {
                LocalVars.Clicks = 0;
            }
            ClientExitedBase(sender, e);
        }

        void ClientExitedBase(object sender, EventArgs e)
        {
            ClientManagement.UpdateRichPresence(ClientManagement.GetStateForType(GlobalVars.GameOpened));

            if (GlobalVars.UserConfiguration.CloseOnLaunch)
            {
                Parent.Visible = true;
            }

            if (GlobalVars.isConsoleOnly)
            {
                CloseEventInternal();
            }
        }

        // FINALLY. https://stackoverflow.com/questions/11530643/treeview-search
        public TreeNode SearchMapsInternal(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return null;
            };

            try
            {
                if (LastSearchText != searchText)
                {
                    //It's a new Search
                    CurrentNodeMatches.Clear();
                    LastSearchText = searchText;
                    LastNodeIndex = 0;
                    SearchNodes(searchText, Tree.Nodes[0]);
                }

                if (LastNodeIndex >= 0 && CurrentNodeMatches.Count > 0 && LastNodeIndex < CurrentNodeMatches.Count)
                {
                    TreeNode selectedNode = CurrentNodeMatches[LastNodeIndex];
                    LastNodeIndex++;
                    return selectedNode;
                }
                else
                {
                    //It's a new Search
                    CurrentNodeMatches.Clear();
                    LastSearchText = searchText;
                    LastNodeIndex = 0;
                    SearchNodes(searchText, Tree.Nodes[0]);
                    TreeNode selectedNode = CurrentNodeMatches[LastNodeIndex];
                    LastNodeIndex++;
                    return selectedNode;
                }
            }
            catch (Exception ex)
            {
                Util.LogExceptions(ex);
                MessageBox.Show("The map '" + searchText + "' cannot be found. Please try another term.", "Novetus - Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        public void SearchMaps()
        {
            TreeNode node = SearchMapsInternal(SearchBar.Text);

            if (node != null)
            {
                Tree.SelectedNode = node;
                Tree.SelectedNode.Expand();
                Tree.Select();
            }
        }

        public void LoadLauncher()
        {
            NovetusSDK im = new NovetusSDK();
            im.Show();
            Util.ConsolePrint("Novetus SDK Launcher Loaded.", 4);
        }

        public void SwitchStyles()
        {
            if (LocalVars.launcherInitState)
                return;

            if (GlobalVars.AdminMode)
            {
                DialogResult closeNovetus = MessageBox.Show("You are in Admin Mode.\nAre you sure you want to switch styles?", "Novetus - Admin Mode Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (closeNovetus == DialogResult.No)
                {
                    return;
                }
            }

            if (GlobalVars.GameOpened != ScriptType.None)
            {
                MessageBox.Show("You must close the currently open client before changing styles.", "Novetus - Client is Open Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            switch (StyleSelectorBox.SelectedIndex)
            {
                case 0:
                    if (FormStyle != Settings.Style.Extended)
                    {
                        GlobalVars.UserConfiguration.LauncherStyle = Settings.Style.Extended;
                        RestartApp();
                    }
                    break;
                case 1:
                    if (FormStyle != Settings.Style.Compact)
                    {
                        GlobalVars.UserConfiguration.LauncherStyle = Settings.Style.Compact;
                        RestartApp();
                    }
                    break;
                case 2:
                    if (FormStyle != Settings.Style.Stylish)
                    {
                        GlobalVars.UserConfiguration.LauncherStyle = Settings.Style.Stylish;
                        RestartApp();
                    }
                    break;
                default:
                    break;
            }
        }

        public void RestartApp()
        {
            var process = Process.GetCurrentProcess();
            Process.Start(process.GetFilePath(), process.GetCommandLine());
            CloseEventInternal();
        }

        public void ReadConfigValues(bool initial = false)
        {
            FileManagement.Config(GlobalPaths.ConfigDir + "\\" + GlobalPaths.ConfigName, false);

            CloseOnLaunchCheckbox.Checked = GlobalVars.UserConfiguration.CloseOnLaunch;
            PlayerIDTextBox.Text = GlobalVars.UserConfiguration.UserID.ToString();
            PlayerTripcodeLabel.Text = GlobalVars.PlayerTripcode.ToString();
            PlayerLimitBox.Value = Convert.ToDecimal(GlobalVars.UserConfiguration.PlayerLimit);
            PlayerNameTextBox.Text = GlobalVars.UserConfiguration.PlayerName;
            SelectedClientLabel.Text = GlobalVars.UserConfiguration.SelectedClient;
            SelectedMapLabel.Text = GlobalVars.UserConfiguration.Map;
            Tree.SelectedNode = TreeNodeHelper.SearchTreeView(GlobalVars.UserConfiguration.Map, Tree.Nodes);
            Tree.Focus();
            IPBox.Text = GlobalVars.CurrentServer.ToString();
            HostPortBox.Value = Convert.ToDecimal(GlobalVars.UserConfiguration.RobloxPort);
            IPLabel.Text = GlobalVars.CurrentServer.ServerIP;
            PortLabel.Text = GlobalVars.CurrentServer.ServerPort.ToString();
            DiscordPresenceCheckbox.Checked = GlobalVars.UserConfiguration.DiscordPresence;
            uPnPCheckBox.Checked = GlobalVars.UserConfiguration.UPnP;
            ShowServerNotifsCheckBox.Checked = GlobalVars.UserConfiguration.ShowServerNotifications;
            ServerBrowserNameBox.Text = GlobalVars.UserConfiguration.ServerBrowserServerName;
            ServerBrowserAddressBox.Text = GlobalVars.UserConfiguration.ServerBrowserServerAddress;

            switch (GlobalVars.UserConfiguration.LauncherStyle)
            {
                case Settings.Style.Compact:
                    StyleSelectorBox.SelectedIndex = 1;
                    break;
                case Settings.Style.Extended:
                    StyleSelectorBox.SelectedIndex = 0;
                    break;
                case Settings.Style.Stylish:
                default:
                    StyleSelectorBox.SelectedIndex = 2;
                    break;
            }

            Util.ConsolePrint("Config loaded.", 3);
            ReadClientValues(initial);
        }

        public void WriteConfigValues(bool ShowBox = false)
        {
            FileManagement.Config(GlobalPaths.ConfigDir + "\\" + GlobalPaths.ConfigName, true);
            ClientManagement.ReadClientValues();
            Util.ConsolePrint("Config Saved.", 3);
            if (ShowBox)
            {
                MessageBox.Show("Config Saved!", "Novetus - Config Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        public void WriteCustomizationValues()
        {
            FileManagement.Customization(GlobalPaths.ConfigDir + "\\" + GlobalPaths.ConfigNameCustomization, true);
            Util.ConsolePrint("Config Saved.", 3);
        }

        public void ResetConfigValues(bool ShowBox = false)
        {
            //https://stackoverflow.com/questions/9029351/close-all-open-forms-except-the-main-menu-in-c-sharp
            List<Form> openForms = new List<Form>();

            foreach (Form f in Application.OpenForms)
                openForms.Add(f);

            foreach (Form f in openForms)
            {
                if (f.Name != Parent.Name)
                    f.Close();
            }

            FileManagement.ResetConfigValues(FormStyle);
            WriteConfigValues();
            ReadConfigValues();
            if (ShowBox)
            {
                MessageBox.Show("Config Reset!", "Novetus - Config Reset", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        public bool GenerateIfInvalid()
        {
            string clientpath = GlobalPaths.ClientDir + @"\\" + GlobalVars.UserConfiguration.SelectedClient + @"\\clientinfo.nov";

            if (!File.Exists(clientpath))
            {
                try
                {
                    MessageBox.Show("No clientinfo.nov detected with the client you chose. The client either cannot be loaded, or it is not available.\n\nNovetus will attempt to generate one.", "Novetus - Client Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    ClientManagement.GenerateDefaultClientInfo(Path.GetDirectoryName(clientpath));
                }
                catch (Exception ex)
                {
                    Util.LogExceptions(ex);
                    MessageBox.Show("Failed to generate default clientinfo.nov. Info: " + ex.Message + "\n\nLoading default client '" + GlobalVars.ProgramInformation.DefaultClient + "'", "Novetus - Client Info Generation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    GlobalVars.UserConfiguration.SelectedClient = GlobalVars.ProgramInformation.DefaultClient;
                    return false;
                }
            }

            return true;
        }

        public void ReadClientValues(bool initial = false)
        {
            //reset clients
            if (!GenerateIfInvalid())
            {
                if (Tabs.SelectedTab == Tabs.TabPages[TabPageClients])
                {
                    ClientBox.SelectedItem = GlobalVars.UserConfiguration.SelectedClient;
                }
            }

            ClientManagement.ReadClientValues(initial);

            PlayerNameTextBox.Enabled = GlobalVars.SelectedClientInfo.UsesPlayerName;

            PlayerIDTextBox.Enabled = GlobalVars.SelectedClientInfo.UsesID;
            RegeneratePlayerIDButton.Enabled = GlobalVars.SelectedClientInfo.UsesID;

            switch (GlobalVars.SelectedClientInfo.UsesID)
            {
                case true:
                    if (GlobalVars.CurrentServer.ServerIP.Equals("localhost"))
                    {
                        LocalPlayCheckBox.Enabled = true;
                    }
                    break;
                case false:
                    LocalPlayCheckBox.Enabled = false;
                    GlobalVars.LocalPlayMode = false;
                    break;
            }

            if (!string.IsNullOrWhiteSpace(GlobalVars.SelectedClientInfo.Warning))
            {
                ClientWarningLabel.Text = GlobalVars.SelectedClientInfo.Warning;
            }
            else
            {
                ClientWarningLabel.Text = "";
            }

            ClientDescriptionBox.Text = GlobalVars.SelectedClientInfo.Description;
            SelectedClientLabel.Text = GlobalVars.UserConfiguration.SelectedClient;
        }

        public void GeneratePlayerID()
        {
            NovetusFuncs.GeneratePlayerID();
            PlayerIDTextBox.Text = Convert.ToString(GlobalVars.UserConfiguration.UserID);
        }

        public async void InstallAddon()
        {
            ModManager addon = new ModManager(ModManager.ModMode.ModInstallation);
            addon.setFileListDisplay(10);
            try
            {
                await addon.LoadMod();
                if (!string.IsNullOrWhiteSpace(addon.getOutcome()))
                {
                    Util.ConsolePrint("ModManager - " + addon.getOutcome(), 3);
                }
            }
            catch (Exception ex)
            {
                Util.LogExceptions(ex);
                if (!string.IsNullOrWhiteSpace(addon.getOutcome()))
                {
                    Util.ConsolePrint("ModManager - " + addon.getOutcome(), 2);
                }
            }

            if (!string.IsNullOrWhiteSpace(addon.getOutcome()))
            {
                MessageBoxIcon boxicon = MessageBoxIcon.Information;

                if (addon.getOutcome().Contains("Error"))
                {
                    boxicon = MessageBoxIcon.Error;
                }

                MessageBox.Show(addon.getOutcome(), "Novetus - Mod Installed", MessageBoxButtons.OK, boxicon);
            }
        }

        public void ClearAssetCache()
        {
            if (Directory.Exists(GlobalPaths.AssetCacheDir))
            {
                Directory.Delete(GlobalPaths.AssetCacheDir, true);
                FileManagement.CreateAssetCacheDirectories();
                Util.ConsolePrint("Asset cache cleared!", 3);
                MessageBox.Show("Asset cache cleared!", "Novetus - Asset Cache Cleared", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("There is no asset cache to clear.", "Novetus - Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void RefreshMaps()
        {
            FileManagement.ResetMapIfNecessary();

            Tree.Nodes.Clear();
            _TreeCache.Nodes.Clear();
            string mapdir = GlobalPaths.MapsDir;
            string[] filePaths = Util.FindAllFiles(GlobalPaths.MapsDir);

            foreach (string path in filePaths)
            {
                Util.RenameFileWithInvalidChars(path);
            }

            string[] fileexts = new string[] { ".rbxl", ".rbxlx" };
            TreeNodeHelper.ListDirectory(Tree, mapdir, fileexts);
            TreeNodeHelper.CopyNodes(Tree.Nodes, _TreeCache.Nodes);
            Tree.SelectedNode = TreeNodeHelper.SearchTreeView(GlobalVars.UserConfiguration.Map, Tree.Nodes);
            if (FormStyle == Settings.Style.Stylish)
            {
                Tree.SelectedNode.BackColor = SystemColors.Highlight;
                Tree.SelectedNode.ForeColor = SystemColors.HighlightText;
            }
            Tree.Focus();

            if (FormStyle != Settings.Style.Stylish)
            {
                LoadMapDesc();
            }
        }

        public void RestartLauncherAfterSetting(CheckBox box, string title, string subText)
        {
            RestartLauncherAfterSetting(box.Checked, title, subText);
        }

        public void RestartLauncherAfterSetting(bool check, string title, string subText)
        {
            if (GlobalVars.AdminMode)
            {
                DialogResult closeNovetus = MessageBox.Show("You are in Admin Mode.\nAre you sure you want to apply this setting?", "Novetus - Admin Mode Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (closeNovetus == DialogResult.No)
                {
                    return;
                }
            }

            if (GlobalVars.GameOpened != ScriptType.None)
            {
                MessageBox.Show("You must close the currently open client before this setting can be applied.", "Novetus - Client is Open Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            switch (check)
            {
                case false:
                    MessageBox.Show("Novetus will now restart.", title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    break;
                default:
                    MessageBox.Show("Novetus will now restart." + Environment.NewLine + subText, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    break;
            }

            CloseEventInternal();
            Application.Restart();
        }

        public void SelectMap()
        {
            if (Tree.SelectedNode.Nodes.Count == 0)
            {
                GlobalVars.UserConfiguration.Map = Tree.SelectedNode.Text.ToString();
                GlobalVars.UserConfiguration.MapPathSnip = Tree.SelectedNode.FullPath.ToString().Replace(@"\", @"\\");
                GlobalVars.UserConfiguration.MapPath = GlobalPaths.BasePath + @"\\" + GlobalVars.UserConfiguration.MapPathSnip;

                if (FormStyle != Settings.Style.Stylish)
                {
                    SelectedMapLabel.Text = GlobalVars.UserConfiguration.Map;
                    LoadMapDesc();
                }
            }
        }

        private void LoadMapDesc()
        {
            if (Tree.SelectedNode == null)
                return;

            if (File.Exists(GlobalPaths.RootPath + @"\\" + Tree.SelectedNode.FullPath.Replace(".rbxl", "").Replace(".rbxlx", "") + "_desc.txt"))
            {
                MapDescBox.Text = File.ReadAllText(GlobalPaths.RootPath + @"\\" + Tree.SelectedNode.FullPath.Replace(".rbxl", "").Replace(".rbxlx", "") + "_desc.txt");
            }
            else
            {
                MapDescBox.Text = Tree.SelectedNode.Text;
            }
        }

        public void AddIPPortListing(ListBox box, string file, object val)
        {
            File.AppendAllText(file, val + Environment.NewLine);

            if (box != null)
            {
                box.Items.Clear();
                string[] lines = File.ReadAllLines(file);
                box.Items.AddRange(lines);
            }
        }

        public void ResetIPPortListing(ListBox box, string file)
        {
            File.Create(file).Dispose();

            if (box != null)
            {
                box.Items.Clear();
                string[] lines = File.ReadAllLines(file);
                box.Items.AddRange(lines);
            }
        }

        public void RemoveIPPortListing(ListBox box, string file, string file_tmp)
        {
            if (box != null)
            {
                if (box.SelectedIndex >= 0)
                {
                    TextLineRemover.RemoveTextLines(new List<string> { box.SelectedItem.ToString() }, file, file_tmp);
                    box.Items.Clear();
                    string[] lines = File.ReadAllLines(file);
                    box.Items.AddRange(lines);
                }
            }
            else
            {
                //requires a ListBox.
                return;
            }
        }

        public void SelectIPListing()
        {
            GlobalVars.CurrentServer.ServerIP = ServerBox.SelectedItem.ToString();
            LocalPlayCheckBox.Enabled = false;
            GlobalVars.LocalPlayMode = false;
            IPLabel.Text = GlobalVars.CurrentServer.ServerIP;
            IPBox.Text = GlobalVars.CurrentServer.ToString();
        }

        public void SelectPortListing()
        {
            GlobalVars.CurrentServer.ServerPort = Convert.ToInt32(PortBox.SelectedItem.ToString());
            IPBox.Text = GlobalVars.CurrentServer.ToString();
        }

        public void ResetCurPort(NumericUpDown box, int value)
        {
            box.Value = Convert.ToDecimal(GlobalVars.DefaultRobloxPort);
            value = GlobalVars.DefaultRobloxPort;
        }

        public void ChangeServerAddress()
        {
            GlobalVars.CurrentServer.SetValues(IPBox.Text);
            PortLabel.Text = GlobalVars.CurrentServer.ServerPort.ToString();
            IPLabel.Text = GlobalVars.CurrentServer.ServerIP;

            switch (GlobalVars.SelectedClientInfo.UsesID)
            {
                case true:
                    if (GlobalVars.CurrentServer.ServerIP.Equals("localhost"))
                    {
                        LocalPlayCheckBox.Enabled = true;
                    }
                    break;
                case false:
                    LocalPlayCheckBox.Enabled = false;
                    GlobalVars.LocalPlayMode = false;
                    break;
            }
        }

        public void ChangeServerPort()
        {
            GlobalVars.UserConfiguration.RobloxPort = Convert.ToInt32(HostPortBox.Value);
        }

        public void ChangeClient()
        {
            if (ClientBox.Items.Count == 0)
                return;

            string clientdir = GlobalPaths.ClientDir;
            DirectoryInfo dinfo = new DirectoryInfo(clientdir);
            DirectoryInfo[] Dirs = dinfo.GetDirectories();
            List<string> clientNameList = new List<string>();
            foreach (DirectoryInfo dir in Dirs)
            {
                clientNameList.Add(dir.Name);
            }

            if (ClientBox.Items.Count == (clientNameList.Count - 1))
                return;

            if (ClientBox.SelectedItem == null)
                return;

            string ourselectedclient = GlobalVars.UserConfiguration.SelectedClient;

            if (GlobalVars.GameOpened != ScriptType.None && !ourselectedclient.Equals(ClientBox.SelectedItem.ToString()))
            {
                MessageBox.Show("You must close the currently open client before changing clients.", "Novetus - Client is Open Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            GlobalVars.UserConfiguration.SelectedClient = ClientBox.SelectedItem.ToString();

            if (!string.IsNullOrWhiteSpace(ourselectedclient))
            {
                if (!ourselectedclient.Equals(GlobalVars.UserConfiguration.SelectedClient))
                {
                    ReadClientValues(true);
                }
                else
                {
                    ReadClientValues();
                }
            }
            else
            {
                return;
            }

            ClientManagement.UpdateRichPresence(ClientManagement.GetStateForType(GlobalVars.GameOpened));

            FormCollection fc = Application.OpenForms;

            foreach (Form frm in fc)
            {
                //iterate through
                if (frm.Name == "CustomGraphicsOptions")
                {
                    frm.Close();
                    break;
                }
            }
        }

        public int GetSpecialNameID(string text)
        {
            string[] names = File.ReadAllLines(GlobalPaths.ConfigDir + "\\names-special.txt");
            int returnname = 0;
            List<SpecialName> specialnames = new List<SpecialName>();

            foreach (var name in names)
            {
                specialnames.Add(new SpecialName(name));
            }

            foreach (var specialname in specialnames)
            {
                if (specialname.NameText.Equals(text, StringComparison.InvariantCultureIgnoreCase))
                {
                    returnname = specialname.NameID;
                    break;
                }
            }

            return returnname;
        }

        public void ChangeName()
        {
            GlobalVars.UserConfiguration.PlayerName = PlayerNameTextBox.Text;
            int autoNameID = GetSpecialNameID(GlobalVars.UserConfiguration.PlayerName);
            if (LocalVars.launcherInitState == false && autoNameID > 0)
            {
                PlayerIDTextBox.Text = autoNameID.ToString();
            }
        }

        public void ChangeUserID()
        {
            int parsedValue;
            if (int.TryParse(PlayerIDTextBox.Text, out parsedValue))
            {
                if (PlayerIDTextBox.Text.Equals(""))
                {
                    GlobalVars.UserConfiguration.UserID = 0;
                }
                else
                {
                    GlobalVars.UserConfiguration.UserID = Convert.ToInt32(PlayerIDTextBox.Text);
                }
            }
            else
            {
                GlobalVars.UserConfiguration.UserID = 0;
            }
        }

        public void ShowMasterServerWarning()
        {
            if (!HideMasterAddressWarning)
            {
                DialogResult res = MessageBox.Show("Due to Novetus' open nature when it comes to hosting master servers, hosting on a public master server may leave your server (and potentially computer) open for security vulnerabilities." +
                "\nTo protect yourself against this, host under a VPN, use a host name, or use a trustworthy master server that is hosted privately or an official server." +
                "\n\nDo you trust the master server you're about to input in?", "Novetus - Master Server Security Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                switch (res)
                {
                    case DialogResult.Yes:
                        break;
                    case DialogResult.No:
                    default:
                        ServerBrowserAddressBox.Text = "localhost";
                        break;
                }

                HideMasterAddressWarning = true;
            }
        }

        public void AddNewMap()
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "Roblox Level (*.rbxl)|*.rbxl|Roblox Level (*.rbxlx)|*.rbxlx";
                ofd.FilterIndex = 1;
                ofd.Title = "Load Roblox map";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    if (!Directory.Exists(GlobalPaths.MapsDirCustom))
                    {
                        Directory.CreateDirectory(GlobalPaths.MapsDirCustom);
                    }

                    string mapname = Path.GetFileName(ofd.FileName);
                    bool success = true;

                    try
                    {
                        Util.FixedFileCopy(ofd.FileName, GlobalPaths.MapsDirCustom + @"\\" + mapname, true, true);
                    }
                    catch (Exception ex)
                    {
                        Util.LogExceptions(ex);
                        MessageBox.Show("Novetus has experienced an error when adding your map file: " + ex.Message + "\n\nYour file has not been added. Please try again.", "Novetus - Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        success = false;
                    }
                    finally
                    {
                        if (success)
                        {
                            RefreshMaps();
                            MessageBox.Show("The map '" + mapname + "' was successfully added to Novetus! Look in the 'Custom' folder for it!" , "Novetus - Map Added", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
            }
        }

        public void LoadSettings()
        {
            LauncherFormSettings im = new LauncherFormSettings();
            im.FormClosing += SettingsExited;
            im.Show();
        }

        void SettingsExited(object sender, FormClosingEventArgs e)
        {
            ClientManagement.ReadClientValues();
        }

        #endregion

        #region Helper Functions
        public void SearchNodes(string SearchText, TreeNode StartNode)
        {
            while (StartNode != null)
            {
                if (StartNode.Text.ToLower().Contains(SearchText.ToLower()))
                {
                    CurrentNodeMatches.Add(StartNode);
                };
                if (StartNode.Nodes.Count != 0)
                {
                    SearchNodes(SearchText, StartNode.Nodes[0]);//Recursive Search 
                };
                StartNode = StartNode.NextNode;
            };

        }
        #endregion
    }
    #endregion
}
