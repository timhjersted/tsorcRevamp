using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent.ItemDropRules;
using Terraria.DataStructures;
using tsorcRevamp.Items.Weapons.Throwing;

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
            NPC.damage = 65;
            NPC.defense = 50;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.lifeMax = 1000;
            NPC.alpha = 20;
            NPC.scale = .7f;
            NPC.knockBackResist = 0.1f;
            NPC.noGravity = true;
            NPC.value = 2000; // life / 2.5 / 2 bc it's so simple : was 125
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.ManOfWarBanner>();
            if (Main.hardMode)
            {
                NPC.lifeMax = 250;
                NPC.defense = 30;
                NPC.value = 620; // was 55
                NPC.knockBackResist = 0.3f;
            }
        }
        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<OilPot>(), 1, 1, 5));
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
                chance = 0.25f;
            }
            if (Main.hardMode && spawnInfo.Player.ZoneDungeon && spawnInfo.Water)
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
            target.AddBuff(BuffID.Electrified, 5 * 60);

            if (Main.rand.NextBool(2))
            {
                target.AddBuff(BuffID.PotionSickness, 30 * 60); //evil! pure evil! (yeah true, don't know who did this but it's 30 now instead of 60)              
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
