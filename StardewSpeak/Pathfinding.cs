﻿using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xTile.ObjectModel;
using xTile.Tiles;

namespace StardewSpeak.Pathfinder
{
    public class Location
    {
        public int X;
        public int Y;
        public int F;
        public int G;
        public int H;
        public Location Parent;
        public bool Preferable = false;
    }

    public class Point
    {
        public int X;
        public int Y;

        public Point(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }
    }

    public class Pathfinder
    {
        public static List<Point> FindPath(GameLocation location, int startX, int startY, int targetX, int targetY, int cutoff = -1)
        {
            if (!IsPassable(location, targetX, targetY)) return null;
            Location current = null;
            Location start = new Location { X = startX, Y = startY };
            Location target = new Location { X = targetX, Y = targetY };
            var openList = new List<Location>();
            var closedList = new List<Location>();
            int g = 0;
            var passableCache = new Dictionary<Tuple<int, int>, bool>();
            
            // start by adding the original position to the open list  
            openList.Add(start);

            while (openList.Count > 0)
            {
                // get the square with the lowest F score  
                var lowest = openList.Min(l => l.F);
                current = openList.First(l => l.F == lowest);

                // add to closed, remove from open
                closedList.Add(current);
                openList.Remove(current);

                // if closed contains destination, we're done
                if (closedList.FirstOrDefault(l => l.X == target.X && l.Y == target.Y) != null) break;

                // if closed has exceed cutoff, break out and fail
                if (cutoff > 0 && closedList.Count > cutoff)
                {
                    //Mod.instance.Monitor.Log("Breaking out of pathfinding, cutoff exceeded");
                    return null;
                }
                var adjacentSquares = GetWalkableAdjacentSquares(current.X, current.Y, location, openList, passableCache);
                g = current.G + 1;

                foreach (var adjacentSquare in adjacentSquares)
                {
                    // if closed, ignore 
                    if (closedList.FirstOrDefault(l => l.X == adjacentSquare.X
                        && l.Y == adjacentSquare.Y) != null)
                        continue;

                    // if it's not in open
                    if (openList.FirstOrDefault(l => l.X == adjacentSquare.X
                        && l.Y == adjacentSquare.Y) == null)
                    {
                        // compute score, set parent  
                        adjacentSquare.G = g;
                        adjacentSquare.H = ComputeHScore(adjacentSquare.Preferable, adjacentSquare.X, adjacentSquare.Y, target.X, target.Y);
                        adjacentSquare.F = adjacentSquare.G + adjacentSquare.H;
                        adjacentSquare.Parent = current;

                        // and add it to open
                        openList.Insert(0, adjacentSquare);
                    }
                    else
                    {
                        // test if using the current G score makes the adjacent square's F score lower
                        // if yes update the parent because it means it's a better path  
                        if (g + adjacentSquare.H < adjacentSquare.F)
                        {
                            adjacentSquare.G = g;
                            adjacentSquare.F = adjacentSquare.G + adjacentSquare.H;
                            adjacentSquare.Parent = current;
                        }
                    }
                }
            }
            //make sure path is complete
            if (current == null) return null;
            if (current.X != targetX || current.Y != targetY)
            {
                //Mod.instance.Monitor.Log("No path available.", StardewModdingAPI.LogLevel.Warn);
                return null;
            }

            // if path exists, let's pack it up for return
            var returnPath = new List<Point>();
            while (current != null)
            {
                returnPath.Add(new Point(current.X, current.Y));
                current = current.Parent;
            }
            returnPath.Reverse();
            return returnPath;
        }

        static List<Location> GetWalkableAdjacentSquares(int x, int y, GameLocation map, List<Location> openList, Dictionary<Tuple<int, int>, bool> passableCache)
        {
            List<Location> list = new List<Location>();

            if (IsPassable(map, x, y - 1, passableCache))
            {
                Location node = openList.Find(l => l.X == x && l.Y == y - 1);
                if (node == null) list.Add(new Location() { Preferable = IsPreferableWalkingSurface(map, x, y), X = x, Y = y - 1 });
                else list.Add(node);
            }

            if (IsPassable(map, x, y + 1, passableCache))
            {
                Location node = openList.Find(l => l.X == x && l.Y == y + 1);
                if (node == null) list.Add(new Location() { Preferable = IsPreferableWalkingSurface(map, x, y), X = x, Y = y + 1 });
                else list.Add(node);
            }

            if (IsPassable(map, x - 1, y, passableCache))
            {
                Location node = openList.Find(l => l.X == x - 1 && l.Y == y);
                if (node == null) list.Add(new Location() { Preferable = IsPreferableWalkingSurface(map, x, y), X = x - 1, Y = y });
                else list.Add(node);
            }

            if (IsPassable(map, x + 1, y, passableCache))
            {
                Location node = openList.Find(l => l.X == x + 1 && l.Y == y);
                if (node == null) list.Add(new Location() { Preferable = IsPreferableWalkingSurface(map, x, y), X = x + 1, Y = y });
                else list.Add(node);
            }

            return list;
        }

