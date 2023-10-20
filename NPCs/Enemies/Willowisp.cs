using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
    public class Willowisp : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = Main.npcFrameCount[NPCID.Pixie];
            // DisplayName.SetDefault("Will'O'Wisp");
        }

        public override void SetDefaults()
        {
            NPC.noGravity = true;
            NPC.width = 20;
            NPC.height = 20;
            NPC.aiStyle = 22;
            NPC.damage = 60;
            NPC.defense = 4;
            NPC.lifeMax = 150;
            NPC.knockBackResist = 0.1f;
            NPC.value = 3000f;
            NPC.buffImmune[BuffID.Poisoned] = true;
            NPC.buffImmune[BuffID.OnFire] = true;
            NPC.buffImmune[BuffID.CursedInferno] = true;
            NPC.buffImmune[BuffID.Venom] = true;
            NPC.buffImmune[BuffID.ShadowFlame] = true;
            NPC.HitSound = SoundID.NPCHit5;
            NPC.DeathSound = SoundID.NPCDeath7;
            AIType = NPCID.Pixie;
            AnimationType = NPCID.Pixie;
            //bannerItem = mod.ItemType("WillowispBanner");
            //banner = mod.NPCType("Willowisp");
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Graveyard,
                new FlavorTextBestiaryInfoElement("A lost spirit of the dead, which attempts to lead travelers to their demise")
            });
        }

        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player P = spawnInfo.Player; //this shortens our code up from writing this line over and over.

            bool Sky = spawnInfo.SpawnTileY <= (Main.rockLayer * 4);
            bool Meteor = P.ZoneMeteor;
            bool Jungle = P.ZoneJungle;
            bool Dungeon = P.ZoneDungeon;
            bool Corruption = (P.ZoneCorrupt || P.ZoneCrimson);
            bool Hallow = P.ZoneHallow;
            bool AboveEarth = P.ZoneOverworldHeight;
            bool InBrownLayer = P.ZoneDirtLayerHeight;
            bool InGrayLayer = P.ZoneRockLayerHeight;
            bool InHell = P.ZoneUnderworldHeight;
            bool FrozenOcean = spawnInfo.SpawnTileX > (Main.maxTilesX - 800);
            bool Ocean = spawnInfo.SpawnTileX < 800;

            // these are all the regular stuff you get , now lets see......
            if (spawnInfo.Player.townNPCs > 0f) return 0;

            if (Main.hardMode && !Meteor && !Jungle && !Dungeon && !Corruption && Hallow && Main.rand.NextBool(85)) return 1;

            if (Main.hardMode && !Meteor && !Jungle && !Dungeon && !Corruption && Hallow && InBrownLayer && Main.rand.NextBool(55)) return 1;

            if (Main.hardMode && !Meteor && !Jungle && !Dungeon && !Corruption && Hallow && InGrayLayer && Main.rand.NextBool(30)) return 1;

            if (Main.hardMode && Ocean && Main.rand.NextBool(15)) return 1;


            return 0;
        }
        #endregion

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {

        }
        Vector2 initialVelocity;
        public override void AI()
        {


            //AI needs improvement, currently bounces into terrain constantly and doesn't look like a floating ghost, would be great if I knew how to have it detect terrain and slow down and adjust direction before hitting it

            //For now I'll just make it phase through walls if can't reach the player, + 100 / + 200 works great! but it goes into walls too easily (+10 and +100 is better, but could be tweaked further)
            if ((Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height + 100)))
            {
                NPC.noTileCollide = false;
                NPC.noGravity = true;
            }
            if ((!Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height + 200)))
            {
                NPC.noTileCollide = true;
            }


            int dust = Dust.NewDust(NPC.position, NPC.width * 2, NPC.height * 2, DustID.MagicMirror, 0.0f, 0.0f, 200);
            Main.dust[dust].velocity *= 0.3f;

            Lighting.AddLight(NPC.position, 0.82f, 0.99f, 10f);
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            for (int i = 0; i < 15; i++)
            {
                int dustIndex = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.MagicMirror, hit.HitDirection, -1f);
                Dust dust = Main.dust[dustIndex];
                dust.velocity.X = dust.velocity.X + Main.rand.Next(-50, 51) * 0.01f;
                dust.velocity.Y = dust.velocity.Y + Main.rand.Next(-50, 51) * 0.01f;
                dust.scale *= 1f + Main.rand.Next(-30, 31) * 0.01f;
            }
            if (NPC.life <= 0)
            {
                for (int i = 0; i < 100; i++)
                {
                    int dustIndex = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.MagicMirror, hit.HitDirection, -1f);
                    Dust dust = Main.dust[dustIndex];
                    dust.velocity.X = dust.velocity.X + Main.rand.Next(-50, 51) * 0.01f;
                    dust.velocity.Y = dust.velocity.Y + Main.rand.Next(-50, 51) * 0.01f;
                    dust.scale *= 1f + Main.rand.Next(-30, 31) * 0.01f;
                }
            }
        }
    }
}
