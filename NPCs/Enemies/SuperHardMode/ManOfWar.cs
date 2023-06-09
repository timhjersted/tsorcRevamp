using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent.ItemDropRules;
using Terraria.DataStructures;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
    class ManOfWar : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 7;
            NPCDebuffImmunityData debuffData = new NPCDebuffImmunityData
            {
                SpecificallyImmuneTo = new int[]
                {
                    BuffID.Confused,
                    BuffID.Frozen
                }
            };
            NPCID.Sets.DebuffImmunitySets.Add(Type, debuffData);
        }
        public override void SetDefaults()
        {
            NPC.width = 26;
            NPC.height = 26;
            AnimationType = NPCID.GreenJellyfish;
            NPC.aiStyle = 18;
            NPC.timeLeft = 750;
            NPC.damage = 60;
            NPC.defense = 40;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.lifeMax = 1000;
            NPC.alpha = 20;
            NPC.scale = .7f;
            NPC.knockBackResist = 0.3f;
            NPC.noGravity = true;
            NPC.value = 1250;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.ManOfWarBanner>();
            if (Main.hardMode)
            {
                NPC.lifeMax = 250;
                NPC.defense = 30;
                NPC.value = 550;
            }
        }

        public override void AI()
        {
            DrawOffsetY = 20;
            if (Main.GameUpdateCount % 60 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Player closestPlayer = UsefulFunctions.GetClosestPlayer(NPC.Center);
                if (closestPlayer != null && Collision.CanHit(NPC, closestPlayer)) {
                    Vector2 targetVector = UsefulFunctions.Aim(NPC.Center, closestPlayer.Center, 1);
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, targetVector, ModContent.ProjectileType<Projectiles.Enemy.JellyfishLightning>(), 30, 1, Main.myPlayer, 0, NPC.whoAmI);
                }
            }
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            float chance = 0;

            if (Main.hardMode && spawnInfo.Water)
            {
                chance = 0.5f;
            }
            if (Math.Abs(spawnInfo.SpawnTileX - Main.spawnTileX) > Main.maxTilesX / 3)
            {
                chance *= 4;
            }

            return chance;
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            if (Main.rand.NextBool(2))
            {
                target.AddBuff(BuffID.PotionSickness, 60 * 60); //evil! pure evil!
                target.AddBuff(BuffID.Electrified, 5 * 60);
            }
        }
        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life <= 0)
            {
                Dust.NewDust(NPC.position, NPC.width, NPC.height, 71, 0.3f, 0.3f, 200, default, 1f);
                Dust.NewDust(NPC.position, NPC.height, NPC.width, 71, 0.2f, 0.2f, 200, default, 2f);
                Dust.NewDust(NPC.position, NPC.width, NPC.height, 71, 0.2f, 0.2f, 200, default, 2f);
                Dust.NewDust(NPC.position, NPC.height, NPC.width, 71, 0.2f, 0.2f, 200, default, 3f);
                Dust.NewDust(NPC.position, NPC.height, NPC.width, 71, 0.2f, 0.2f, 200, default, 2f);
                Dust.NewDust(NPC.position, NPC.width, NPC.height, 71, 0.2f, 0.2f, 200, default, 2f);
                Dust.NewDust(NPC.position, NPC.height, NPC.width, 71, 0.2f, 0.2f, 200, default, 2f);
                Dust.NewDust(NPC.position, NPC.height, NPC.width, 71, 0.2f, 0.2f, 200, default, 2f);
                Dust.NewDust(NPC.position, NPC.height, NPC.width, 71, 0.2f, 0.2f, 200, default, 2f);
            }
        }
    }
}
