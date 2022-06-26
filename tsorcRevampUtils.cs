using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.Chat;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp
{

    //using static tsorcRevamp.SpawnHelper;
    public static class SpawnHelper
    {

        //undergroundJungle, undergroundEvil, and undergroundHoly are deliberately missing. call Cavern && p.zone instead.

        public static bool Cavern(Player p)
        { //if youre calling Cavern without a p.zone check, also call NoSpecialBiome
            return p.position.Y >= Main.rockLayer && p.position.Y <= Main.rockLayer * 25;
        }

        public static bool NoSpecialBiome(Player p)
        {
            return (!p.ZoneJungle && !p.ZoneCorrupt && !p.ZoneCrimson && !p.ZoneHallow && !p.ZoneMeteor && !p.ZoneDungeon);
        }

        public static bool Sky(Player p)
        { //p.ZoneSkyHeight is more restrictive than this, so use this if an enemy uses it
            return p.position.Y < Main.worldSurface * 0.44999998807907104;
        }

        public static bool Surface(Player p)
        {
            return !Sky(p) && (p.position.Y < Main.worldSurface); //dont need to check nospecialbiome here since we're already calling Sky
        }

        public static bool Underground(Player p)
        {
            return Main.worldSurface > p.position.Y && p.position.Y < Main.rockLayer;
        }

        public static bool Underworld(Player p)
        {
            int playerYTile = (int)(p.Bottom.Y + 8f) / 16;
            return playerYTile > Main.maxTilesY - 190;
        }
    }

    //using static tsorcRevamp.oSpawnHelper;
    public static class oSpawnHelper
    {

        public static bool oCavern(Player p)
        {
            return (p.position.Y >= (Main.rockLayer * 17)) && (p.position.Y < (Main.rockLayer * 24));
        }

        public static bool oCavernByTile(Player p)
        {
            int playerYTile = (int)(p.Bottom.Y + 8f) / 16;
            return playerYTile >= (Main.maxTilesY * 0.4f) && playerYTile < (Main.maxTilesY * 0.6f);
        }

        public static bool oMagmaCavern(Player p)
        {
            return (p.position.Y >= (Main.rockLayer * 24)) && (p.position.Y < (Main.rockLayer * 32));
        }

        public static bool oMagmaCavernByTile(Player p)
        {
            int playerYTile = (int)(p.Bottom.Y + 8f) / 16;
            return playerYTile >= (Main.maxTilesY * 0.6f) && playerYTile < (Main.maxTilesY * 0.8f);
        }

        public static bool oSky(Player p)
        {
            return p.position.Y <= Main.rockLayer * 4;
        }

        public static bool oSkyByTile(Player p)
        {
            int playerYTile = (int)(p.Bottom.Y + 8f) / 16;
            return playerYTile < (Main.maxTilesY * 0.1f);
        }

        public static bool oSurface(Player p)
        {
            return !p.ZoneSkyHeight && (p.position.Y <= Main.worldSurface);
        }

        public static bool oUnderground(Player p)
        {
            return (p.position.Y >= (Main.rockLayer * 13)) && (p.position.Y < (Main.rockLayer * 17));
        }

        public static bool oUndergroundByTile(Player p)
        {
            int playerYTile = (int)(p.Bottom.Y + 8f) / 16;
            return playerYTile >= (Main.maxTilesY * 0.3f) && playerYTile < (Main.maxTilesY * 0.4f);
        }

        public static bool oUnderSurface(Player p)
        {
            return (p.position.Y > (Main.rockLayer * 8)) && (p.position.Y < (Main.rockLayer * 13));
        }

        public static bool oUnderSurfaceByTile(Player p)
        {
            int playerYTile = (int)(p.Bottom.Y + 8f) / 16;
            return (playerYTile >= (Main.maxTilesY * 0.2f) && playerYTile < (Main.maxTilesY * 0.3f));
        }

        public static bool oUnderworld(Player p)
        {
            return p.position.Y >= (Main.rockLayer * 32);
        }

        public static bool oUnderworldByHeight(Player p)
        {
            int playerYTile = (int)(p.Bottom.Y + 8f) / 16;
            return (playerYTile >= (Main.maxTilesY * 0.8f));
        }
    }

    public static class VariousConstants
    {
        public const int CUSTOM_MAP_WORLD_ID = 44874972;
        public const string MUSIC_MOD_URL = "https://github.com/timhjersted/tsorcDownload/raw/1.4/tsorcMusic.tmod";
        public const string MAP_URL = "https://github.com/timhjersted/tsorcDownload/raw/1.4/the-story-of-red-cloud.wld";
        public const string CHANGELOG_URL = "https://raw.githubusercontent.com/timhjersted/tsorcDownload/1.4/changelog.txt";
    }

    public static class PriceByRarity
    {

        //minimal exploration. pre-hardmode ores. likely no items that craft from souls will use this
        public static readonly int White_0 = Item.buyPrice(platinum: 0, gold: 0, silver: 40, copper: 0);

        //underground chest loots (shoe spikes, CiaB, etc), shadow orb items, floating island
        public static readonly int Blue_1 = Item.buyPrice(platinum: 0, gold: 2, silver: 25, copper: 0);

        //gold dungeon chest (handgun, cobalt shield, etc), goblin invasion
        public static readonly int Green_2 = Item.buyPrice(platinum: 0, gold: 4, silver: 50, copper: 0);

        //hell, underground jungle
        public static readonly int Orange_3 = Item.buyPrice(platinum: 0, gold: 7, silver: 50, copper: 0);

        //early hardmode (hm ores), mimics
        public static readonly int LightRed_4 = Item.buyPrice(platinum: 0, gold: 12, silver: 50, copper: 0);

        //hallowed tier. post mech, pre plantera. pirates.
        public static readonly int Pink_5 = Item.buyPrice(platinum: 0, gold: 25, silver: 50, copper: 0);

        //some biome mimic gear, high level tinkerer combinations (ankh charm, mechanical glove). seldom used in vanilla
        public static readonly int LightPurple_6 = Item.buyPrice(platinum: 0, gold: 37, silver: 50, copper: 0);

        //plantera, golem, chlorophyte
        public static readonly int Lime_7 = Item.buyPrice(platinum: 0, gold: 47, silver: 50, copper: 0);

        //post-plantera dungeon, martian madness, pumpkin/frost moon
        public static readonly int Yellow_8 = Item.buyPrice(platinum: 0, gold: 55, silver: 0, copper: 0);

        //lunar fragments, dev armor. seldom used in vanilla
        public static readonly int Cyan_9 = Item.buyPrice(platinum: 0, gold: 67, silver: 50, copper: 0);

        //luminite, lunar fragment gear, moon lord drops
        public static readonly int Red_10 = Item.buyPrice(platinum: 0, gold: 80, silver: 0, copper: 0);

        //no vanilla items have purple rarity base. only cyan and red with modifiers can be purple. im guessing on the price.
        public static readonly int Purple_11 = Item.buyPrice(platinum: 1, gold: 20, silver: 0, copper: 0);



    }

    public static class UsefulFunctions
    {
        ///<summary> 
        ///Gets the coordinates of the first solid thing a line fired in a certain direction will hit
        ///Counts both tiles and NPCs as solid
        ///Returns null if no collision
        ///This could be a lot faster and more accurate if necessary by simply bifurcating the distance repeatedly until the exact collision point is found
        ///That probably isn't necessary unless there's something that needs to run this every tick
        ///</summary>         
        ///<param name="start">The start of the vector</param>
        ///<param name="direction">The direction it's aiming in</param>
        ///<param name="maxDistance">How far to search for</param>
        ///<param name="ignoreFriendly">Ignore town NPCs in collision checks</param>
        public static Vector2 GetFirstCollision(Vector2 start, Vector2 direction, float maxDistance = 5000f, bool ignoreFriendly = false)
        {
            direction.Normalize();
            Vector2 unitVector = direction;
            direction *= 10;

            Vector2 currentPosition = direction * maxDistance / 10;
            for (float distance = 0; distance <= maxDistance / 10f; distance += 1f)
            {
                currentPosition = start + (direction * distance);

                if (!Collision.CanHit(start, 1, 1, currentPosition, 1, 1) && !Collision.CanHitLine(start, 1, 1, currentPosition, 1, 1))
                {
                    distance -= 5f;
                    break;
                }
            }

            float closestCollision = maxDistance;
            for(int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i] == null || Main.npc[i].active == false)
                {
                    continue;
                }
                if (ignoreFriendly && Main.npc[i].friendly) { continue; }
                else
                {
                    NPC npc = Main.npc[i];
                    float collision = maxDistance;

                    //Expand the enemy hitbox slightly to increase consistency
                    Vector2 adjustedPosition = npc.position;
                    Vector2 adjustedSize = npc.Size;
                    adjustedSize *= 1.5f;
                    adjustedPosition -= (adjustedSize - npc.Size) / 2;
                    if(Collision.CheckAABBvLineCollision(adjustedPosition, adjustedSize, start, currentPosition, 32, ref collision)){
                        if(collision < closestCollision)
                        {
                            closestCollision = collision;
                        }
                    }
                }
            }

            if(closestCollision < (start - currentPosition).Length())
            {
                currentPosition = start + (unitVector * closestCollision);
            }

            return currentPosition;
        }

        ///<summary> 
        ///Modifies a float between 0 and 1, and returns a 'softened' value between 0 and one calculated via the easing curve sin(x * pi/2)
        ///Useful for making smoother looking motion
        ///</summary>         
        ///<param name="source">A float between 0 and 1</param>
        public static float EasingCurve(float source)
        {
            return (float)Math.Sin(source * MathHelper.PiOver2);
        }

        ///<summary> 
        ///Returns a vector pointing from a source, to a target, with a speed.
        ///Simplifies basic projectile, enemy dash, etc aiming calculations to a single call.
        ///If "ballistic" is true it adjusts for gravity. Default is 0.1f, may be stronger or weaker for some projectiles though.
        ///</summary>         
        ///<param name="source">The start point of the vector</param>
        ///<param name="target">The end point it is aiming towards</param>
        ///<param name="speed">The length of the resulting vector</param>
        public static Vector2 GenerateTargetingVector(Vector2 source, Vector2 target, float speed)
        {
            Vector2 distance = target - source;
            distance.Normalize();
            return distance * speed;
        }

        ///<summary> 
        ///Returns a vector that indicates a true ballistic trajectory from a source to a target
        ///</summary>         
        ///<param name="source">The start point of the vector</param>
        ///<param name="target">The end point it is aiming towards</param>
        ///<param name="speed">The initial speed of the projectile</param>
        ///<param name="gravity">How much does the projectile's Y velocity increase every tick? Default is fairly close for aiStyle 1 projectiles, but for true accuracy set it yourself in the projectile AI instead of using an aiStyle</param>
        ///<param name="highAngle">There are two solutions to this equation. This makes it return the higher arcing one. Does not work at *all* for projectiles with vanilla aiStyles</param>
        ///<param name="fallback">If this is set to true it will fall back to GenerateTargetingVector if it's mathematically impossible to hit its target. If not it will return Vector2.Zero so you can handle it yourself</param>
        public static Vector2 BallisticTrajectory(Vector2 source, Vector2 target, float speed, float gravity = 0.035f, bool highAngle = false, bool fallback = true)
        {
            //This is where the fun begins
            Vector2 diff = target - source;
            diff.Y *= -1;

            double calculation = (gravity * diff.X * diff.X) + (2 * speed * speed * diff.Y);
            calculation *= gravity;
            calculation = Math.Pow(speed, 4) - calculation;
            calculation = Math.Sqrt(calculation);

            double angle;
            if (highAngle)
            {
                angle = Math.Atan(((speed * speed) + calculation) / (gravity * diff.X));
            }
            else
            {
                angle = Math.Atan(((speed * speed) - calculation) / (gravity * diff.X));
            }

            if (Double.IsNaN(angle))
            {
                if (fallback)
                {
                    return GenerateTargetingVector(source, target, speed);
                }
                else
                {
                    return Vector2.Zero;
                }
            }

            Vector2 velocity = new Vector2();
            velocity.X = speed * (float)Math.Cos(angle);
            velocity.Y = -speed * (float)Math.Sin(angle);

            if (diff.X < 0)
            {
                velocity *= -1;
            }
            return velocity;
        }

        ///<summary> 
        ///Draws a projectile fully lit. Goes in PreDraw(), simply return false after calling it.
        ///</summary>         
        ///<param name="spriteBatch">The currently open SpriteBatch</param>
        ///<param name="projectile">The projectile to be drawn</param>
        ///<param name="texture">An empty static Texture2D variable that this function can use to cache the projectile's texture.</param>
        public static void DrawSimpleLitProjectile(Projectile projectile, ref Texture2D texture)
        {
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (projectile.spriteDirection == -1)
            {
                spriteEffects = SpriteEffects.FlipHorizontally;
            }

            if (texture == null || texture.IsDisposed)
            {
                texture = (Texture2D)ModContent.Request<Texture2D>(projectile.ModProjectile.Texture);
            }

            int frameHeight = texture.Height / Main.projFrames[projectile.type];
            int startY = frameHeight * projectile.frame;
            Rectangle sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);
            Vector2 origin = sourceRectangle.Size() / 2f;
            Main.EntitySpriteDraw(texture,
                projectile.Center - Main.screenPosition + new Vector2(0f, projectile.gfxOffY),
                sourceRectangle, Color.White, projectile.rotation, origin, projectile.scale, spriteEffects, 0);
        }

        ///<summary> 
        ///Spawns a ring of dust around a point
        ///</summary>         
        ///<param name="center">Center of the dust ring</param>
        ///<param name="radius">Radius of the ring</param>
        ///<param name="dustID">ID of the dust to spawn</param>
        ///<param name="dustCount">How many to spawn per tick</param>
        ///<param name="dustSpeed">How fast dust should rotate</param>
        public static void DustRing(Vector2 center, float radius, int dustID, int dustCount = 5, float dustSpeed = 2)
        {
            for (int j = 0; j < dustCount; j++)
            {
                Vector2 dir = Main.rand.NextVector2CircularEdge(radius, radius);
                Vector2 dustPos = center + dir;
                Vector2 dustVel = new Vector2(dustSpeed, 0).RotatedBy(dir.ToRotation() + MathHelper.Pi / 2);
                Dust.NewDustPerfect(dustPos, dustID, dustVel, 200).noGravity = true;
            }
        }

        ///<summary> 
        ///Checks if a point is within an ellipse
        ///</summary>         
        ///<param name="point">Point to be checked</param>
        ///<param name="center">Centerpoint of the ellipse</param>
        ///<param name="width">How wide is the ellipse</param>
        ///<param name="height">How tall is the ellipse</param>
        public static bool IsPointWithinEllipse(Vector2 point, Vector2 center, float width, float height)
        {
            float xTerm = ((point.X - center.X) * (point.X - center.X)) / (width * width);
            float yTerm = ((point.Y - center.Y) * (point.Y - center.Y)) / (height * height);

            if (xTerm + yTerm < 1)
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        ///<summary> 
        ///Broadcasts a message from the server to all players. Safe to use in either multiplayer or singleplayer contexts, where it simply defaults to a NewText() instead.
        ///It does nothing when run on a multiplayer client, as that would make it spam a new copy of the message for every client that runs it. Use NewText() for client-only code like items!
        ///</summary>         
        ///<param name="text">String containing the text</param>
        public static void BroadcastText(string text)
        {
            BroadcastText(text, Color.Yellow);
        }
        public static void BroadcastText(string text, int r, int g, int b)
        {
            BroadcastText(text, new Color(r, g, b));
        }

        ///<summary> 
        ///Now in technicolor!
        ///</summary>         
        ///<param name="text">String containing the text</param>
        ///<param name="color">Color of the text</param>
        public static void BroadcastText(string text, Color color)
        {
            if (Main.netMode == NetmodeID.Server)
            {
                ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(text), color);
            }
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                Main.NewText(text, color);
            }
        }


        ///<summary> 
        ///Gets the first npc of a given type. Basically NPC.AnyNPC, except it actually returns what it finds.
        ///Uses nullable ints, aka "int?". Will return null if it can't find one.
        ///</summary>         
        ///<param name="type">Type of NPC to look for</param>
        public static int? GetFirstNPC(int type)
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && Main.npc[i].type == type)
                {
                    return i;
                }
            }

            return null;
        }

        ///<summary> 
        ///Gets the closest Enemy NPC to a given point. Returns that NPC's whoami.
        ///Uses nullable ints, will return null if it can't find one (like in the rare but possible situation where there *are* no NPCs). You've been warned!
        ///</summary>
        ///<param name="point">The point it's comparing to</param>
        public static int? GetClosestEnemyNPC(Vector2 point)
        {
            int? closestNPC = null;
            float distance = float.MaxValue;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && !Main.npc[i].friendly && !Main.npc[i].dontTakeDamage && (Main.npc[i].damage > 0 || Main.npc[i].type == NPCID.TargetDummy))
                {
                    float newDistance = Vector2.DistanceSquared(point, Main.npc[i].Center);
                    if (newDistance < distance)
                    {
                        distance = newDistance;
                        closestNPC = i;
                    }
                }
            }

            return closestNPC;
        }

        ///<summary> 
        ///Does this tile exist, and if so is it solid?
        ///Yes, this requires all of this to learn the answer safely. Nothing can be easy here.
        ///Also returns false if the tile is null, or if the coordinates you gave it are out of range of the tile array.
        ///</summary>
        ///<param name="tilePos">The coordinates of the tile</param>
        public static bool IsTileReallySolid(Vector2 tilePos)
        {
            if (Main.tile.Width > tilePos.X && Main.tile.Height > tilePos.Y)
            {
                Tile thisTile = Main.tile[(int)tilePos.X, (int)tilePos.Y];

                //null = tile is not instantiated at all (yes, that is possible) | active = tile is not air | inActive = actuated | Main.tileSolid = is it solid
                if (thisTile != null && thisTile.HasTile && !thisTile.IsActuated && Main.tileSolid[thisTile.TileType])
                {
                    return true;
                }
            }

            return false;
        }

        ///<summary> 
        ///Does this tile exist, and if so is it solid?
        ///Yes, this requires all of this to learn the answer safely. Nothing can be easy here.
        ///Also returns false if the tile is null, or if the coordinates you gave it are out of range of the tile array.
        ///</summary>
        ///<param name="X">The X coordinate of the tile</param>
        ///<param name="Y">The Y coordinate of the tile</param>
        public static bool IsTileReallySolid(int X, int Y)
        {
            if (Main.tile.Width > X && Main.tile.Height > Y)
            {
                Tile thisTile = Main.tile[X, Y];

                if (thisTile.HasTile && !thisTile.IsActuated && Main.tileSolid[thisTile.TileType])
                {
                    return true;
                }
            }

            return false;
        }

        ///<summary> 
        ///Call in a projectile's AI to allow the projectile to home on enemies
        ///</summary>         
        ///<param name="projectile">The current projectile</param>
        ///<param name="homingRadius">The homing radius</param>
        ///<param name="topSpeed">The projectile's maximum velocity</param>
        ///<param name="rotateTowards">Should the projectile maintain topSpeed speed and rotate towards targets, instead of standard homing?</param>
        ///<param name="homingStrength">The homing strength coefficient. Unused if rotateTowards.</param>
        ///<param name="needsLineOfSight">Does the projectile need line of sight to home on a target?</param>
        public static void HomeOnEnemy(Projectile projectile, float homingRadius, float topSpeed, bool rotateTowards = false, float homingStrength = 1f, bool needsLineOfSight = false)
        {
            if (!projectile.active || !projectile.friendly) return;
            const int BASE_STRENGTH = 30;

            Vector2 targetLocation = Vector2.UnitY;
            bool foundTarget = false;

            for (int i = 0; i < 200; i++)
            {
                if (!Main.npc[i].active) continue;
                float toNPCEdge = (Main.npc[i].width / 2) + (Main.npc[i].height / 2); //make homing on larger targets more consistent

                //WithinRange is just faster Distance (skips sqrt)
                if (Main.npc[i].CanBeChasedBy(projectile) && projectile.WithinRange(Main.npc[i].Center, homingRadius + toNPCEdge) && (!needsLineOfSight || Collision.CanHitLine(projectile.Center, 1, 1, Main.npc[i].Center, 1, 1)))
                {
                    targetLocation = Main.npc[i].Center;
                    foundTarget = true;
                    break;
                }
            }

            if (foundTarget)
            {
                Vector2 homingDirection = Vector2.Normalize(targetLocation - projectile.Center);
                projectile.velocity = (projectile.velocity * (BASE_STRENGTH / homingStrength) + homingDirection * topSpeed) / ((BASE_STRENGTH / homingStrength) + 1);
            }
            if (rotateTowards)
            {
                if (projectile.velocity.Length() < topSpeed)
                {
                    projectile.velocity *= topSpeed / projectile.velocity.Length();
                }
            }
            if (projectile.velocity.Length() > topSpeed)
            {
                projectile.velocity *= topSpeed / projectile.velocity.Length();
            }
        }


        ///<summary> 
        ///Spawns a client-side instanced item similar to treasure bags. Safe to use in single-player, where it simply drops the item normally.
        ///</summary>         
        ///<param name="Position">Where the item should be spawned</param>
        ///<param name="HitboxSize">How big a hitbox it should have</param>
        ///<param name="itemType">The type of item</param>
        ///<param name="itemStack">How many of the item it should spawn</param>
        ///<param name="includeThesePlayers">If it should only drop for specific players, pass a list of them here</param>
        public static void NewItemInstanced(Vector2 Position, Vector2 HitboxSize, int itemType, int itemStack = 1, List<int> includeThesePlayers = null)
        {
            int dummyItemIndex = Item.NewItem(new Terraria.DataStructures.EntitySource_Misc("¯\\_(ツ)_/¯"), Position, HitboxSize, itemType, itemStack, true, 0, false, false);
            Main.timeItemSlotCannotBeReusedFor[dummyItemIndex] = 54000;
            if (Main.netMode != NetmodeID.SinglePlayer)
            {
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    if (includeThesePlayers != null)
                    {
                        if (includeThesePlayers.Contains(i))
                        {
                            NetMessage.SendData(MessageID.InstancedItem, i, number: dummyItemIndex);
                        }
                    }
                    else
                    {
                        NetMessage.SendData(MessageID.InstancedItem, i, number: dummyItemIndex);
                    }
                }
                Main.item[dummyItemIndex].active = false;
            }
        }

        ///<summary> 
        ///Ends the previous spritebatch, and starts a new one that you can apply shaders to.
        ///Call it before drawing the thing you're trying to shade.
        ///Call it again *after* drawing the thing you're trying to shade to let the game return to the normal drawing mode.
        ///Thanks for the tip W1K!
        ///</summary>         
        ///<param name="spriteBatch">The spritebatch to operate on</param>
        public static void RestartSpritebatch(ref SpriteBatch spriteBatch)
        {
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, (Effect)null, Main.GameViewMatrix.TransformationMatrix);
        }

        ///<summary> 
        ///Compares the angle of two vectors. Returns the absolute value of the difference between their angles.
        ///Tip: I hate XNA so fucking much
        ///</summary>         
        ///<param name="firstVector">The first vector to be compared</param>
        ///<param name="secondVector">The second vector to be compared</param>
        public static double CompareAngles(Vector2 firstVector, Vector2 secondVector)
        {
            double a1 = firstVector.ToRotation();
            if (a1 < 0)
            {
                a1 = MathHelper.TwoPi + a1;
            }

            double a2 = secondVector.ToRotation();
            if (a2 < 0)
            {
                a2 = MathHelper.TwoPi + a2;
            }

            double c = a2 - a1;

            //The largest angle between two points on a circle should be Pi radians at most
            //If comparing them clockwise resulted in a bigger number, then compare them counterclockwise
            //Also shift them both over by a quadrant to avoid comparing across the 0/TwoPi breakpoint
            if (Math.Abs(c) > MathHelper.Pi)
            {
                a1 += MathHelper.PiOver2;
                a2 += MathHelper.PiOver2;
                if (a1 > MathHelper.TwoPi)
                {
                    a1 -= MathHelper.TwoPi;
                }
                if (a2 > MathHelper.TwoPi)
                {
                    a2 -= MathHelper.TwoPi;
                }
                c = a2 - a1;
            }

            return Math.Abs(c);
        }

        /// <summary>
        /// No more fall damage from teleports!
        /// </summary>
        public static void SafeTeleport(this Player player, Vector2 destination)
        {
            player.position.X = destination.X;
            player.position.Y = destination.Y;
            player.gravDir = 1;
            player.velocity.X = 0f;
            player.velocity.Y = 0f;
            player.fallStart = (int)player.Center.Y;
        }

        /// <summary>
        /// Toggles rain. If called serverside, syncs. Cannot be called clientside
        /// </summary>
        public static void ToggleRain()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                //Clients should NEVER execute this, will cause desync
                return;
            }

            if (!Main.raining)
            {
                StartRain();
            }
            else
            {
                StopRain();
            }
        }

        public static void StopRain()
        {
            Main.raining = false;
            Main.maxRaining = 0f;
            Main.rainTime = 0;

            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.WorldData);
            }
        }

        public static void StartRain()
        {
            Main.raining = true;
            Main.rainTime = 18000;

            ChangeRain();

            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.WorldData);
            }
        }

        private static void ChangeRain()
        {
            //private, will be public in 1.4, together with StartRain (which has random duration)
            if (Main.cloudBGActive >= 1f || Main.numClouds > 150)
            {
                if (Main.rand.NextBool(3))
                {
                    Main.maxRaining = Main.rand.Next(20, 90) * 0.01f;
                }
                else
                {
                    Main.maxRaining = Main.rand.Next(40, 90) * 0.01f;
                }
            }
            else if (Main.numClouds > 100)
            {
                if (Main.rand.NextBool(3))
                {
                    Main.maxRaining = Main.rand.Next(10, 70) * 0.01f;
                }
                else
                {
                    Main.maxRaining = Main.rand.Next(20, 60) * 0.01f;
                }
            }
            else if (Main.rand.NextBool(3))
            {
                Main.maxRaining = Main.rand.Next(5, 40) * 0.01f;
            }
            else
            {
                Main.maxRaining = Main.rand.Next(5, 30) * 0.01f;
            }
        }

        /// <summary>
        /// Returns the character's position from (pos + 1) frames ago. Direct array access, beware out of bounds access
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 OldPos(this Player player, int pos)
        {
            return player.GetModPlayer<tsorcRevampPlayer>().oldPos[pos];
        }

        public static Texture2D Crop(Texture2D image, Rectangle source) {
            Texture2D croppedImage = new Texture2D(image.GraphicsDevice, source.Width, source.Height);

            Color[] imageData = new Color[image.Width * image.Height];
            Color[] cropData = new Color[source.Width * source.Height];

            image.GetData<Color>(imageData);

            int index = 0;

            for (int y = source.Y; y < source.Height; y++) {
                for (int x = source.X; x < source.Width; x++) {
                    cropData[index] = imageData[y * image.Width + x];
                    index++;
                }
            }
            croppedImage.SetData<Color>(cropData);
            return croppedImage;
        }

    }
}