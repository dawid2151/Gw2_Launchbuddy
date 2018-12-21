﻿using Gw2_Launchbuddy.ObjectManagers;
using IWshRuntimeLibrary;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Xml;

namespace Gw2_Launchbuddy
{
    public partial class MainWindow : Window
    {


        ///Gw2 Launchbuddy by TheCheatsrichter 2016
        ///
        ///Argument generator and shortcut creator for Guild Wars 2

        /// Object prefix:
        ///     bt= button
        ///     lab = label
        ///     cb = checkbox
        ///
        ///##########################################
        ///
        
        private bool cinemamode = false;
        private bool slideshowthread_isrunning = false;

        private int reso_x, reso_y;

        private SortAdorner listViewSortAdorner = null;
        private GridViewColumnHeader listViewSortCol = null;

        public class CinemaImage
        {
            public string Name
            {
                set { }
                get { return System.IO.Path.GetFileName(Path); }
            }

            public string Path { set; get; }

            public CinemaImage(string Path)
            {
                this.Path = Path;
            }
        }

        public MainWindow()
        {
            try
            {             
                InitializeComponent();
                if (!Directory.Exists(Globals.AppDataPath))
                {
                    Directory.CreateDirectory(Globals.AppDataPath);
                }
            }
            catch
            {
                Properties.Settings.Default.Reset();
            }
            Init();
        }

        private void UIInit()
        {
            //Account Setups
            lv_accs.ItemsSource = lv_accssettings.ItemsSource = AccountManager.Accounts;

            //CrashAnalyser
            lv_crashlogs.ItemsSource = CrashAnalyzer.Crashlogs;

        }

        public void Init()
        {
#if !DEBUG
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledExceptionReport);

            //LB statistics
            Properties.Settings.Default.counter_launches += 1;
            Properties.Settings.Default.Save();
#else
            System.Diagnostics.Debug.WriteLine("Compiled without crash handler, and always first run.");
            Properties.Settings.Default.counter_launches = 1;
#endif


            //Setup
            DonatePopup();
            UIInit();
            
            /*Thread checkver = new Thread(checkversion);
            checkver.IsBackground = true;
            checkver.Start();*/
            cinema_setup();
            Mainwin_LoadSetup(); //do this after cinema setup!

            //Cinema_Accountlist.ItemsSource= AccountManager.Accounts;
            //lv_accs.ItemsSource = AccountManager.Accounts;
            //lv_accssettings.ItemsSource = AccountManager.Accounts;
            //argListBox.ItemsSource = AccountArgumentManager.AccountArgumentCollection.Where(a => a.Argument.Active && a.Account == AccountManager.DefaultAccount);


            LoadAddons();
            SettingsTabSetup();
            AddOnManager.LaunchLbAddons();
            if (Properties.Settings.Default.notifylbupdate)
            {
                Thread checklbver = new Thread(checklbversion);
                checklbver.Start();
            }

        }

        private void LoadDlls()
        {
            lv_InjectDlls.ItemsSource= DllInjector.LoadDlls();
        }

        private void Mainwin_LoadSetup()
        {
            if (Properties.Settings.Default.mainwin_pos_x >=0 && Properties.Settings.Default.mainwin_pos_y >= 0)
            {
                this.Top = Properties.Settings.Default.mainwin_pos_x;
                this.Left = Properties.Settings.Default.mainwin_pos_y;
            }

            if (Properties.Settings.Default.mainwin_size_x >= 100 && Properties.Settings.Default.mainwin_size_y >= 100)
            {
                this.Height = Properties.Settings.Default.mainwin_size_y;
                this.Width = Properties.Settings.Default.mainwin_size_x;
            }
        }

        private void Mainwin_SaveSetup()
        {
            Properties.Settings.Default.mainwin_pos_x = this.Left;
            Properties.Settings.Default.mainwin_pos_y = this.Top;
            Properties.Settings.Default.mainwin_size_y = this.Height;
            Properties.Settings.Default.mainwin_size_x = this.Width;
        }

        private void SettingsTabSetup()
        {
            cb_lbupdatescheck.IsChecked = Properties.Settings.Default.notifylbupdate;
            cb_useinstancegui.IsChecked = Properties.Settings.Default.useinstancegui;
            cb_useloadingui.IsChecked = Properties.Settings.Default.useloadingui;
        }

        private void DonatePopup()
        {
            if ((Properties.Settings.Default.counter_launches % 100) == 5)
            {
                Popup popup = new Popup();
                popup.Show();
            }
        }

