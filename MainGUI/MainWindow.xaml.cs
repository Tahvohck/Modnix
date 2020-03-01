﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using static System.Globalization.CultureInfo;

namespace Sheepy.Modnix.MainGUI {

   public partial class MainWindow : Window, IAppGui {
      private readonly AppControl App = AppControl.Instance;
      private string AppVer, AppState, GamePath, GameVer;
      private bool IsGameRunning;

      private bool IsInjected => AppState == "modnix" || AppState == "both";
      private bool CanModify => AppState != null && ! IsGameRunning;

      public MainWindow () { try {
         InitializeComponent();
         Log( "Disclaimer:\nModnix icon made from Phoenix Point's Technicial icon\n" +
              "Info and Red Cross icons from https://en.wikipedia.org/ under Public Domain\n" +
              "Other action icons from https://www.visualpharm.com/ (https://icons8.com/) under its Linkware License\n" +
              "Site icons belong to relevant sites." );
         RefreshGUI();
      } catch ( Exception ex ) { Console.WriteLine( ex ); } }

      private void RefreshGUI () { try {
         Log( "Resetting GUI" );
         RefreshAppInfo();
         RefreshGameInfo();
         RefreshModList();
         RefreshUpdateStatus();
         Log( "Initiating Controller" );
         App.CheckStatusAsync();
         if ( ! App.ParamSkipStartupCheck )
            CheckUpdate( false );
      } catch ( Exception ex ) { Log( ex ); } }

      public void SetInfo ( GuiInfo info, object value ) { this.Dispatch( () => { try {
         Log( $"Set {info} = {value}" );
         string txt = value?.ToString();
         switch ( info ) {
            case GuiInfo.VISIBILITY : Show(); break;
            case GuiInfo.APP_VER : AppVer = txt; RefreshAppInfo(); break;
            case GuiInfo.APP_STATE : AppState = txt; RefreshAppInfo(); break;
            case GuiInfo.APP_UPDATE : Update = value; UpdateChecked(); RefreshUpdateStatus(); break;
            case GuiInfo.GAME_RUNNING : IsGameRunning = (bool) value; RefreshAppInfo(); break;
            case GuiInfo.GAME_PATH : GamePath = txt; RefreshGameInfo(); break;
            case GuiInfo.GAME_VER :
               if ( GameVer == txt ) return;
               GameVer  = txt;
               RefreshGameInfo();
               App.GetModList();
               break;
            case GuiInfo.MOD_LIST :
               RefreshModList( value as IEnumerable<ModInfo> );
               break;
            default :
               Log( $"Unknown info {info}" );
               break;
         }
      } catch ( Exception ex ) { Log( ex ); } } ); }

      private void Window_Activated ( object sender, EventArgs e ) => CheckGameRunning();

      private void CheckGameRunning ( object _ = null ) {
         if ( AppControl.IsGameRunning() != IsGameRunning )
            SetInfo( GuiInfo.GAME_RUNNING, ! IsGameRunning );
      }

      #region App Info Area
      private void RefreshAppInfo () { try {
         Log( "Refreshing app info" );
         string txt = $"Modnix\rVer {AppVer}\rStatus: ";
         if ( IsGameRunning )
            txt += "Game is running";
         else if ( AppState == null )
            txt += "Busy";
         else
            switch ( AppState ) {
               case "ppml"   : txt += "PPML only, need setup"; break;
               case "both"   : txt += "PPML found, can remove"; break;
               case "modnix" : txt += "Injected"; break;
               case "setup"  : txt += "Requires Setup"; break;
               case "no_game": txt += "Game not found; Please do Manual Setup"; break;
               default: txt += "Unknown state; see log"; break;
            }
         RichAppInfo.TextRange().Text = txt;
         RefreshAppButtons();
         RefreshModList();
      } catch ( Exception ex ) { Log( ex ); } }

      private void RefreshAppButtons () { try {
         Log( "Refreshing app buttons, " + ( CanModify ? "can mod" : "cannot mod" ) );
         ButtonSetup.IsEnabled = AppState != null;
         ButtonRunOnline .IsEnabled = CanModify && GamePath != null;
         ButtonRunOffline.IsEnabled = CanModify && GamePath != null;
         ButtonModOpenModDir.IsEnabled = CurrentMod != null;
         ButtonModDelete.IsEnabled = CanDeleteMod;
         if ( IsGameRunning )
            BtnTxtSetup.Text = "Refresh";
         else if ( AppState == "modnix" )
            BtnTxtSetup.Text = "Revert";
         else if ( AppState == "modnix" )
            BtnTxtSetup.Text = "Setup";
         ButtonLoaderLog.IsEnabled = File.Exists( LoaderLog );
      } catch ( Exception ex ) { Log( ex ); } }

