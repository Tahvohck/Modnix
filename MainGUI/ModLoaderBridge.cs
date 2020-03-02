﻿using Sheepy.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using static Sheepy.Modnix.MainGUI.WpfHelper;

namespace Sheepy.Modnix.MainGUI {
   internal class ModLoaderBridge {
      private AppControl App = AppControl.Instance;
      private bool Loading;

      internal object LoadModList () {
         lock ( App ) {
            if ( Loading ) return null;
            Loading = true;
         }
         if ( ModLoader.NeedSetup ) {
            ModLoader.SetLog( new GUILogger( App.GUI ) );
            ModLoader.Setup();
         }
         App.Log( "Building mod list" );
         ModScanner.BuildModList();
         lock ( App ) {
            Loading = false;
            return ModScanner.AllMods.Select( e => new GridModItem( e ) );
         }
      }

      internal void Delete ( ModInfo mod, ModActionType type ) {
         App.Log( $"{type} {mod.Name}" );
         switch ( type ) {
            case ModActionType.DELETE_FILE : DeleteModFile( mod ); break;
            case ModActionType.DELETE_SETTINGS : break;
            case ModActionType.DELETE_DIR : DeleteModFolder( mod ); break;
            default: throw new ArgumentException( $"Unknown mod deletion {type}" );
         }
      }

      private void DeleteModFile ( ModInfo mod ) {
         string path = mod.Path;
         App.Log( $"Deleting {path}" );
         File.Delete( path );
         RemoveEmptyFolders( path );
      }

      private void DeleteModFolder ( ModInfo mod ) {
         string path = Path.GetDirectoryName( mod.Path );
         if ( path == ModLoader.ModDirectory ) throw new IOException( "Cannot delete mod folder" );
         RecurDelete( path );
      }

      private void RecurDelete ( string path ) {
         foreach ( var file in Directory.EnumerateFiles( path ) ) {
            App.Log( $"Deleting {file}" );
            File.Delete( file );
         }
         foreach ( var dir in Directory.EnumerateDirectories( path ) )
            RecurDelete( dir );
         App.Log( $"Deleting {path}" );
         Directory.Delete( path );
         RemoveEmptyFolders( path );
      }

      private void RemoveEmptyFolders ( string path ) {
         path = Path.GetDirectoryName( path );
         while ( path != ModLoader.ModDirectory && ! Directory.EnumerateFileSystemEntries( path ).Any() ) {
            App.Log( $"Deleting empty {path}" );
            Directory.Delete( path );
            path = Path.GetDirectoryName( path );
         }
      }
   }

   internal class GridModItem : ModInfo {
      internal readonly ModEntry Mod;
      internal GridModItem ( ModEntry mod ) => Mod = mod ?? throw new ArgumentNullException( nameof( mod ) );
      public override string Name => Mod.Metadata.Name?.ToString( "en" );
      public override string Version => Mod.Metadata.Version?.ToString();
      public override string Author => Mod.Metadata.Author?.ToString( "en" );
      public override string Status { get { lock ( Mod ) return Mod.Disabled ? "Disabled" : "Enabled"; } }

      public override object Query ( ModQueryType prop ) { lock ( Mod ) {
         switch ( prop ) {
            case ModQueryType.IS_FOLDER :
               var path = System.IO.Path.GetDirectoryName( Path );
               return path != AppControl.Instance.ModFolder && Directory.EnumerateFileSystemEntries( path ).Count() > 1;
            case ModQueryType.IS_CHILD :
               return Mod.Parent != null;
            case ModQueryType.HAS_SETTINGS :
               return false;
            default:
               return null;
         }
      } }

      public override void BuildDesc ( FlowDocument doc ) { lock ( Mod ) {
         doc.Replace(
            BuildBlock( BuildBasicDesc ),
            new Paragraph( new Run( Mod.Metadata.Description?.ToString( "en" ) ) ),
            BuildBlock( BuildLinks ),
            BuildBlock( BuildContacts ),
            BuildBlock( BuildFileList ),
            BuildCopyright()
         );
      } }

      private void BuildBasicDesc ( ModMeta meta, InlineCollection list ) {
         list.Add( new Bold( new Run( meta.Name.ToString( "en" ) ) ) );
         if ( meta.Version != null ) list.Add( $"\tVer {Version}" );
         list.Add( $"\rType\t{Type}" );
         if ( meta.Author != null ) list.Add( $"\rAuthor\t{Author}" );
      }

      private static void BuildLinks ( ModMeta meta, InlineCollection list ) {
         if ( meta.Url == null ) return;
         list.Add( "Link(s)" );
         BuildDict( meta.Url, list );
      }

      private void BuildContacts ( ModMeta meta, InlineCollection list ) {
         if ( meta.Contact == null ) return;
         list.Add( "Contact(s)" );
         BuildDict( meta.Contact, list );
      }

      private void BuildFileList ( ModMeta meta, InlineCollection list ) {
         Func< string, string > fileName = System.IO.Path.GetFileName;
         list.Add( "File(s)" );
         var self = fileName( Path );
         var selfRun = new Run( "\r" + self );
         list.Add( selfRun );
         foreach ( var e in meta.Dlls ) {
            var path = fileName( e.Path );
            if ( path == self ) list.Remove( selfRun );
            list.Add( "\r" + path + " [" + string.Join( ", ", e.Methods.Keys ) + "]" );
         }
      }

      private Block BuildCopyright () {
         var txt = Mod.Metadata.Copyright?.ToString( "en" );
         if ( string.IsNullOrWhiteSpace( txt ) ) return null;
         if ( ! txt.StartsWith( "Copyright", StringComparison.InvariantCultureIgnoreCase ) )
            txt = "Copyright: " + txt;
         return new Paragraph( new Run( txt ) );
      }

      private Block BuildBlock ( Action<ModMeta,InlineCollection> builder ) {
         var block = new Paragraph();
         builder( Mod.Metadata, block.Inlines );
         return block.Inlines.Count == 0 ? null : block;
      }

      private static void BuildDict ( TextSet data, InlineCollection list ) {
         if ( data.Dict == null ) {
            list.Add( data.Default );
            return;
         }
         foreach ( var e in data.Dict ) {
            string name = e.Key, link = e.Value;
            if ( string.IsNullOrWhiteSpace( name ) || string.IsNullOrWhiteSpace( link ) ) continue;
            list.Add( "\r" + name + "\t" ); 
            list.Add( new Hyperlink( new Run( link ) ){ NavigateUri = new Uri( link ) } );
         }
      }

      public override string Path => Mod.Path;

      public override string Type { get { lock ( Mod ) {
         var dlls = Mod.Metadata.Dlls;
         if ( dlls == null ) return "???";
         if ( dlls.Any( e => e?.Methods?.ContainsKey( "Init" ) ?? false ) ) return "PPML";
         if ( dlls.Any( e => e?.Methods?.ContainsKey( "Initialize" ) ?? false ) ) return "PPML+";
         return "DLL";
      } } }

      public override string ToString () { lock ( Mod ) return Mod.ToString(); }
   }

   internal class GUILogger : Logger {
      private readonly IAppGui GUI;
      public GUILogger ( IAppGui gui ) => GUI = gui;
      public override void Clear () { }
      public override void Flush () { }
      protected override void _Log ( LogEntry entry ) => GUI.Log( EntryToString( entry ) );
   }
}
