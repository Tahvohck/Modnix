﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Sheepy.Modnix.MainGUI {

   public partial class SetupWindow : Window, IAppGui {

      private readonly AppControl App;
      private string AppVer, AppState, GamePath;
      private string Mode = "log"; // launch, setup, log
      private string LogContent;

      public SetupWindow ( AppControl app, string mode ) {
         Contract.Requires( app != null );
         App = app;
         Mode = mode;
         InitializeComponent();
         RefreshInfo();
      }

      public void SetInfo ( string info, string value ) { this.Dispatch( () => {
         switch ( info ) {
            case "visible": Show(); break;
            case "version": AppVer = value; break;
            case "state": AppState = value; break;
            case "game_path": GamePath = value; break;
            case "game_version": break;
            default: Log( $"Unknown info {info}" ); return;
         }
         RefreshInfo();
      } ); }

      private void RefreshInfo () {
         if ( Mode == "log" ) {
            TextMessage.Text = LogContent;
            return;
         }
         string txt = $"Modnix {AppVer}\r";
         if ( Mode == "launch" ) {
            txt += "Installed at\r" + App.ModGuiExe;
            AccessAction.Text = "_Launch";
            ButtonAction.IsEnabled = true;
         } else { // Mode == "setup"
            txt += "\rPhoenix Point\r{GamePath}";
            AccessAction.Text = "_Setup";
            ButtonAction.IsEnabled = GamePath != null;
         }
         TextMessage.Text = txt;
      }

      public void Prompt ( string parts, Exception ex = null ) {
         this.Dispatch( () => {
            Log( $"Prompt {parts}" );
            SharedGui.Prompt( parts, ex, () => {
               Process.Start( App.ModGuiExe, "/i " + Process.GetCurrentProcess().Id );
               Close();
            } );
         } );
      }

      public void Log ( string message ) {
         string time = DateTime.Now.ToString( "hh:mm:ss.ffff " );
         lock ( this ) {
            LogContent += $"{time} {message}";
            if ( Mode == "Log" )
               this.Dispatch( RefreshInfo );
         }
      }

   }
}
