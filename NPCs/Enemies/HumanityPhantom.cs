using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.NPCs.Enemies
{
    public class HumanityPhantom : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 8;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Venom] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire3] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.ShadowFlame] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.CursedInferno] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Ichor] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][ModContent.BuffType<DarkInferno>()] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][ModContent.BuffType<CrimsonBurn>()] = true;
        }

        public override void SetDefaults()
        {
            NPC.width = 20;
            NPC.height = 46;
            NPC.aiStyle = -1; //Unique AI
            NPC.damage = 60;
            NPC.knockBackResist = 0;
            NPC.defense = 8;
            NPC.scale = Main.rand.NextFloat(0.5f, 1f);
            if (!Main.hardMode) NPC.lifeMax = (int)(150 * NPC.scale);
            else NPC.lifeMax = (int)(500 * NPC.scale);
            if (tsorcRevampWorld.SuperHardMode) NPC.lifeMax *= 5;
            NPC.value = 1000;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.HitSound = SoundID.NPCHit1;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.HumanityPhantomBanner>();
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo) //Spawns in extremely deep, dark places.
        {
            float chance = 0;

            if (spawnInfo.SpawnTileY >= Main.maxTilesY - 400)
            {
                if (spawnInfo.Player.ZoneRockLayerHeight && Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].WallType == WallID.SpiderUnsafe) //This is at the very bottom of the chasm. Accessible pre-HM. Still difficult to encounter them as the area isn't really big enough to allow them to spawn offscreen
                {
                    chance = 2f;
                }

                if ((spawnInfo.Player.ZoneRockLayerHeight || spawnInfo.Player.ZoneUnderworldHeight) && (Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].WallType == WallID.ObsidianBrickUnsafe || Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].WallType == WallID.TitanstoneBlock)) //Gwyns tomb entrance, a SHM cave under the krakens arena, and the caves leading up to the Witchking
                {
                    chance = 1.5f;
                }

                if (Math.Abs(spawnInfo.SpawnTileX - Main.spawnTileX) < Main.maxTilesX / 3 && Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].WallType == WallID.StarlitHeavenWallpaper) //Inner third of the map, abyss wall. This is the heart of the abyss, SHM
                {
                    chance = 10f;
                }
            }
            return chance;
        }

        public override void AI()
        {
            Player player = Main.LocalPlayer;
            NPC.TargetClosest(true);

            /*Vector2 difference = Main.player[npc.target].Center - npc.Center; //Distance between player center and npc center
			Vector2 velocity = new Vector2(0.5f, 0.5f).RotatedBy(difference.ToRotation()); //Give it velocity so it can face the right direction

			if (Main.player[npc.target].Distance(npc.Center) < 500f)
			{
				npc.velocity = velocity;
			}
			else npc.velocity = new Vector2(0, 0);*/

            Vector2 targetPosition = Main.player[NPC.target].position; // get a local copy of the targeted player's position
            Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));

            if (Main.player[NPC.target].Distance(NPC.Center) < 500f)
            {
                if (Main.player[NPC.target].position.X < vector8.X)
                {
                    if (NPC.velocity.X > -7) { NPC.velocity.X = -0.5f; }
                }
                if (Main.player[NPC.target].position.X > vector8.X)
                {
                    if (NPC.velocity.X < 7) { NPC.velocity.X = 0.5f; }
                }

                if (Main.player[NPC.target].position.Y < vector8.Y)
                {
                    if (NPC.velocity.Y > 0f) NPC.velocity.Y = -0.5f;
                    else NPC.velocity.Y = -0.5f;
                }
                if (Main.player[NPC.target].position.Y > vector8.Y)
                {
                    if (NPC.velocity.Y < 0f) NPC.velocity.Y = 0.5f;
                    else NPC.velocity.Y = 0.5f;
                }
            }
            else
            {
                NPC.velocity = Vector2.Zero;
            }
        }


        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            //"0.5f scale phantoms have 20% chance of dropping, scaling up towards 1f scale phantoms dropping humanity 70% of the time"
            //haha fuck that, 45% flat it is
            npcLoot.Add(new CommonDrop(ModContent.ItemType<Items.Humanity>(), 100, 1, 1, 45));
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.HumanityPhantom];

            spriteBatch.Draw(texture, NPC.Center - Main.screenPosition, new Rectangle(0, NPC.frame.Y, 48, 68), Color.White, NPC.rotation, new Vector2(24, 34), NPC.scale, SpriteEffects.None, 0);

            return false;
        }

        public override void FindFrame(int frameHeight)
        {
            NPC.frameCounter++;

            if (NPC.frameCounter < 12)
            {
                NPC.frame.Y = 0 * frameHeight;
            }
            else if (NPC.frameCounter < 24)
            {
                NPC.frame.Y = 1 * frameHeight;
            }
            else if (NPC.frameCounter < 36)
            {
                NPC.frame.Y = 2 * frameHeight;
            }
            else if (NPC.frameCounter < 48)
            {
                NPC.frame.Y = 3 * frameHeight;
            }
            else if (NPC.frameCounter < 60)
            {
                NPC.frame.Y = 4 * frameHeight;
            }
            else if (NPC.frameCounter < 72)
            {
                NPC.frame.Y = 5 * frameHeight;
            }
            else if (NPC.frameCounter < 84)
            {
                NPC.frame.Y = 6 * frameHeight;
            }
            else if (NPC.frameCounter < 96)
            {
                NPC.frame.Y = 7 * frameHeight;
            }
            else
            {
                NPC.frameCounter = 0;
            }
        }
    }
}