        static bool IsPreferableWalkingSurface(GameLocation location, int x, int y)
        {
            //todo, make roads more desireable
            return false;
        }

        public static bool IsPassable(GameLocation loc, int x, int y, Dictionary<Tuple<int, int>, bool> passableCache) 
        {
            bool passable;
            var key = new Tuple<int, int>(x, y);
            if (!passableCache.TryGetValue(key, out passable)) {
                passable = IsPassable(loc, x, y);
                passableCache.Add(key, passable);
            }
            return passable;
        }

        public static bool IsPassable(GameLocation loc, int x, int y)
        {
            foreach (var w in loc.warps)
            {
                if (w.X == x && w.Y == y) return true;
            }
            var vec = new Vector2(x, y);
            if (isTileOccupied(loc, vec) || !loc.isTileOnMap(vec))
            {
                return false;
            }
            var tile = loc.Map.GetLayer("Buildings").Tiles[x, y];
            if (tile != null && tile.TileIndex != -1)
            {
                PropertyValue property = null;
                string value2 = null;
                tile.TileIndexProperties.TryGetValue("Action", out property);
                if (property == null)
                {
                    tile.Properties.TryGetValue("Action", out property);
                }
                if (property != null)
                {
                    value2 = property.ToString();
                    if (value2.StartsWith("LockedDoorWarp"))
                    {
                        return false;
                    }
                    if (!value2.Contains("Door") && !value2.Contains("Passable"))
                    {
                        return false;
                    }
                }
                else if (loc.doesTileHaveProperty(x, y, "Passable", "Buildings") == null)
                {
                    return false;
                }
            }
            if (loc.doesTileHaveProperty(x, y, "NoPath", "Back") != null)
            {
                return false;
            }
            if (loc is Farm)
            {
                var fff = loc as Farm;
                if (fff.getBuildingAt(vec) != null)
                {
                    return false;
                }
            }
            foreach (var rc in loc.resourceClumps)
            {
                
                if (rc.occupiesTile(x, y))
                {
                    return false;
                }
            }
            return true;
        }

        static int ComputeHScore(bool preferable, int x, int y, int targetX, int targetY)
        {
            return (Math.Abs(targetX - x) + Math.Abs(targetY - y)) - (preferable ? 1 : 0);
        }

        public static bool isTileOccupied(GameLocation location, Vector2 tileLocation, string characterToIgnore = "")
        {
            location.objects.TryGetValue(tileLocation, out StardewValley.Object o);
            if (o != null) 
            {
                return !o.isPassable();
            }
            Microsoft.Xna.Framework.Rectangle tileLocationRect = new Microsoft.Xna.Framework.Rectangle((int)tileLocation.X * 64 + 1, (int)tileLocation.Y * 64 + 1, 62, 62);
            for (int i = 0; i < location.characters.Count; i++)
            {
                if (location.characters[i] != null && !location.characters[i].name.Equals(characterToIgnore) && location.characters[i].GetBoundingBox().Intersects(tileLocationRect))
                {
                    return true;
                }
            }
            if (location.terrainFeatures.ContainsKey(tileLocation) && tileLocationRect.Intersects(location.terrainFeatures[tileLocation].getBoundingBox(tileLocation)) && !location.terrainFeatures[tileLocation].isPassable())
            {
                return true;
            }
            if (location.largeTerrainFeatures != null)
            {
                foreach (LargeTerrainFeature largeTerrainFeature in location.largeTerrainFeatures)
                {
                    if (largeTerrainFeature.getBoundingBox().Intersects(tileLocationRect))
                    {
                        return true;
                    }
                }
            }
            var f = location.GetFurnitureAt(tileLocation);
            if (f != null && !f.isPassable())
            {
                return true;
            }
            return false;
        }
    }
}