      private void ButtonWiki_Click ( object sender, RoutedEventArgs e ) => OpenUrl( "wiki", e );
      private void ButtonGitHub_Click ( object sender, RoutedEventArgs e ) => OpenUrl( "home", e );
      private void ButtonUserGuide_Click ( object sender, RoutedEventArgs e ) => OpenUrl( "guide", e );

      private void ButtonSetup_Click ( object sender, RoutedEventArgs e ) { try {
         Log( "Main action button clicked" );
         if ( e?.Source is UIElement src ) src.Focus();
         if ( IsGameRunning ) { // Refresh
            CheckGameRunning();
            return;
         }
         switch ( AppState ) {
            case "ppml" : case "both" : case "setup" :
               DoSetup();
               break;
            case "modnix" :
               if ( MessageBox.Show( "Remove Modnix from Phoenix Poing?", "Revert", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No ) == MessageBoxResult.Yes )
                  DoRestore();
               break;
            default:
               DoManualSetup();
               break;
         }
      } catch ( Exception ex ) { Log( ex ); } }

      private void DoSetup () {
         Log( "Calling setup" );
         AppState = null;
         RefreshAppInfo();
         App.DoSetupAsync();
      }

      private void DoManualSetup () => OpenUrl( "my_doc", null );

      private void DoRestore () {
         Log( "Calling restore" );
         AppState = null;
         RefreshAppInfo();
         App.DoRestoreAsync();
      }

      private void ButtonModDir_Click ( object sender, RoutedEventArgs e ) { try {
         string arg = $"/select, \"{Path.Combine( App.ModFolder, App.ModGuiExe )}\"";
         Log( $"Launching explorer.exe {arg}" );
         Process.Start( "explorer.exe", arg );
      } catch ( Exception ex ) { Log( ex ); } }

      public void Prompt ( string parts, Exception ex = null ) { this.Dispatch( () => { try {
         Log( $"Prompt {parts}" );
         SharedGui.Prompt( parts, ex, () => {
            AppControl.Explore( App.ModGuiExe );
         } );
      } catch ( Exception err ) { Log( err ); } } ); }

      private void ButtonNexus_Click ( object sender, RoutedEventArgs e ) => OpenUrl( "nexus", e );
      #endregion

      #region Game Info Area
      private void RefreshGameInfo () { try {
         Log( "Refreshing game info" );
         string txt = "Phoenix Point";
         if ( GamePath != null ) {
            txt += "\r" + Path.GetFullPath( GamePath );
            if ( GameVer  != null )
               txt += "\rVer: " + GameVer;
         } else
            txt += "Game not found";
         RichGameInfo.TextRange().Text = txt;
         RefreshAppButtons();
      } catch ( Exception ex ) { Log( ex ); } }

      private void ButtonOnline_Click  ( object sender, RoutedEventArgs e ) {
         App.LaunchGame( "online" );
         SetInfo( GuiInfo.GAME_RUNNING, true );
         new Timer( CheckGameRunning, null, 10_000, Timeout.Infinite );
      }

      private void ButtonOffline_Click ( object sender, RoutedEventArgs e ) {
         App.LaunchGame( "offline" );
         SetInfo( GuiInfo.GAME_RUNNING, true );
         new Timer( CheckGameRunning, null, 10_000, Timeout.Infinite );
      }

      private void ButtonCanny_Click   ( object sender, RoutedEventArgs e ) => OpenUrl( "canny", e );
      private void ButtonDiscord_Click ( object sender, RoutedEventArgs e ) => OpenUrl( "discord", e );
      private void ButtonForum_Click   ( object sender, RoutedEventArgs e ) => OpenUrl( "forum", e );
      private void ButtonManual_Click  ( object sender, RoutedEventArgs e ) => OpenUrl( "manual", e );
      private void ButtonReddit_Click  ( object sender, RoutedEventArgs e ) => OpenUrl( "reddit", e );
      private void ButtonTwitter_Click ( object sender, RoutedEventArgs e ) => OpenUrl( "twitter", e );
      private void ButtonWebsite_Click ( object sender, RoutedEventArgs e ) => OpenUrl( "www", e );
      #endregion

      #region Mod Info Area
      private ModInfo CurrentMod;
      private IEnumerable<ModInfo> ModList;

      private bool CanDeleteMod => CanModify && CurrentMod != null && ! (bool) CurrentMod.Query( ModQueryType.IS_CHILD );

