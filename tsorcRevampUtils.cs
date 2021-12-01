using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp {

    //using static tsorcRevamp.SpawnHelper;
    public static class SpawnHelper {

        //undergroundJungle, undergroundEvil, and undergroundHoly are deliberately missing. call Cavern && p.zone instead.

        public static bool Cavern(Player p) { //if youre calling Cavern without a p.zone check, also call NoSpecialBiome
            return p.position.Y >= Main.rockLayer && p.position.Y <= Main.rockLayer * 25;
        }

        public static bool NoSpecialBiome(Player p) {
            return (!p.ZoneJungle && !p.ZoneCorrupt && !p.ZoneCrimson && !p.ZoneHoly && !p.ZoneMeteor && !p.ZoneDungeon);
        }

        public static bool Sky(Player p) { //p.ZoneSkyHeight is more restrictive than this, so use this if an enemy uses it
            return p.position.Y < Main.worldSurface * 0.44999998807907104;
        }

        public static bool Surface(Player p) {
            return !Sky(p) && (p.position.Y < Main.worldSurface); //dont need to check nospecialbiome here since we're already calling Sky
        }

        public static bool Underground(Player p) {
            return Main.worldSurface > p.position.Y && p.position.Y < Main.rockLayer;
        } 

        public static bool Underworld(Player p) {
            int playerYTile = (int)(p.Bottom.Y + 8f) / 16;
            return playerYTile > Main.maxTilesY - 190;
        }
    }

    //using static tsorcRevamp.oSpawnHelper;
    public static class oSpawnHelper { 

        public static bool oCavern(Player p) {
            return (p.position.Y >= (Main.rockLayer * 17)) && (p.position.Y < (Main.rockLayer * 24));
        }

        public static bool oCavernByTile(Player p) {
            int playerYTile = (int)(p.Bottom.Y + 8f) / 16;
            return playerYTile >= (Main.maxTilesY * 0.4f) && playerYTile < (Main.maxTilesY * 0.6f);
        }

        public static bool oMagmaCavern(Player p) {
            return (p.position.Y >= (Main.rockLayer * 24)) && (p.position.Y < (Main.rockLayer * 32));
        }

        public static bool oMagmaCavernByTile(Player p) {
            int playerYTile = (int)(p.Bottom.Y + 8f) / 16;
            return playerYTile >= (Main.maxTilesY * 0.6f) && playerYTile < (Main.maxTilesY * 0.8f);
        }

        public static bool oSky(Player p) {
            return p.position.Y <= Main.rockLayer * 4;
        }

        public static bool oSkyByTile(Player p) {
            int playerYTile = (int)(p.Bottom.Y + 8f) / 16;
            return playerYTile < (Main.maxTilesY * 0.1f);
        }

        public static bool oSurface(Player p) {
            return !p.ZoneSkyHeight && (p.position.Y <= Main.worldSurface);
        }

        public static bool oUnderground(Player p) {
            return (p.position.Y >= (Main.rockLayer * 13)) && (p.position.Y < (Main.rockLayer * 17));
        }

        public static bool oUndergroundByTile(Player p) {
            int playerYTile = (int)(p.Bottom.Y + 8f) / 16;
            return playerYTile >= (Main.maxTilesY * 0.3f) && playerYTile < (Main.maxTilesY * 0.4f);
        }

        public static bool oUnderSurface(Player p) {
            return (p.position.Y > (Main.rockLayer * 8)) && (p.position.Y < (Main.rockLayer * 13));
        }

        public static bool oUnderSurfaceByTile(Player p) {
            int playerYTile = (int)(p.Bottom.Y + 8f) / 16;
            return (playerYTile >= (Main.maxTilesY * 0.2f) && playerYTile < (Main.maxTilesY * 0.3f));
        }

        public static bool oUnderworld(Player p) {
            return p.position.Y >= (Main.rockLayer * 32);
        }

        public static bool oUnderworldByHeight(Player p) {
            int playerYTile = (int)(p.Bottom.Y + 8f) / 16;
            return (playerYTile >= (Main.maxTilesY * 0.8f));
        }
    }

    public static class VariousConstants {
        public const int CUSTOM_MAP_WORLD_ID = 44874972;
        public const string MUSIC_MOD_URL = "https://github.com/timhjersted/tsorcDownload/raw/main/tsorcMusic.tmod";
        public const string MAP_URL = "https://github.com/timhjersted/tsorcDownload/raw/main/the-story-of-red-cloud.wld";
    }

    public static class PriceByRarity { 
        
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
        
        //Returns a vector pointing from a source point to a target point, at a speed.
        //Intended for projectiles. Just give it the info, and it will give you a velocity vector for that projectile to fire it at that speed.
        //Can be used for non-projectile things too though, such as making a weapon point at a target point, or an NPC dash toward a certain point.
        //No more need to rewrite the exact same vector math 100000 times
        public static Vector2 GenerateTargetingVector(Vector2 source, Vector2 target, float speed)
        {
            Vector2 diff = target - source;
            float angle = diff.ToRotation();
            Vector2 velocity = new Vector2(speed, 0);
            velocity = velocity.RotatedBy(angle);
            return velocity;
        }

        //Draws the projectile, but fully lit so that it's actually visible in the dark. Even works for animated ones! Isn't technology wonderful
        //Just stick it in predraw, follow it up with "return false;", and let it do its thing
        //If you're using a custom texture, feel free to specify it. If not, it will use the projectile's default texture.
        public static void DrawSimpleLitProjectile(SpriteBatch spriteBatch, Projectile projectile, Texture2D texture = null)
        {
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (projectile.spriteDirection == -1)
            {
                spriteEffects = SpriteEffects.FlipHorizontally;
            }

            if (texture == null)
            {
                texture = ModContent.GetTexture(projectile.modProjectile.Texture);
            }

            int frameHeight = Main.projectileTexture[projectile.type].Height / Main.projFrames[projectile.type];
            int startY = frameHeight * projectile.frame;
            Rectangle sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);
            Vector2 origin = sourceRectangle.Size() / 2f;
            Main.spriteBatch.Draw(texture,
                projectile.Center - Main.screenPosition + new Vector2(0f, projectile.gfxOffY),
                sourceRectangle, Color.White, projectile.rotation, origin, projectile.scale, spriteEffects, 0f);
        }    
        
        //Generates a ring of dust with various properties. Simple enough.
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

        //Broadcasts a message, because the line to do so is a bit long
        //Runs a NewText in singleplayer so that this can be used as a general "just display a line of text exactly once to everyone"
        public static void ServerText(string text)
        {
            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.BroadcastChatMessage(NetworkText.FromLiteral(text), Color.Yellow);
            }
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                Main.NewText(text);
            }
        }

        //Yes, this has to be seperate. Can't use optional parameters because for some dumb reason XNA colors aren't a compile-time constant.
        public static void ServerText(string text, Color color)
        {
            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.BroadcastChatMessage(NetworkText.FromLiteral(text), color);
            }
            if(Main.netMode == NetmodeID.SinglePlayer)
            {
                Main.NewText(text, color);
            }
        }

        //Gets the first npc of a given type. Basically NPC.AnyNPC, except it actually returns what it finds.
        //Uses nullable ints, aka "int?". Will return null if it can't find one.
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
        ///Call in a projectile's AI to allow the projectile to home on enemies
        ///</summary>         
        ///<param name="projectile">The current projectile</param>
        ///<param name="homingRadius">The homing radius</param>
        ///<param name="topSpeed">The projectile's maximum velocity</param>
        ///<param name="rotateTowards">Should the projectile maintain topSpeed speed and rotate towards targets, instead of standard homing?</param>
        ///<param name="homingStrength">The homing strength coefficient. Unused if rotateTowards.</param>
        ///<param name="needsLineOfSight">Does the projectile need line of sight to home on a target?</param>
        public static void HomeOnEnemy(Projectile projectile, float homingRadius, float topSpeed, bool rotateTowards = false, float homingStrength = 1f, bool needsLineOfSight = false) {
            if (!projectile.active || !projectile.friendly) return;
            const int BASE_STRENGTH = 30;

            Vector2 targetLocation = Vector2.UnitY;
            bool foundTarget = false;

            for (int i = 0; i < 200; i++) {
                if (!Main.npc[i].active) return;
                float toNPCEdge = (Main.npc[i].width / 2) + (Main.npc[i].height / 2); //make homing on larger targets more consistent

                //WithinRange is just faster Distance (skips sqrt)
                if (Main.npc[i].CanBeChasedBy(projectile) && projectile.WithinRange(Main.npc[i].Center, homingRadius + toNPCEdge) && (!needsLineOfSight || Collision.CanHitLine(projectile.Center, 1, 1, Main.npc[i].Center, 1, 1))) {
                    targetLocation = Main.npc[i].Center;
                    foundTarget = true;
                    break;
                }
            }

            if (foundTarget) {
                Vector2 homingDirection = Vector2.Normalize(targetLocation - projectile.Center);
                projectile.velocity = (projectile.velocity * (BASE_STRENGTH / homingStrength) + homingDirection * topSpeed) / ((BASE_STRENGTH / homingStrength) + 1);
            }
            if (rotateTowards) {
                if (projectile.velocity.Length() < topSpeed) {
                    projectile.velocity *= topSpeed / projectile.velocity.Length();
                }
            }
            if (projectile.velocity.Length() > topSpeed) {
                projectile.velocity *= topSpeed / projectile.velocity.Length();
            }
        }


        /**INCOMPLETE!!!
         
        //TODO:
        //1) Maybe restructure this. Could maybe sync a long queue of randomly generated numbers instead? 
        //Being able to check to validate the queues are all in sync *long* before any given value is used would make it far less vulnerable to desyncs
        //But it would introduce whole new issues if the queue ran out, as well as take more network traffic to validate a whole queue instead of just a seed and tally...
        //2) Add a check every few frames to ensure they're still all in sync, and figure out what is causing occasional desyncs (client tick lag?)
        //3) Add a check so that if this is called in code that *only* runs on the client (like PreDraw or item Shoot) it doesn't desync
        //Instead it must return the normal Main.rand. The client calling SyncedRand when the server does not will throw it out of sync.

        //SyncedRand is a random number generator which is *already* synced.
        //This existed in tConfig, but TML dropped it. It's *extremely* useful and makes worrying about MP desyncs a non-issue in many cases

        /**Here's how it works: In tsorcRevampPlayer.SyncPlayer, upon anyone joining the server GenerateRandomSeed is called.
        //The server picks a random seed, and sends it to all players.
        //Whenever the function is called, the server and every client all roll a random value based off the same seed

        

        //The random generator. Initiated with a seed.
        static Terraria.Utilities.UnifiedRandom SyncedRand;
        
        //Used to initiate the random number generator. Synced across all devices.
        //Seed is normally an int, it's a byte here because packets use bytes and we don't need it to be secure.
        public static byte seed;

        //Increases every time the random number generator is used. Compared across devices to make sure none fall out of sync.
        public static int tally = 0;

        //The main important function here. Returns a synced rand, and also adds a tally while it does so. If single player, we don't need to sync anything and can just default to the normal Main.rand.
        public static Terraria.Utilities.UnifiedRandom GetSyncedRand()
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                return Main.rand;
            }
            else
            {
                tally++;
                return SyncedRand;
            }
        }        

        //Called on the server, generates a seed and sends it to clients
        public static void GenerateRandomSeed()
        {
            //Reset tally
            tally = 0;
            //Generate seed and start up a new random based on it
            seed = (byte)Main.rand.Next();
            SyncedRand = new Terraria.Utilities.UnifiedRandom(seed);
            
            NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("Server sending seed: " + seed), Color.Blue);

            //Create a new packet to send the seed to clients
            ModPacket randPacket = ModContent.GetInstance<tsorcRevamp>().GetPacket();
            randPacket.Write((byte)2);
            randPacket.Write(seed);
            randPacket.Send();            
        }

        //Called by the client in HandlePacket once it recieves the seed and current tally count, and instantiates the SyncedRand with it.
        public static void RecieveRandPacket(byte recievedSeed, int tallyCount)
        {   
            seed = recievedSeed;
            tally = 0;
            Main.NewText("Client seed: " + seed);
            SyncedRand = new Terraria.Utilities.UnifiedRandom(seed);
        }
        ***/

    }

}