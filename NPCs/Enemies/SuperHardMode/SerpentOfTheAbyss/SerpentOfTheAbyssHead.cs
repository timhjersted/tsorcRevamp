using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items;
using tsorcRevamp.Projectiles.Enemy;
using tsorcRevamp.Buffs.Debuffs;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using tsorcRevamp.Items.Accessories.Defensive;
using tsorcRevamp.Items.Accessories;
using tsorcRevamp.Items.Potions;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode.SerpentOfTheAbyss
{
    class SerpentOfTheAbyssHead : ModNPC
    {

        int breathCD = 110;
        bool breath = false;
        public override void SetStaticDefaults()
        {
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire3] = true;
        }
        public override void SetDefaults()
        {
            NPC.netAlways = true;
            NPC.npcSlots = 3;
            NPC.width = 42;
            NPC.height = 81;
            NPC.aiStyle = 6;
            NPC.defense = 30;
            NPC.timeLeft = 22500;
            NPC.damage = 85;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath5;
            NPC.lifeMax = 8000; // was 10k
            NPC.knockBackResist = 0;
            NPC.lavaImmune = true;
            NPC.scale = 1;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.behindTiles = true;
            NPC.value = 26660; // life / 3 due to multi-hit : was 2550
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.SerpentOfTheAbyssBanner>();

            bodyTypes = new int[33];
            int bodyID = ModContent.NPCType<SerpentOfTheAbyssBody>();
            for (int i = 0; i < 33; i++)
            {
                bodyTypes[i] = bodyID;
            }
        }

        int cursedBreathDamage = 35;
        int poisonFlamesDamage = 39;
        int dragonMeteorDamage = 41;

        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)/* tModPorter Note: bossLifeScale -> balance (bossAdjustment is different, see the docs for details) */
        {
            cursedBreathDamage = (int)(cursedBreathDamage * tsorcRevampWorld.SHMScale);
            poisonFlamesDamage = (int)(poisonFlamesDamage * tsorcRevampWorld.SHMScale);
            dragonMeteorDamage = (int)(dragonMeteorDamage * tsorcRevampWorld.SHMScale);
        }
        int[] bodyTypes;

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player p = spawnInfo.Player;
            Point pTile = p.Center.ToTileCoordinates();
            bool worldEdge = (pTile.X < Main.maxTilesX * 0.3f) || (pTile.X > Main.maxTilesX * 0.7f); //thanks i hate it

            //Ensuring it can't spawn if one already exists.
            int count = 0;
            for (int i = 0; i < Main.npc.Length; i++)
            {
                if (Main.npc[i].type == NPC.type)
                {
                    count++;
                    if (count > 0)
                    {
                        return 0;
                    }
                }
            }

            if (tsorcRevampWorld.SuperHardMode)
            {
                if (p.ZoneUnderworldHeight)
                {
                    if (worldEdge)
                    {
                        if (Main.bloodMoon) { return 0.2f; } //blood moon, underworld, edge
                        else return 0.067f; //not blood moon, underworld, edge
                    }
                    else if (Main.bloodMoon) { return 0.067f; } //blood moon, underworld
                    else return 0.02f; //underworld
                }
            }
            return 0; //outside shm
        }

        public override void AI()
        {

            tsorcRevampGlobalNPC.AIWorm(NPC, ModContent.NPCType<SerpentOfTheAbyssHead>(), bodyTypes, ModContent.NPCType<SerpentOfTheAbyssTail>(), 35, .8f, 17, 0.25f, false, false, false, true, true);


            Player nT = Main.player[NPC.target];
            //190 was 90
            if (Main.rand.NextBool(190))
            {
                breath = true;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20);
                NPC.netUpdate = true;
            }


            if (breath)
            {

                float rotation = (float)Math.Atan2(NPC.Center.Y - Main.player[NPC.target].Center.Y, NPC.Center.X - Main.player[NPC.target].Center.X);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y + (20f * NPC.direction), NPC.velocity.X * 3f + (float)Main.rand.Next(-2, 3), NPC.velocity.Y * 3f + (float)Main.rand.Next(-2, 3), ModContent.ProjectileType<CursedDragonsBreath>(), cursedBreathDamage, 0f, Main.myPlayer); //cursed dragons breath
                    Main.projectile[num54].timeLeft = 50;
                }

                breathCD--;

            }
            if (breathCD <= 0)
            {
                breath = false;
                breathCD = 120;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20);
            }
            if (Main.rand.NextBool(940))
            {
                for (int pcy = 0; pcy < 10; pcy++)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)nT.position.X - 100 + Main.rand.Next(200), (float)nT.position.Y - 400f, (float)(-80 + Main.rand.Next(160)) / 10, 10.9f, ModContent.ProjectileType<PoisonFlames>(), poisonFlamesDamage, 2f, Main.myPlayer); //9.9f was 14.9f
                    }
                }
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20);
            }
            if (Main.rand.NextBool(2760))
            {
                for (int pcy = 0; pcy < 10; pcy++)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)nT.position.X - 100 + Main.rand.Next(1600), (float)nT.position.Y - 300f, (float)(-40 + Main.rand.Next(80)) / 10, 9.5f, ModContent.ProjectileType<DragonMeteor>(), dragonMeteorDamage, 2f, Main.myPlayer); //dragon meteor
                    }
                }
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20);
            }
            if (Main.rand.NextBool(60))
            {
                int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, 6, NPC.velocity.X / 4f, NPC.velocity.Y / 4f, 100, default(Color), 1f);
                Main.dust[d].noGravity = true;
            }
        }


        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Humanity>(), 1, 3, 6));
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life <= 0)
            {
                if (!Main.dedServ)
                {
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Lich King Serpent Head Gore").Type);
                }
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            if (Main.rand.NextBool(2))
            {
                target.AddBuff(BuffID.CursedInferno, 10 * 60);
                target.AddBuff(ModContent.BuffType<SlowedLifeRegen>(), 20 * 60);
            }
        }
    }
}
