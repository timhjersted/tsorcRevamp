using Terraria;

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
    }
}