        private void checklbversion()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                bt_downloadrelease.Content = "Fetching Release List please wait";
            }));

            VersionSwitcher.CheckForUpdate();
            Dispatcher.Invoke(new Action(() =>
            {
                lv_lbversions.ItemsSource = VersionSwitcher.Releaselist;
                bt_downloadrelease.Content = "Download";
            }));
        }

        private static void UnhandledExceptionReport(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            CrashReporter.ReportCrashToAll(e);
        }

        private void slideshow_diashow(string imagespath)
        {
            bool isactive = false;
            string[] files = Directory.GetFiles(imagespath, "*.*", SearchOption.AllDirectories).Where(a => a.EndsWith(".png") || a.EndsWith(".jpg") || a.EndsWith(".jpeg") || a.EndsWith(".bmp")).ToArray<string>();
            Random rnd = new Random();
            BitmapSource startimg = LoadImage(files[rnd.Next(files.Length - 1)]);
            startimg.Freeze();

            Dispatcher.Invoke(new Action(() =>
            {
                img_slideshow.Source = startimg;
            }));

            while (true)
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    isactive = myWindow.IsActive;
                }));

                while (isactive)
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        Storyboard ImgFadeIn = (Storyboard)FindResource("anim_imgfadein");
                        ImgFadeIn.Begin();
                    }));
                    Thread.Sleep(1000);
                    int nr = rnd.Next(files.Length - 1);
                    BitmapSource currentimg = LoadImage(files[nr]);
                    currentimg.Freeze();
                    Thread.Sleep(5000); //Time how long the picture is actually displayed
                    Dispatcher.Invoke(new Action(() =>
                    {
                        Storyboard ImgFadeOut = (Storyboard)FindResource("anim_imgfadeout");
                        ImgFadeOut.Begin();
                    }));
                    Thread.Sleep(1000);
                    Dispatcher.Invoke(new Action(() =>
                    {
                        img_slideshow.Source = currentimg;
                    }));

                    Thread.Sleep(1000);
                }
                Thread.Sleep(3000);
            }
        }

        private bool cinema_checksetup(bool checkslideshow, bool checkvideomode)
        {
            string imagespath = Properties.Settings.Default.cinema_imagepath;
            string musicpath = Properties.Settings.Default.cinema_musicpath;
            string maskpath = Properties.Settings.Default.cinema_maskpath;
            string videopath = Properties.Settings.Default.cinema_videopath;
            string loginwindowpath = Properties.Settings.Default.cinema_loginwindowpath;
            var backgroundcolor = Properties.Settings.Default.cinema_backgroundcolor;

            string[] musicext = {
                ".WAV", ".MID", ".MIDI", ".WMA", ".MP3", ".OGG", ".RMA",
                ".AVI", ".MP4", ".DIVX", ".WMV",
            };

            string[] videoext = {
                ".AVI", ".MP4", ".DIVX", ".WMV", ".M4A",
            };

            string[] picext = {
                ".PNG", ".JPG", ".JPEG", ".BMP", ".GIF",
            };

            bool invalid = false;

            if (checkvideomode)
            {
                //Check all needed VideoMode resources here
                if (!videoext.Contains(Path.GetExtension(videopath), StringComparer.OrdinalIgnoreCase) || !System.IO.File.Exists(videopath))
                {
                    MessageBox.Show("Invalid video file detected! File could not be found / is not a video file.\n File path: " + videopath + "\n\nPlease choose another file in the cinema mode section!");
                    invalid = true;
                }
            }

            if (checkslideshow)
            {
                //Check all needed SlideshowMode resources here
                if (!musicext.Contains(Path.GetExtension(musicpath), StringComparer.OrdinalIgnoreCase) || !System.IO.File.Exists(musicpath))
                {
                    MessageBox.Show("Invalid music file detected! File could not be found / is not a music file.\n File path: " + musicpath + "\n\nPlease choose another file in the cinema mode section!");
                    invalid = true;
                }

                if (!picext.Contains(Path.GetExtension(maskpath), StringComparer.OrdinalIgnoreCase) || !System.IO.File.Exists(maskpath))
                {
                    MessageBox.Show("Invalid mask file detected! File could not be found / is not a picture file.\n File path: " + maskpath + "\n\nPlease choose another file in the cinema mode section!");
                    invalid = true;
                }

                if (!Directory.Exists(imagespath) || Directory.GetFiles(imagespath, "*.*", SearchOption.AllDirectories).Where(a => a.EndsWith(".png") || a.EndsWith(".jpg") || a.EndsWith(".jpeg") || a.EndsWith(".bmp")).ToArray<string>().Length <= 0)
                {
                    MessageBox.Show("Invalid image folder detected! No Images could be found at the chosen location! \n File path: " + imagespath + "\n\nPlease choose another folder in the cinema mode section!");
                    invalid = true;
                }
            }

            //General needed resources
            if ((!picext.Contains(Path.GetExtension(loginwindowpath), StringComparer.OrdinalIgnoreCase) || !System.IO.File.Exists(loginwindowpath)) && loginwindowpath != "")
            {
                MessageBox.Show("Invalid login window file detected! File could not be found / is not a picture file.\n File path: " + loginwindowpath + "\n\nPlease choose another file in the cinema mode section!");
                invalid = true;
            }

            if (invalid) return false;

            return true;
        }

        private void cinema_setup()
        {
            bool videomode = Properties.Settings.Default.cinema_video;
            bool slideshowmode = Properties.Settings.Default.cinema_slideshow;
            cinemamode = Properties.Settings.Default.cinema_use;
            if (cinemamode) cinemamode = cinema_checksetup(slideshowmode, videomode);
            Properties.Settings.Default.cinema_use = cinemamode;
            Properties.Settings.Default.Save();
            LoadCinemaSettings();

            string musicpath = Properties.Settings.Default.cinema_musicpath;
            string imagespath = Properties.Settings.Default.cinema_imagepath;
            string maskpath = Properties.Settings.Default.cinema_maskpath;
            string videopath = Properties.Settings.Default.cinema_videopath;
            string loginwindowpath = Properties.Settings.Default.cinema_loginwindowpath;
            var backgroundcolor = Properties.Settings.Default.cinema_backgroundcolor;

            //Settings UI Setup
            try
            {
                cinema_videoplayback.Source = new Uri(videopath, UriKind.Relative);
            }
            catch (Exception err)
            {
                MessageBox.Show("Video File for Video Mode could not be found! \nPath: " + videopath + "\n" + err.Message);
            }
            if (videomode && !slideshowmode) rb_cinemavideomode.IsChecked = true;
            if (!videomode && slideshowmode) rb_cinemaslideshowmode.IsChecked = true;
            ClrPcker_Background.SelectedColor = backgroundcolor;
            lab_loginwindowpath.Content = "Current Login Window: " + Path.GetFileNameWithoutExtension(loginwindowpath);
            sl_logoendpos.Value = Properties.Settings.Default.cinema_slideshowendpos;
            sl_logoendscaleX.Value = Properties.Settings.Default.cinema_slideshowendscale;
            sl_volumecontrol.Value = Properties.Settings.Default.mediaplayer_volume;
            Cinema_MediaPlayer.Volume = sl_volumecontrol.Value;

            if (cinemamode)
            {
                //Cinema Mode

                /*
                WindowHeaderGrid.Visibility = Visibility.Hidden;
                reso_x = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;
                reso_y = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;
                myWindow.WindowState = WindowState.Maximized;
                */

                //Notes: Login frame = 560x300
                //Test resolutions here!
                //Only edit width!
                
                myWindow.Width = 1600;

                myWindow.Height = (int)(myWindow.Width / 16 * 9);
                int reso_x = (int)myWindow.Width;
                int reso_y = (int)myWindow.Height;

                //Centering Window, only needed in Debug
                double screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
                double screenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
                double windowWidth = this.Width;
                double windowHeight = this.Height;
                this.Left = (screenWidth / 2) - (windowWidth / 2);
                this.Top = (screenHeight / 2) - (windowHeight / 2);
                

                //Setting up the Login Window Location
                Canvas.SetTop(Canvas_login, reso_y - (reso_y / 2));
                Canvas.SetLeft(Canvas_login, reso_x / 10);
                //Setting up End Position of Logo Animation
                var endpos = (System.Windows.Media.Animation.EasingDoubleKeyFrame)Resources["Mask_EndPos"];
                endpos.Value = Properties.Settings.Default.cinema_slideshowendpos * reso_x / 200;
                var endscale = (System.Windows.Media.Animation.EasingDoubleKeyFrame)Resources["Mask_EndScaleX"];
                endscale.Value = (double)Properties.Settings.Default.cinema_slideshowendscale;

                //General UI Hiding/Scaling
                SettingsGrid.Visibility = Visibility.Hidden;
                bt_ShowSettings.Visibility = Visibility.Visible;
                Grid.SetColumnSpan(WindowOptionsColum, 2);
                Cinema_MediaPlayer.Visibility = Visibility.Hidden;
                Canvas_Custom_UI.Visibility = Visibility.Visible;
                VolumeControl.Visibility = Visibility.Visible;

                //Setting Custom mode independent Cinema Elements
                try
                {
                    if (backgroundcolor != null) myWindow.Background = new SolidColorBrush(backgroundcolor);
                    if (loginwindowpath != "") img_loginwindow.Source = LoadImage(loginwindowpath);
                }
                catch (Exception err)
                {
                    MessageBox.Show("Login Window Image could not be found!\nPath: " + loginwindowpath + "\n" + err.Message);
                }

                if (videomode)
                {
                    try
                    {
                        img_slideshow.Visibility = Visibility.Hidden;
                        //Load background video
                        Cinema_MediaPlayer.Visibility = Visibility.Visible;
                        Cinema_MediaPlayer.Source = new Uri(Properties.Settings.Default.cinema_videopath, UriKind.Relative);
                        Cinema_MediaPlayer.Play();
                    }
                    catch (Exception err)
                    {
                        MessageBox.Show("The chosen video for cinema mode is not valid/ does not exist!\n" + err.Message);
                        Properties.Settings.Default.cinema_use = false;
                        Properties.Settings.Default.Save();
                    }
                }

                if (slideshowmode)
                {
                    try
                    {
                        img_slideshow.Visibility = Visibility.Visible;
                        Storyboard anim_slideshow = (Storyboard)FindResource("anim_slideshow_start");
                        anim_slideshow.Begin();

                        if (maskpath != null)
                        {
                            //Setting opacity mask (logo)
                            ImageBrush mask = new ImageBrush(LoadImage(maskpath));
                            mask.Stretch = Stretch.Uniform;
                            img_slideshow.OpacityMask = mask;
                        }

                        //Starting background slide show thread
                        if (!slideshowthread_isrunning)
                        {
                            Thread th_slideshow = new Thread(() => slideshow_diashow(imagespath));
                            th_slideshow.Start();
                        }
                        slideshowthread_isrunning = true;

                        img_slideshow.Visibility = Visibility.Visible;
                        Cinema_MediaPlayer.Source = new Uri(musicpath);
                        Cinema_MediaPlayer.Play();
                    }
                    catch (Exception err)
                    {
                        MessageBox.Show("One or more settings for slide show mode are missing!\n" + err.Message);
                        Properties.Settings.Default.cinema_use = false;
                        Properties.Settings.Default.Save();
                    }
                }
            }
            else
            {
                //Normal Mode
                WindowHeaderGrid.Visibility = Visibility.Visible;
                VolumeControl.Visibility = Visibility.Collapsed;
                Cinema_MediaPlayer.Stop();
                Cinema_MediaPlayer.Visibility = Visibility.Hidden;
                SettingsGrid.Visibility = Visibility.Visible;
                myWindow.WindowState = WindowState.Normal;
                myWindow.Height = 680;
                myWindow.Width = 700;
                bt_ShowSettings.Visibility = Visibility.Collapsed;
                Grid.SetColumnSpan(WindowOptionsColum, 1);
                Canvas_Custom_UI.Visibility = Visibility.Collapsed;
            }
        }

        private void checkversion()
        {
            if(EnviromentManager.GwClientUpToDate==false)
            {
                lab_version.Content = "Build: " + EnviromentManager.GwClientVersion + " (outdated)";
                lab_version.Foreground = new SolidColorBrush(Colors.Red);
            }else
            {
                lab_version.Content = "Build: " + EnviromentManager.GwClientVersion;
                lab_version.Foreground = new SolidColorBrush(Colors.Green);
            }
        }

        private void setupend()
        {
            try
            {
                myWindow.Visibility = Visibility.Visible;
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }
        }

        private void UpdateServerUI()
        {
            bt_checkservers.IsEnabled = true;

            listview_auth.ItemsSource = ServerManager.authservers;
            listview_assets.ItemsSource = ServerManager.assetservers;
            lab_authserverlist.Content = "Authentication Servers (" + ServerManager.authservers.Count + " servers found):";
            lab_assetserverlist.Content = "Asset Servers (" + ServerManager.assetservers.Count + " servers found):";
            bt_checkservers.Content = "Check Servers (Last update: " + $"{DateTime.Now:HH:mm:ss tt}" + ")";

            // Sorting  servers (ping).
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(listview_auth.ItemsSource);
            view.SortDescriptions.Add(new SortDescription("Ping", ListSortDirection.Descending));
            CollectionView sview = (CollectionView)CollectionViewSource.GetDefaultView(listview_assets.ItemsSource);
            sview.SortDescriptions.Add(new SortDescription("Ping", ListSortDirection.Descending));
            sview.Refresh();
        }

        private void LoadConfig()
        {
            //Read the GFX Settings
            lv_gfx.ItemsSource = EnviromentManager.GwClientXmlPath;
            lv_gfx.Items.Refresh();
            lab_path.Content = "Install Path: " + EnviromentManager.GwClientPath;
            RefreshUI();
        }

        private void bt_checkservers_Click(object sender, RoutedEventArgs e)
        {
            //Starting server check thread
            bt_checkservers.Content = "Loading Server List";
            bt_checkservers.IsEnabled = false;

            Thread serverthread = new Thread(UpdateServerList);
            serverthread.IsBackground = true;
            serverthread.Start();

        }

        private void UpdateServerList()
        {
            ServerManager.UpdateServerlists();
            try
            {
                Application.Current.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Background,
                new Action(() => UpdateServerUI()));
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }
        }

        private void listview_assets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // UI Handling for selected Asset Server
            if (listview_assets.SelectedItem!=null)
            {
                ServerManager.SelectedAuthserver = (Server)listview_assets.SelectedItem;
                tb_assetsport.Text = ServerManager.SelectedAssetserver.Port;
                checkb_assets.Content = "Use Assets Server : " + ServerManager.SelectedAssetserver.IP;
                checkb_assets.IsEnabled = true;
            }
            else
            {
                checkb_assets.IsChecked = false;
            }
        }

        private void listview_auth_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // UI Handling for selected Authentication Server
            if (listview_auth.SelectedItem!=null)
            {
                ServerManager.SelectedAssetserver = (Server)listview_auth.SelectedItem;

                tb_authport.Text = ServerManager.SelectedAssetserver.Port;

                checkb_auth.Content = "Use Authentication Server : " + ServerManager.SelectedAssetserver.IP;
                checkb_auth.IsEnabled = true;
            }
            else
            {
                checkb_auth.IsChecked = false;
            }
        }

        private void checkb_auth_Checked(object sender, RoutedEventArgs e)
        {
            /*
            lab_port_auth.IsEnabled = true;
            tb_authport.IsEnabled = true;
            tb_authport.Text = ServerManager.SelectedAssetserver.Port;
            AccountArgumentManager.StopGap.IsSelected("-authsrv");
            AccountArgumentManager.StopGap.SetOptionString("-authsrv", ServerManager.SelectedAssetserver.IP + ":" + tb_authport.Text);
            RefreshUI();
            */
        }

        private void checkb_auth_Unchecked(object sender, RoutedEventArgs e)
        {
            /*
            lab_port_auth.IsEnabled = false;
            tb_authport.IsEnabled = false;
            AccountArgumentManager.StopGap.IsSelected("-authsrv", false);
            AccountArgumentManager.StopGap.SetOptionString("-authsrv", null);
            RefreshUI();
            */
        }

        private void checkb_assets_Checked(object sender, RoutedEventArgs e)
        {
            /*
            lab_port_assets.IsEnabled = true;
            tb_assetsport.IsEnabled = true;
            tb_assetsport.Text = ServerManager.SelectedAssetserver.Port;
            AccountArgumentManager.StopGap.IsSelected("-assetsrv");
            AccountArgumentManager.StopGap.SetOptionString("-assetsrv", ServerManager.SelectedAssetserver.IP + ":" + tb_assetsport.Text);
            RefreshUI();
            */
        }

        private void checkb_assets_Unchecked(object sender, RoutedEventArgs e)
        {
            /*
            lab_port_assets.IsEnabled = false;
            tb_assetsport.IsEnabled = false;
            AccountArgumentManager.StopGap.IsSelected("-assetsrv", false);
            AccountArgumentManager.StopGap.SetOptionString("-assetsrv", null);
            RefreshUI();
            */
        }

        private void checkb_clientport_Checked(object sender, RoutedEventArgs e)
        {
            /*
            tb_clientport.IsEnabled = true;
            lab_port_client.IsEnabled = true;
            AccountArgumentManager.StopGap.IsSelected("-clientport");
            AccountArgumentManager.StopGap.SetOptionString("-clientport", tb_clientport.Text);
            RefreshUI();
            */
        }

        private void checkb_clientport_Unchecked(object sender, RoutedEventArgs e)
        {
            /*
            tb_clientport.IsEnabled = false;
            lab_port_client.IsEnabled = false;
            AccountArgumentManager.StopGap.IsSelected("-clientport", false);
            AccountArgumentManager.StopGap.SetOptionString("-clientport", null);
            RefreshUI();
            */
        }

        private void bt_launch_Click(object sender, RoutedEventArgs e)
        {
            myWindow.WindowState = WindowState.Minimized;
            AccountManager.LaunchAccounts();
        }

        private void bt_installpath_Click(object sender, RoutedEventArgs e)
        {
            //Alternative Path selection (when XML import fails)
            Builders.FileDialog.DefaultExt(".exe")
                .Filter("EXE Files(*.exe)|*.exe")
                .EnforceExt(".exe")
                .ShowDialog((Helpers.FileDialog fileDialog) =>
                {
                    if (fileDialog.FileName != "")
                    {
                        EnviromentManager.GwClientPath = Path.GetDirectoryName(fileDialog.FileName) + @"\";
                        EnviromentManager.GwClientExeName = Path.GetFileName(fileDialog.Fi‌​leName);
                        lab_path.Content = EnviromentManager.GwClientPath + EnviromentManager.GwClientExePath;
                    }
                });
        }

        public void CreateShortcut(string shortcutName, string shortcutPath, string targetFileLocation)
        {
            /*

            // Needs rewrite
            // Modified Shortcut script by "CooLMinE" at http://www.fluxbytes.com/
            try
            {
                string shortcutLocation = System.IO.Path.Combine(shortcutPath, shortcutName + ".lnk");
                WshShell shell = new WshShell();
                IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutLocation);
                string arguments = AccountManager.Account(null).CommandLine();
                shortcut.IconLocation = Assembly.GetExecutingAssembly().Location;
                shortcut.Description = "Created with Gw2 Launchbuddy, © TheCheatsrichter";

                if (cb_reshade.IsChecked == true)
                {
                    // Using command line to launch both EXE files from the link file
                    // EXAMPLE: cmd.exe /c start "" "C:\Program Files (x86)\Guild Wars 2\ReshadeUnlocker" && start "" "C:\Program Files (x86)\Guild Wars 2\Gw2"
                    shortcut.Arguments = " /c start \"\" \"" + Globals.unlockerpath + "\" && start \"\" \"" + EnviromentManager.GwClientPath + EnviromentManager.GwClientExePath + "\" " + arguments;
                    MessageBox.Show(shortcut.Arguments);
                    shortcut.TargetPath = "cmd.exe"; // win will automatically extend this to the CMD path
                    shortcut.Save();
                }
                else
                {
                    shortcut.Arguments = arguments;
                    shortcut.TargetPath = targetFileLocation;
                    shortcut.Save();
                }

                string dynamicinfo = "";
                foreach (string arg in arguments.Split(' '))
                {
                    dynamicinfo += arg + "\n\t\t";
                }

                System.Windows.MessageBox.Show("Custom Launcher created at : " + EnviromentManager.GwClientPath + "\nUse ReshadeUnlocker: " + cb_reshade.IsChecked.ToString() + "\nUsed arguments:" + dynamicinfo);
            }
            catch (Exception err)
            {
                MessageBox.Show("Error when creating shortcut. Invalid Path?\n\n" + err.Message);
            }
            */
        }


        private void bt_shortcut_Click(object sender, RoutedEventArgs e)
        {
            /*
            if (cb_login.IsChecked == true)
            {
                try
                {
                    CreateShortcut("Gw2_Launcher_" + AccountManager.DefaultAccount.Nickname, EnviromentManager.GwClientPath, EnviromentManager.GwClientPath + EnviromentManager.GwClientExePath);
                }
                catch (Exception err)
                {
                    MessageBox.Show(err.Message);
                }
            }
            else
            {
                CreateShortcut("Gw2_Custom_Launcher", EnviromentManager.GwClientPath, EnviromentManager.GwClientPath + EnviromentManager.GwClientExePath);
            }
            try
            {
                Process.Start(EnviromentManager.GwClientPath);
            }
            catch (Exception err)
            {
                MessageBox.Show("Could not open file directory\n" + err.Message);
            }
            */
        }

        private void SaveAddons()
        {
            AddOnManager.SaveAddons(Globals.AppDataPath + "Addons.xml");
        }

        private void LoadAddons()
        {
            //lv_AddOns.ItemsSource = AddOnManager.LoadAddons(Globals.AppDataPath + "Addons.xml");
        }

        private void bt_addacc_Click(object sender, RoutedEventArgs e)
        {
            bt_accsave.IsEnabled = true;

            if (lv_accssettings.SelectedItem as Account != null)
            {
                Account acc = new Account("New Account", lv_accssettings.SelectedItem as Account);
            }
            else
            {
                Account acc = new Account("New Account", lv_accssettings.SelectedItem as Account);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.instance_win_X = Globals.Appmanager.Left;
            Properties.Settings.Default.instance_win_Y = Globals.Appmanager.Top;
            Properties.Settings.Default.Save();
            AccountManager.SaveAccounts();
            SaveAddons();
            Environment.Exit(Environment.ExitCode);
        }

        private void bt_remacc_Click(object sender, RoutedEventArgs e)
        {
            bt_accsave.IsEnabled = true;
            if (lv_accssettings.SelectedItem != null)
            {
                AccountManager.Remove(lv_accssettings.SelectedItem as Account);
                lv_accssettings.SelectedIndex = -1;
            }
        }

        private void tb_authport_LostKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            ServerManager.SelectedAssetserver.Port = tb_authport.Text;
        }

        private void tb_assetsport_LostKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            ServerManager.SelectedAssetserver.Port = tb_assetsport.Text;
        }

        private void exp_server_Collapsed(object sender, RoutedEventArgs e)
        {
            //ServerUI.Height = new GridLength(30);
            Application.Current.MainWindow.Height = 585;
        }

        private void exp_server_Expanded(object sender, RoutedEventArgs e)
        {
            //ServerUI.Height = new GridLength(290);
            Application.Current.MainWindow.Height = 845;
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(listview_auth.ItemsSource);
            view.SortDescriptions.Add(new SortDescription("Ping", ListSortDirection.Ascending));
            CollectionView sview = (CollectionView)CollectionViewSource.GetDefaultView(listview_assets.ItemsSource);
            sview.SortDescriptions.Add(new SortDescription("Ping", ListSortDirection.Ascending));
        }

        private void SortByColumn(ListView list, object sender)
        {
            GridViewColumnHeader column = (sender as GridViewColumnHeader);
            string sortBy = column.Tag.ToString();
            if (listViewSortCol != null)
            {
                AdornerLayer.GetAdornerLayer(listViewSortCol).Remove(listViewSortAdorner);
                list.Items.SortDescriptions.Clear();
            }

            ListSortDirection newDir = ListSortDirection.Ascending;
            if (listViewSortCol == column && listViewSortAdorner.Direction == newDir)
                newDir = ListSortDirection.Descending;

            listViewSortCol = column;
            listViewSortAdorner = new SortAdorner(listViewSortCol, newDir);
            AdornerLayer.GetAdornerLayer(listViewSortCol).Add(listViewSortAdorner);
            list.Items.SortDescriptions.Add(new SortDescription(sortBy, newDir));
        }

        private void bt_donate_Click(object sender, RoutedEventArgs e)
        {
            string url = "";

            string business = "thecheatsrichter@gmx.at";  // your PayPal email
            string description = "Gw2 Launchbuddy Donation";            // '%20' represents a space. remember HTML!
            string currency = "EUR";                 // AUD, USD, etc.

            url += "https://www.paypal.com/cgi-bin/webscr" +
                "?cmd=" + "_donations" +
                "&business=" + business +
                //"&lc=" + country +
                "&item_name=" + description +
                "&currency_code=" + currency +
                "&bn=" + "PP%2dDonationsBF";

            System.Diagnostics.Process.Start(url);
        }

        private void bt_patreon_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(@"www.patreon.com/gw2launchbuddy");
        }

        private void bt_close_Click(object sender, RoutedEventArgs e)
        {
            foreach (var plugin in PluginManager.PluginCollection) plugin.Exit();
            Mainwin_SaveSetup();
            DllInjector.SaveDlls();
            Properties.Settings.Default.Save();
            Application.Current.Shutdown();
        }

        private void tab_options_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl)
            {
                RefreshUI();
            }
        }

        private void tab_options_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            RefreshUI();
        }

        private void bt_minimize_Click(object sender, RoutedEventArgs e)
        {
            myWindow.WindowState = WindowState.Normal;
            myWindow.WindowState = WindowState.Minimized;
            myWindow.Opacity = 0;
        }

        private void bt_AddAddon_Click(object sender, RoutedEventArgs e)
        {
            /*
            string[] args = Regex.Matches(tb_AddonArgs.Text, "-\\w* ?(\".*\")?").Cast<Match>().Select(m => m.Value).ToArray();
            AddOnManager.Add(tb_AddonName.Text, args, (bool)cb_AddonMultilaunch.IsChecked, (bool)cb_AddonOnLB.IsChecked);
            lv_AddOns.ItemsSource = AddOnManager.addOnCollection;
            */
        }

        private void bt_RemAddon_Click(object sender, RoutedEventArgs e)
        {
            /*
            AddOn item = lv_AddOns.SelectedItem as AddOn;
            if (item != null)
            {
                AddOnManager.Remove(item.Name);
            }
            */
        }

        private void bt_cinema_setimagefolder_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folderdialog = new System.Windows.Forms.FolderBrowserDialog();

            if (System.Windows.Forms.DialogResult.OK == folderdialog.ShowDialog())
            {
                lv_cinema_images.SelectedIndex = -1;
                lab_imagepreview.Content = "Current Image:";
                var files = Directory.GetFiles(folderdialog.SelectedPath, "*.*", SearchOption.AllDirectories).Where(a => a.EndsWith(".png") || a.EndsWith(".jpg") || a.EndsWith(".jpeg") || a.EndsWith(".bmp"));
                ObservableCollection<CinemaImage> images = new ObservableCollection<CinemaImage>();
                foreach (var file in files)
                {
                    images.Add(new CinemaImage(file));
                    lv_cinema_images.ItemsSource = images;
                    Properties.Settings.Default.cinema_imagepath = folderdialog.SelectedPath;
                    Properties.Settings.Default.Save();
                }
            }
        }

        private void lv_cinema_images_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedimage = lv_cinema_images.SelectedItem as CinemaImage;
            if (selectedimage != null)
            {
                img_imagepreview.Source = LoadImage(selectedimage.Path);
                lab_imagepreview.Content = "Current Image: " + selectedimage.Name;
            }
        }

        private void bt_cinema_setmask_Click(object sender, RoutedEventArgs e)
        {

            Builders.FileDialog.DefaultExt(".png")
                .Filter("PNG Files(*.png)|*.png")
                .ShowDialog((Helpers.FileDialog fileDialog) =>
                {
                    if (fileDialog.FileName != "")
                    {
                        Properties.Settings.Default.cinema_maskpath = fileDialog.FileName;
                        Properties.Settings.Default.Save();
                        lab_maskpreview.Content = "Current Mask: " + Path.GetFileName(fileDialog.FileName);
                        img_maskpreview.Source = LoadImage(fileDialog.FileName);
                        ImageBrush newmask = new ImageBrush(LoadImage(fileDialog.FileName));
                        newmask.Stretch = Stretch.Uniform;
                        img_slideshow.OpacityMask = newmask;
                    }
                });
        }

        private void listview_auth_Click(object sender, RoutedEventArgs e)
        {
            SortByColumn(listview_auth, sender);
        }

        private void bt_setmusic_Click(object sender, RoutedEventArgs e)
        {
            Builders.FileDialog.Filter("MP3 Files(*.mp3)|*.mp3|WAV Files (*.wav)|*.wav|AAC Files (*.aac)|*.aac|All Files(*.*)|*.*")
                .ShowDialog((Helpers.FileDialog fileDialog) =>
                {
                    if (fileDialog.FileName != "")
                    {
                        Properties.Settings.Default.cinema_musicpath = fileDialog.FileName;
                        Properties.Settings.Default.Save();
                        lab_musicpath.Content = "Current Music File: " + Path.GetFileName(fileDialog.FileName);
                        Cinema_MediaPlayer.Source = (new Uri(fileDialog.FileName));
                    }
                });
        }

        private bool IsValidPath(string path)
        {
            try
            {
                Path.GetFullPath(path);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void LoadCinemaSettings()
        {
            string imagepath = Properties.Settings.Default.cinema_imagepath;
            string maskpath = Properties.Settings.Default.cinema_maskpath;
            string musicpath = Properties.Settings.Default.cinema_musicpath;
            string loginpath = Properties.Settings.Default.cinema_loginwindowpath;

            try
            {
                if (IsValidPath(imagepath) && Directory.Exists(imagepath))
                {
                    var files = Directory.GetFiles(imagepath, "*.*", SearchOption.AllDirectories).Where(a => a.EndsWith(".png") || a.EndsWith(".jpg") || a.EndsWith(".jpeg") || a.EndsWith(".bmp"));
                    ObservableCollection<CinemaImage> images = new ObservableCollection<CinemaImage>();
                    foreach (var file in files)
                    {
                        images.Add(new CinemaImage(file));
                        lv_cinema_images.ItemsSource = images;
                    }
                }
            }
            catch { }

            try
            {
                if (IsValidPath(maskpath) && Path.GetExtension(maskpath) == ".png")
                {
                    img_maskpreview.Source = LoadImage(maskpath);
                    lab_maskpreview.Content = "Current Mask: " + Path.GetFileName(maskpath);
                }
            }
            catch
            {
                lab_maskpreview.Content = "Current Mask: ERROR! " + Path.GetFileName(maskpath) + " file not found!";
            }

            try
            {
                if (IsValidPath(loginpath) && Path.GetExtension(maskpath) == ".png")
                {
                    lab_loginwindowpath.Content = "Current Login Window: " + Path.GetFileName(loginpath);
                }
            }
            catch
            {
                lab_loginwindowpath.Content = "Current Mask: ERROR! " + Path.GetFileName(loginpath) + " file not found!";
            }

            try
            {
                if (IsValidPath(musicpath))
                {
                    Cinema_MediaPlayer.Source = (new Uri(musicpath));
                    lab_musicpath.Content = "Current Music File: " + Path.GetFileName(musicpath);
                }
            }
            catch
            {
                lab_maskpreview.Content = "Current Music File: ERROR! " + Path.GetFileName(musicpath) + " file not found!";
            }
        }

        private void bt_musicstart_Click(object sender, RoutedEventArgs e)
        {
            if (Properties.Settings.Default.cinema_musicpath != null && Properties.Settings.Default.cinema_musicpath != "")
            {
                Cinema_MediaPlayer.Source = new Uri(Properties.Settings.Default.cinema_musicpath);
                Cinema_MediaPlayer.Play();
            }
            else
            {
                MessageBox.Show("Invalid Music Path");
            }
        }

        private void bt_musicstop_Click(object sender, RoutedEventArgs e)
        {
            Cinema_MediaPlayer.Stop();
        }

        private void bt_cinema_Click(object sender, RoutedEventArgs e)
        {
            cinemamode = !Properties.Settings.Default.cinema_use;
            Properties.Settings.Default.cinema_use = cinemamode;
            Properties.Settings.Default.Save();
            Cinema_MediaPlayer.Stop();
            cinema_setup();
        }

        private void bt_ShowSettings_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsGrid.Visibility == Visibility.Hidden)
            {
                SettingsGrid.Visibility = Visibility.Visible;
            }
            else
            {
                SettingsGrid.Visibility = Visibility.Hidden;
            }
        }

        private void rb_slideshowmode(object sender, RoutedEventArgs e)
        {
            try
            {
                Videomode.Visibility = Visibility.Collapsed;
                Slideshow.Visibility = Visibility.Visible;
                Properties.Settings.Default.cinema_video = false;
                Properties.Settings.Default.cinema_slideshow = true;
                Properties.Settings.Default.Save();
            }
            catch { }
        }

        private void rb_videomode(object sender, RoutedEventArgs e)
        {
            try
            {
                Slideshow.Visibility = Visibility.Collapsed;
                Videomode.Visibility = Visibility.Visible;
                Properties.Settings.Default.cinema_video = true;
                Properties.Settings.Default.cinema_slideshow = false;
                Properties.Settings.Default.Save();
            }
            catch { }
        }

        private void bt_cinema_setvideo_Click(object sender, RoutedEventArgs e)
        {
            cinema_videoplayback.Stop();
            Builders.FileDialog.DefaultExt(".mp4")
                .Filter("Mp4 Files(*.mp4)|*.mp4|Raw Files(*.raw)|*.raw|WMV Files(*.wmv)|*.wmv|MPEG Files(*.mpeg)|*.mpeg|All Files(*.*)|*.*")
                .CheckForMedia(Helpers.FileDialog.MediaTypes.Video, cinema_videoplayback)
                .ShowDialog((Helpers.FileDialog fileDialog) =>
                {
                    if (fileDialog.Result == System.Windows.Forms.DialogResult.OK)
                    {
                        cinema_videoplayback.Source = new Uri(fileDialog.FileName, UriKind.Relative);
                        Properties.Settings.Default.cinema_videopath = fileDialog.FileName;
                        SetVideoInfo();
                        cinema_videoplayback.Play();
                    }
                });
        }

        private void SetVideoInfo()
        {
            try
            {
                string videopath = Properties.Settings.Default.cinema_videopath;

                lab_videoname.Content = "Name: " + Path.GetFileNameWithoutExtension(videopath);
                lab_videopath.Content = "Path: " + Path.GetFullPath(videopath);
                lab_videoformat.Content = "Format: " + Path.GetExtension(videopath);
                lab_videoresolution.Content = "Resolution: " + cinema_videoplayback.NaturalVideoWidth + " x " + cinema_videoplayback.NaturalVideoHeight;
                lab_videolength.Content = "Length: " + cinema_videoplayback.NaturalDuration.ToString();
            }
            catch { }
        }

        private void bt_cinema_videoplay_Click(object sender, RoutedEventArgs e)
        {
            if (cinema_videoplayback.Source != null)
            {
                cinema_videoplayback.Play();
            }
        }

        private void bt_cinema_videostop_Click(object sender, RoutedEventArgs e)
        {
            if (cinema_videoplayback.Source != null)
            {
                cinema_videoplayback.Stop();
            }
        }

        private void listview_assets_Click(object sender, RoutedEventArgs e)
        {
            SortByColumn(listview_assets, sender);
        }

        private static BitmapSource LoadImage(string path)
        {
            var bitmap = new BitmapImage();

            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
            }

            return bitmap;
        }

        private void cinema_videoplayback_Loaded(object sender, RoutedEventArgs e)
        {
            SetVideoInfo();
        }

        private void cinema_videoplayback_MediaOpened(object sender, RoutedEventArgs e)
        {
            SetVideoInfo();
        }

        private void Cinema_Launchaccount_Click(object sender, RoutedEventArgs e)
        {
            AccountManager.LaunchAccounts();
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            SettingsGrid.Visibility = SettingsGrid.Visibility == Visibility.Hidden ? Visibility.Visible : Visibility.Hidden;
        }

        private void CheckBox_Checked(Object sender, RoutedEventArgs e)
        {
            /*
            AccountArgumentManager.StopGap.IsSelected(((CheckBox)sender).Content.ToString(), true);
            RefreshUI();
            */
        }

        private void CheckBox_Unchecked(Object sender, RoutedEventArgs e)
        {
            /*
            AccountArgumentManager.StopGap.IsSelected(((CheckBox)sender).Content.ToString(), false);
            RefreshUI();
            */
        }

        private void RefreshUI()
        {
            /*
            lab_currentsetup.Content = "Current Setup: " + AccountArgumentManager.StopGap.Print();
            lab_usedaddons.Content = "Used AddOns: " + AddOnManager.ListAddons();
            */
        }

        private void UpdateServerArgs()
        {
            /*
            //Should really be bound to changing applicable UI elements
            if (checkb_assets.IsChecked == true)
                AccountArgumentManager.StopGap.SetOptionString("-assetsrv", ServerManager.SelectedAssetserver.IP + ":" + tb_assetsport.Text);
            if (checkb_auth.IsChecked == true)
                AccountArgumentManager.StopGap.SetOptionString("-authsrv ", ServerManager.SelectedAssetserver.IP + ":" + tb_authport.Text);
            if (checkb_clientport.IsChecked == true)
                AccountArgumentManager.StopGap.SetOptionString("-clientport", tb_clientport.Text);
            */
        }

        private void Window_LostKeyboardFocus(Object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            if (cinemamode || rb_cinemavideomode.IsChecked == true) { Cinema_MediaPlayer.Pause(); }
            BeginStoryboard(this.FindResource("anim_musicfadeout") as System.Windows.Media.Animation.Storyboard);
        }

        private void Window_GotKeyboardFocus(Object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            if (cinemamode || rb_cinemavideomode.IsChecked == true) { Cinema_MediaPlayer.Play(); }
            BeginStoryboard(this.FindResource("anim_musicfadein") as System.Windows.Media.Animation.Storyboard);
        }

        private void myWindow_MouseLeftButtonDown_1(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void bt_mute_Click(object sender, RoutedEventArgs e)
        {
            if (Cinema_MediaPlayer.IsMuted)
            {
                Cinema_MediaPlayer.IsMuted = false;
                img_mutebutton = new ImageBrush(new BitmapImage(new Uri("/Resources/Icons/speaker_loud.png", UriKind.Relative)));
            }
            else
            {
                Cinema_MediaPlayer.IsMuted = true;
                img_mutebutton = new ImageBrush(new BitmapImage(new Uri("/Resources/Icons/speaker_mute.png", UriKind.Relative)));

            }
        }

        private void bt_mute_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            sl_volumecontrol.Visibility = Visibility.Visible;
        }

        private void sl_volumecontrol_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Cinema_MediaPlayer.Volume = sl_volumecontrol.Value;
        }

        private void WrapPanel_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            sl_volumecontrol.Visibility = Visibility.Collapsed;
            Properties.Settings.Default.mediaplayer_volume = sl_volumecontrol.Value;
            Properties.Settings.Default.Save();
        }

        private void Cinema_MediaPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            (sender as MediaElement).Stop();
            (sender as MediaElement).Play();
        }

        private void ClrPcker_Background_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            myWindow.Background = new SolidColorBrush((Color)ClrPcker_Background.SelectedColor);
            if ((Color)ClrPcker_Background.SelectedColor != null)
            {
                Properties.Settings.Default.cinema_backgroundcolor = (Color)ClrPcker_Background.SelectedColor;
                Properties.Settings.Default.Save();
            }
        }

        private void bt_setloginwindow_Click(object sender, RoutedEventArgs e)
        {
            Builders.FileDialog.DefaultExt(".png")
                .Filter("PNG Files(*.png) | *.png")
                .ShowDialog((Helpers.FileDialog fileDialog) =>
                {
                    if (fileDialog.Result == System.Windows.Forms.DialogResult.OK)
                    {
                        cinema_videoplayback.Source = new Uri(fileDialog.FileName, UriKind.Relative);
                        Properties.Settings.Default.cinema_loginwindowpath = fileDialog.FileName;
                        img_loginwindow.Source = LoadImage(fileDialog.FileName);
                        lab_loginwindowpath.Content = "Current Login Window: " + Path.GetFileNameWithoutExtension(fileDialog.FileName);
                        Properties.Settings.Default.Save();
                    }
                });
        }

        private void sl_logoendpos_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var anim_slideshow = (System.Windows.Media.Animation.Storyboard)Resources["anim_slideshow_start"];
            anim_slideshow.Begin();
        }

        private void sl_logoendpos_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            var anim_slideshow = (System.Windows.Media.Animation.Storyboard)Resources["anim_slideshow_start"];
            anim_slideshow.Begin();
            Properties.Settings.Default.cinema_slideshowendpos = (int)sl_logoendpos.Value;
            Properties.Settings.Default.Save();
        }

        private void sl_logoendscaleX_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var endscale = (System.Windows.Media.Animation.EasingDoubleKeyFrame)Resources["Mask_EndScaleX"];
            endscale.Value = sl_logoendscaleX.Value;
            lab_endscaleX.Content = "Image EndScale: " + Math.Round(sl_logoendscaleX.Value, 2) + " X Zoom";
        }

        private void sl_logoendscaleX_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            var anim_slideshow = (System.Windows.Media.Animation.Storyboard)Resources["anim_slideshow_start"];
            anim_slideshow.Begin();
            Properties.Settings.Default.cinema_slideshowendscale = sl_logoendscaleX.Value;
            Properties.Settings.Default.Save();
        }

        private void bt_loadgfx_Click(object sender, RoutedEventArgs e)
        {
            Builders.FileDialog.DefaultExt(".xml")
                .Filter("XML Files(*.xml)|*.xml")
                .InitialDirectory(EnviromentManager.GwClientPath)
                .ShowDialog((Helpers.FileDialog fileDialog) =>
                {
                    if (GFXManager.IsValidGFX(fileDialog.FileName))
                    {
                        var tmp = GFXManager.LoadFile(fileDialog.FileName);
                        if (tmp != null)
                        {
                            GFXManager.SelectedGFX = tmp;
                            lv_gfx.ItemsSource = GFXManager.SelectedGFX.Config;
                            lv_gfx.Items.Refresh();
                        }
                        else
                        {
                            MessageBox.Show("Invalid GFX Config File selected!");
                        }
                    }
                });
        }

        private void bt_resetgfx_Click(object sender, RoutedEventArgs e)
        {
            GFXManager.SelectedGFX = GFXManager.ReadFile(EnviromentManager.GwClientXmlPath);
            lv_gfx.ItemsSource = GFXManager.SelectedGFX.Config;
            lv_gfx.Items.Refresh();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
            ComboBox box = sender as ComboBox;
            GFXOption option = box.DataContext as GFXOption;

            if (option !=null)
            {
                lv_gfx.SelectedItem = option;
                if(option.Value!=option.OldValue)
                {
                    (lv_gfx.SelectedItem as ListViewItem).Background = Brushes.Gray;
                }
            }
            
        }

        private void lv_gfx_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //if (lv_gfx.SelectedItem != null) MessageBox.Show((lv_gfx.SelectedItem as GFXOption).ToXml());
        }

        private void bt_savegfx_Click(object sender, RoutedEventArgs e)
        {
            GFXManager.SaveFile();
        }

        private void bt_applygfx_Click(object sender, RoutedEventArgs e)
        {
            GFXManager.OverwriteGFX();

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = EnviromentManager.GwClientPath + EnviromentManager.GwClientExePath;
            startInfo.Arguments = " -image -shareArchive";
            Process gw2pro = new Process { StartInfo = startInfo };

            gw2pro.Start();
            gw2pro.WaitForExit();

            GFXManager.SelectedGFX = GFXManager.ReadFile(EnviromentManager.GwClientXmlPath);
            lv_gfx.ItemsSource = GFXManager.SelectedGFX.Config;
            lv_gfx.Items.Refresh();
        }

        private void bt_accsortup_Click(object sender, RoutedEventArgs e)
        {
            bt_accsave.IsEnabled = true;
            if (lv_accssettings.SelectedItem != null)
            {
                AccountManager.MoveAccount(lv_accssettings.SelectedItem as Account, +1);
            }
        }

        private void bt_accsortdown_Click(object sender, RoutedEventArgs e)
        {
            bt_accsave.IsEnabled = true;
            if (lv_accssettings.SelectedItem != null)
            {
                AccountManager.MoveAccount(lv_accssettings.SelectedItem as Account, -1);
            }
        }

        private void bt_selecticon_Click(object sender, RoutedEventArgs e)
        {
            /*
            Account acc = (sender as Button).DataContext as Account;
            acc = AccountManager.AccountCollection.Single(x => x.Email == acc.Email);

            Builders.FileDialog.DefaultExt(".png")
                .Filter("PNG Files(*.png) | *.png")
                .ShowDialog((Helpers.FileDialog fileDialog) =>
                {
                    if (fileDialog.Result == System.Windows.Forms.DialogResult.OK)
                    {
                        acc.SetIcon(fileDialog.FileName);
                    }
                    lv_accssettings.Items.Refresh();
                });
            */
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bt_downloadrelease.IsEnabled = true;
            bt_downloadrelease.Content = "Download and use Release V" + (lv_lbversions.SelectedItem as Release).Version;
            wb_releasedescr.NavigateToString((lv_lbversions.SelectedItem as Release).Description);
        }

        private void bt_downloadrelease_Click(object sender, RoutedEventArgs e)
        {
            if (lv_lbversions.SelectedItem != null)
            {
                Version rl_version = (lv_lbversions.SelectedItem as Release).Version;
                if (rl_version.CompareTo(Globals.LBVersion) < 0)
                {
                    MessageBoxResult win = MessageBox.Show("Usage of older versions of Launchbuddy can corrupt your Launchbuddy data!\n\nAre you sure you want to download V" + rl_version, "Release Download", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (win.ToString() == "No")
                    {
                        return;
                    }
                }
                (sender as Button).Content = "Downloading LB V" + rl_version;
                VersionSwitcher.ApplyRelease(lv_lbversions.SelectedItem as Release);
                (sender as Button).Content = "Download";
            }
        }

        private void bt_selectaccgfx_Click(object sender, RoutedEventArgs e)
        {
            Account acc = (sender as Button).DataContext as Account;

            Builders.FileDialog.DefaultExt(".xml")
                .Filter("GFX Files(*.xml)|*.xml")
                .ShowDialog((Helpers.FileDialog fileDialog) =>
                {

                    acc.Settings.GFXFile = fileDialog.FileName;
                    (sender as Button).Content = acc.Settings.GFXFile;
                });
        }

        private void bt_bugreport_Click(object sender, RoutedEventArgs e)
        {
            CrashReporter.ReportCrashToAll(new Exception("BugReport"));
        }

        private void bt_fetchlbversions_Click(object sender, RoutedEventArgs e)
        {
            Thread checkforlbupdate = new Thread(checklbversion);
            checkforlbupdate.Start();
        }

        private void cb_lbupdatescheck_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.notifylbupdate = (bool)cb_lbupdatescheck.IsChecked;
            Properties.Settings.Default.Save();
        }

        private void cb_useinstancegui_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.useinstancegui = (bool)cb_useinstancegui.IsChecked;
            Properties.Settings.Default.Save();
        }

        private void bt_manualauthserver_Click(object sender, RoutedEventArgs e)
        {
            ServerManager.AddAuthServer(tb_manualauthserver.Text);
        }

        private void bt_manualassetserver_Click(object sender, RoutedEventArgs e)
        {
            ServerManager.AddAssetServer(tb_manualassetserver.Text);
        }

        private void cb_useloadingui_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.useloadingui = (bool)cb_useloadingui.IsChecked;
            Properties.Settings.Default.Save();
        }

        private void bt_accsave_Click(object sender, RoutedEventArgs e)
        {
            AccountManager.SaveAccounts();
            bt_accsave.IsEnabled = false;
        }

        private void lv_crashlogs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Crashlog crashlog = lv_crashlogs.SelectedItem as Crashlog;
            tblock_crashinfo.Text =  crashlog.Quickinfo;
            tblock_crashsolutioninfo.Text = crashlog.Solutioninfo;
            bt_fixcrash.IsEnabled = crashlog.IsSolveable;
        }

        private void bt_fixcrash_Click(object sender, RoutedEventArgs e)
        {
            Crashlog crashlog = lv_crashlogs.SelectedItem as Crashlog;
            crashlog.Solve();
            bt_fixcrash.IsEnabled = false;
        }

        private void bt_AddDll_Click(object sender, RoutedEventArgs e)
        {
            DllInjector.AddDLL();
        }

        private void bt_RemDll_Click(object sender, RoutedEventArgs e)
        {
            DllInjector.RemDLL(lv_InjectDlls.SelectedItem as string);
        }

        private void bt_accadd_Click(object sender, RoutedEventArgs e)
        {
            AccountManager.CreateEmptyAccount();
        }

        private void lv_accs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var accs = (sender as ListView).SelectedItems;
            AccountManager.SwitchSelectionAll(false);
            foreach(Account acc in accs)
            {
                acc.IsEnabled = true;
            }
        }

        private void lv_accssettings_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var acc=(sender as ListView).SelectedItem as Account;
            if (acc!=null )
            {
                AccountManager.EditAccount = acc;
                gr_acceditor.DataContext = AccountManager.EditAccount;
                gr_acceditor.IsEnabled = true;
                sp_acclistbuttons.IsEnabled = true;
            }
            else
            {
                gr_acceditor.IsEnabled = false;
                sp_acclistbuttons.IsEnabled = false;
            }


        }

        private void bt_accrem_Click(object sender, RoutedEventArgs e)
        {
            AccountManager.Remove(lv_accssettings.SelectedItem as Account);
        }

        private void cb_argument_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            textblock_descr.Text = ((sender as CheckBox).DataContext as Argument).Description;
        }

        private void sl_logoendpos_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var endpos = (System.Windows.Media.Animation.EasingDoubleKeyFrame)Resources["Mask_EndPos"];
            endpos.Value = sl_logoendpos.Value * (reso_x / 200);
        }
    }

    public class SortAdorner : Adorner
    {
        private static Geometry ascGeometry =
                Geometry.Parse("M 0 4 L 3.5 0 L 7 4 Z");

        private static Geometry descGeometry =
                Geometry.Parse("M 0 0 L 3.5 4 L 7 0 Z");

        public ListSortDirection Direction { get; private set; }

        public SortAdorner(UIElement element, ListSortDirection dir) : base(element)
        {
            this.Direction = dir;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (AdornedElement.RenderSize.Width < 20)
                return;

            TranslateTransform transform = new TranslateTransform
                    (
                            AdornedElement.RenderSize.Width - 15,
                            (AdornedElement.RenderSize.Height - 5) / 2
                    );
            drawingContext.PushTransform(transform);

            Geometry geometry = ascGeometry;
            if (this.Direction == ListSortDirection.Descending)
                geometry = descGeometry;
            drawingContext.DrawGeometry(Brushes.Black, null, geometry);

            drawingContext.Pop();
        }
    }

    public class ListBoxHelper : DependencyObject
    {
        public static int GetAutoSizeItemCount(DependencyObject obj)
        {
            return (int)obj.GetValue(AutoSizeItemCountProperty);
        }

        public static void SetAutoSizeItemCount(DependencyObject obj, int value)
        {
            obj.SetValue(AutoSizeItemCountProperty, value);
        }

        // Using a DependencyProperty as the backing store for AutoSizeItemCount.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AutoSizeItemCountProperty =
            DependencyProperty.RegisterAttached("AutoSizeItemCount", typeof(int), typeof(ListBoxHelper), new PropertyMetadata(0, OnAutoSizeItemCountChanged));

        private static void OnAutoSizeItemCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ListBox listBox = d as ListBox;
            listBox.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler((lb, arg) => UpdateSize(listBox)));
            listBox.ItemContainerGenerator.ItemsChanged += (ig, arg) => UpdateSize(listBox);
        }

        private static void UpdateSize(ListBox listBox)
        {
            ItemContainerGenerator gen = listBox.ItemContainerGenerator;
            FrameworkElement element = listBox.InputHitTest(new Point(listBox.Padding.Left + 5, listBox.Padding.Top + 5)) as FrameworkElement;
            if (element != null && gen != null)
            {
                object item = element.DataContext;
                if (item != null)
                {
                    FrameworkElement container = gen.ContainerFromItem(item) as FrameworkElement;
                    if (container == null)
                    {
                        container = element;
                    }
                    int maxCount = GetAutoSizeItemCount(listBox);
                    double newHeight = Math.Min(maxCount, gen.Items.Count) * container.ActualHeight;
                    newHeight += listBox.Padding.Top + listBox.Padding.Bottom + listBox.BorderThickness.Top + listBox.BorderThickness.Bottom + 2;
                    if (listBox.ActualHeight != newHeight)
                        listBox.Height = newHeight;
                }
            }
        }
 
    }
    

}