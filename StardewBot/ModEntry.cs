﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using StardewBot.Pathfinder;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Tools;

namespace StardewBot
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        internal static bool FeedLocation = false;
        SpeechEngine speechEngine;
        public static Action<string, LogLevel> log { get; private set; }
        public static Dictionary<string, Stream> Streams { get; set; } = new Dictionary<string, Stream>();

        /*********
** Public methods
*********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.GameLoop.UpdateTicked += GameLoop_UpdateTicked;
            helper.Events.Player.Warped += this.OnWarped;
            ModEntry.log = this.Monitor.Log;
            this.speechEngine = new SpeechEngine();
            this.speechEngine.LaunchProcess();

        }

        public static void Log(string msg) {
            ModEntry.log(msg, LogLevel.Debug);
        }

        public static void WriteJson(string fname, object obj) 
        {
            var settings = new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Ignore };
            settings.Error = (serializer, err) => err.ErrorContext.Handled = true;
            string objStr = JsonConvert.SerializeObject(obj, Formatting.None, settings);
            string path = @"C:\Program Files (x86)\GOG Galaxy\Games\Stardew Valley\Mods\StardewBot\StardewBot\lib\speech-client\debug\" + fname;
            using (var writetext = new StreamWriter(path))
            {
                writetext.WriteLine(objStr);
            }
        }

        private void OnWarped(object sender, WarpedEventArgs e)
        {
            long milliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            var oldLocation = e.OldLocation.NameOrUniqueName;
            var newLocation = e.NewLocation.NameOrUniqueName;
            var warpEvent = new { timestamp = milliseconds, oldLocation, newLocation };
            foreach (var pair in ModEntry.Streams)
            {
                var id = pair.Key;
                var stream = pair.Value;
                if (stream.Name == "ON_WARPED") {
                    var message = new { stream_id = id, value = warpEvent };
                    this.speechEngine.SendMessage("STREAM_MESSAGE", message);
                    Log($"Warped to {e.NewLocation}");
                }
            }
            //this.speechEngine.SendEvent("ON_WARPED", warpEvent);
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady) { }
                //return;
            // print button presses to the console window
            string pressed = e.Button.ToString();
            //this.Monitor.Log($"{Game1.player.Name} presseddd {e.Button}.", LogLevel.Debug);
            //this.Monitor.Log(e.Button.ToString(), LogLevel.Debug);
            if (pressed == "M") {
                var menu = Game1.activeClickableMenu;
                var pa = Game1.player.CurrentTool as Pickaxe;
            }
            if (pressed == "P")
            {
                this.Monitor.Log("pressed p");
                if (Routing.Ready)
                {
                    var route = Routing.GetRoute("HaleyHouse");
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    var targetTile = Game1.currentCursorTile;
                    var targetX = (int)targetTile.X;
                    var targetY = (int)targetTile.Y;

                    var path = Pathfinder.Pathfinder.FindPath(Game1.player.currentLocation, Game1.player.getTileX(), Game1.player.getTileY(), targetX, targetY, -1);
                    if (path == null)
                    {
                        ModEntry.Log("No path found");
                    }
                    else { 
                        foreach(var node in path) {
                            ModEntry.Log($"{node.X}, {node.Y}");
                        }
                    }
                    watch.Stop();
                    var elapsedMs = watch.ElapsedMilliseconds;
                    ModEntry.Log($"{elapsedMs}");
                    }
                else
                {
                    Routing.Reset();
                }
                //Core.EquipToolIfOnHotbar("Pickaxe");
            }
            else if (pressed == "L")
            {
                var location = Game1.player.currentLocation;
                var point = Game1.currentCursorTile;
                var tileX = (int)point.X;
                var tileY = (int)point.Y;
                var x = Game1.player.Position.X / Game1.tileSize;
                var y = Game1.player.Position.Y / Game1.tileSize;
                this.Monitor.Log($"Current tiles: x: {Game1.player.getTileX()}, y: {Game1.player.getTileY()} --- {x}, {y}", LogLevel.Debug);
                //var location = Game1.player.currentLocation;
                var v = new Vector2(tileX, tileY);
                var rec = new xTile.Dimensions.Location(tileX, tileY);
                var pl = Game1.player;
                WriteJson("debris.json", Game1.currentLocation.debris.ToList());
                WriteJson("objects.json", Game1.currentLocation.Objects.Values.ToList());
            }
        }
        private void GameLoop_UpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            foreach (var pair in ModEntry.Streams)
            {
                var id = pair.Key;
                var stream = pair.Value;
                if (stream.Name != "UPDATE_TICKED" || !e.IsMultipleOf((uint)stream.Data.ticks)) continue;
                var value = stream.Gather(e);
                var message = new { stream_id = id, value };
                this.speechEngine.SendMessage("STREAM_MESSAGE", message);
            }
        }
    }
}