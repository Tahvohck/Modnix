﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace Sheepy.Modnix.Tests {
   using ActionDef = Dictionary<string,object>;

   [TestClass()]
   public class ActionTest {

      [TestMethod()] public void FilterActionTest () {
         ActionDef[] defs = new ActionDef[]{
            CreateDef( "action", "Default", "all", "Def1" ),
            CreateDef( "eval", "Code1" ),
            CreateDef( "action", "Default", "more", "Def2" ),
            CreateDef( "skip", "splash", "phase", "SplashMod" ),
            CreateDef( "eval", "Code2" ),
         };

         var splash = ModActions.FilterActions( defs, "splashmod", out int defCount );
         Assert.AreEqual( 1, splash?.Count, "1 splash actions" );
         splash[0].TryGetValue( "skip", out object val );
         Assert.AreEqual( "splash", val, "splash field" );
         splash[0].TryGetValue( "all", out val );
         Assert.AreEqual( "Def1", val, "splash def 1" );
         splash[0].TryGetValue( "more", out val );
         Assert.AreEqual( "Def2", val, "splash def 2" );
         Assert.AreEqual( 2, defCount, "splash defCount" );

         var main = ModActions.FilterActions( defs, "mainmod", out defCount );
         Assert.AreEqual( 2, main?.Count, "2 main actions" );
         Assert.AreEqual( 2, defCount, "main defCount" );
      }

      private static ActionDef CreateDef ( params string[] keyValues ) {
         var result = new ActionDef();
         for ( int i = 0 ; i < keyValues.Length ; i += 2 )
            result.Add( keyValues[i]?.ToString(), keyValues[ i+1 ] );
         return result;
      }

   }

}