      private void RefreshModList ( IEnumerable<ModInfo> list ) {
         ModList = list;
         RefreshModList();
      }

      private void RefreshModList () { try {
         Log( "Refreshing mod list" );
         ButtonAddMod.IsEnabled = Directory.Exists( App.ModFolder );
         ButtonModDir.IsEnabled = Directory.Exists( App.ModFolder );
         ButtonRefreshMod.IsEnabled = AppState != null;
         if ( GridModList.ItemsSource != ModList ) {
            Log( "New mod list, clearing selection" );
            GridModList.ItemsSource = ModList;
            RefreshModInfo( null );
         }
         if ( IsInjected || AppState == null || IsGameRunning ) {
            LabelModList.Content = AppState == null || ModList == null ? "Checking..." : $"{ModList.Count()} Mods";
            LabelModList.Foreground = Brushes.Black;
         } else {
            LabelModList.Content = "NOT INSTALLED";
            LabelModList.Foreground = Brushes.Red;
         }
         GridModList.Items?.Refresh();
      } catch ( Exception ex ) { Log( ex ); } }

      private void RefreshModInfo ( ModInfo mod ) {
         if ( mod == CurrentMod ) return;
         CurrentMod = mod;
         RefreshModInfo();
      }

      private void RefreshModInfo () { try {
         RefreshAppButtons();
         if ( CurrentMod == null ) {
            Log( "Clearing mod info" );
            RichModInfo.TextRange().Text = "";
            BkgdModeInfo.Opacity = 0.5;
            return;
         }
         Log( $"Refreshing mod {CurrentMod}" );
         BkgdModeInfo.Opacity = 0;
         CurrentMod.BuildDesc( RichModInfo.Document );
      } catch ( Exception ex ) { Log( ex ); } }

      private void ButtonAddMod_Click ( object sender, RoutedEventArgs e ) {
         RefreshModList( null );
         new Timer( ( _ ) => App.GetModList(), null, 100, Timeout.Infinite );
      }

      private void ButtonModOpenModDir_Click ( object sender, RoutedEventArgs e ) {
         string path = CurrentMod?.Path;
         if ( string.IsNullOrWhiteSpace( path ) ) return;
         AppControl.Explore( path );
      }

      private void ButtonModDelete_Click ( object sender, RoutedEventArgs e ) {
         if ( CurrentMod == null ) return;
         ModActionType action = ModActionType.DELETE_FILE;
         if ( (bool) CurrentMod.Query( ModQueryType.IS_FOLDER ) ) {
            var ans = MessageBox.Show( $"Delete {CurrentMod.Name} folder?\nSay no to delete just the file.", "Confirm",
                  MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Cancel );
            if ( ans == MessageBoxResult.Cancel ) return;
            if ( ans == MessageBoxResult.Yes )
               action = ModActionType.DELETE_DIR;
         } else {
            var ans = MessageBox.Show( $"Delete {CurrentMod.Name}?", "Confirm",
                  MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No );
            if ( ans == MessageBoxResult.No ) return;
         }
         if ( action == ModActionType.DELETE_FILE && (bool) CurrentMod.Query( ModQueryType.HAS_SETTINGS ) ) {
            var ans = MessageBox.Show( $"Delete {CurrentMod.Name} settings?", "Confirm",
                  MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Cancel );
            if ( ans == MessageBoxResult.Cancel ) return;
            if ( ans == MessageBoxResult.Yes )
               App.DoModActionAsync( ModActionType.DELETE_SETTINGS, CurrentMod );
         }
         ButtonModDelete.IsEnabled = false;
         App.DoModActionAsync( action, CurrentMod );
      }

      private void GridModList_CurrentCellChanged ( object sender, EventArgs e ) {
         if ( GridModList.CurrentItem == null ) return;
         RefreshModInfo( GridModList.CurrentItem as ModInfo );
      }
      #endregion

      #region Updater
      private object Update;

      private void CheckUpdate ( bool manual ) { try {
         if ( ! manual ) {
            DateTime lastCheck = Properties.Settings.Default.Last_Update_Check;
            Log( $"Last update check was {lastCheck}" );
            if ( lastCheck != null && ( DateTime.Now - lastCheck ).TotalDays < 7 ) return;
         }
         Log( "Checking update" );
         Update = "checking";
         RefreshUpdateStatus();
         App.CheckUpdateAsync();
      } catch ( Exception ex ) { Log( ex ); } }

      private void UpdateChecked () { try {
         Log( $"Updating last update check time." );
         Properties.Settings.Default.Last_Update_Check = DateTime.Now;
         Properties.Settings.Default.Save();
         ButtonCheckUpdate.IsEnabled = true;
      } catch ( Exception ex ) { Log( ex ); } }

      private void ButtonCheckUpdate_Click ( object sender, RoutedEventArgs e ) => CheckUpdate( true );

      private void RefreshUpdateStatus () { try {
         Log( $"Update is {(Update ?? "null")}" );
         if ( Object.Equals( "checking", Update ) ) {
            ButtonCheckUpdate.IsEnabled = false;
            BtnTextCheckUpdate.Text = "Checking...";
            return;
         }
         ButtonCheckUpdate.IsEnabled = true;
         BtnTextCheckUpdate.Text = "Check Update";
         GithubRelease release = Update as GithubRelease;
         if ( release == null ) return;

         MessageBoxResult result = MessageBox.Show( $"Update {release.Tag_Name} released.\nOpen download page?", "Updater", MessageBoxButton.YesNo );
         if ( result == MessageBoxResult.No ) return;
         if ( ! String.IsNullOrWhiteSpace( release.Html_Url ) )
            Process.Start( release.Html_Url );
      } catch ( Exception ex ) { Log( ex ); } }
      #endregion

      #region Logging
      public void Log ( object message ) {
         var txt = message?.ToString();
         Console.WriteLine( txt );
         string time = DateTime.Now.ToString( "hh:mm:ss.ffff ", InvariantCulture );
         this.Dispatch( () => { try {
            TextLog.AppendText( time + txt + "\n" );
            TextLog.ScrollToEnd();
            ButtonLogSave.IsEnabled = true;
         } catch ( Exception ex ) { Console.WriteLine( ex ); } } );
      }

      private void ButtonLogSave_Click ( object sender, RoutedEventArgs e ) { try {
         var dialog = new Microsoft.Win32.SaveFileDialog {
            FileName = AppControl.LIVE_NAME + " Log " + DateTime.Now.ToString( "u", InvariantCulture ).Replace( ':', '-' ),
            DefaultExt = ".txt",
            Filter = "Log Files (.txt .log)|*.txt;*.log|All Files|*.*"
         };
         if ( dialog.ShowDialog().GetValueOrDefault() ) {
            File.WriteAllText( dialog.FileName, TextLog.Text );
            AppControl.Explore( dialog.FileName );
         }
      } catch ( Exception ex ) { Log( ex ); } }

      private string LoaderLog => Path.Combine( App.ModFolder, "ModnixLoader.log" );

      private void ButtonLoaderLog_Click ( object sender, RoutedEventArgs e ) {
         if ( ! File.Exists( LoaderLog ) )
            MessageBox.Show( "Launch the game at least once to create loader log." );
         else
            AppControl.Explore( LoaderLog );
      }

      private void ButtonLogClear_Click ( object sender, RoutedEventArgs e ) {
         TextLog.Clear();
         ButtonLogSave.IsEnabled = false;
      }
      #endregion

      #region Openers
      private void OpenUrl ( string type, RoutedEventArgs e = null ) {
         Log( "OpenUrl " + type );
         if ( e?.Source is UIElement src ) src.Focus();
         string url;
         switch ( type ) {
            case "canny"  : url = "https://phoenixpoint.canny.io/feedback?sort=trending"; break;
            case "discord": url = "https://discordapp.com/invite/phoenixpoint"; break;
            case "forum"  : url = "https://forums.snapshotgames.com/c/phoenix-point"; break;
            case "guide"  : url = "https://github.com/Sheep-y/Modnix/wiki/"; break;
            case "home"   : url = "https://github.com/Sheep-y/Modnix"; break;
            case "manual" : url = "https://drive.google.com/open?id=1n8ORQeDtBkWcnn5Es4LcWBxif7NsXqet"; break;
            case "my_doc" : url = "https://github.com/Sheep-y/Modnix/wiki/Manual-Setup#wiki-wrapper"; break;
            case "nexus"  : url = "https://www.nexusmods.com/phoenixpoint/mods/?BH=0"; break;
            case "reddit" : url = "https://www.reddit.com/r/PhoenixPoint/"; break;
            case "twitter": url = "https://twitter.com/Phoenix_Point"; break;
            case "wiki"   : url = "https://phoenixpoint.fandom.com/wiki/"; break;
            case "www"    : url = "https://phoenixpoint.info/"; break;
            default       : return;
         }
         Log( $"Opening {url}" );
         Process.Start( url );
      }
      #endregion
   }

   public static class WpfHelper {
      public static TextRange TextRange ( this RichTextBox box ) {
         return new TextRange( box.Document.ContentStart, box.Document.ContentEnd );
      }
   }
}