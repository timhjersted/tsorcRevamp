using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
    class ArmoredWraith : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = Main.npcFrameCount[NPCID.Wraith];
            NPCDebuffImmunityData debuffData = new NPCDebuffImmunityData
            {
                SpecificallyImmuneTo = new int[] 
                {
                    BuffID.Poisoned,
                    BuffID.OnFire,
                    BuffID.Confused
                }
            };
            NPCID.Sets.DebuffImmunitySets.Add(Type, debuffData);
        }

        public override void SetDefaults()
        {
            NPC.CloneDefaults(NPCID.Wraith);
            AnimationType = NPCID.Wraith;
            AIType = NPCID.CaveBat;
            NPC.lifeMax = 75;
            NPC.damage = 48;
            NPC.scale = 1f;
            NPC.knockBackResist = 0;
            NPC.value = 370; // life / 2 : was 50 
            NPC.defense = 25;
            NPC.alpha = 100;
            NPC.lavaImmune = true;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.ArmoredWraithBanner>();
        }
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            float chance = 0;
            if (spawnInfo.Player.ZoneMeteor && !Main.dayTime && spawnInfo.Player.ZoneOverworldHeight)
            {
                chance = .04f;
            }
            if (spawnInfo.Player.ZoneMeteor && !Main.dayTime && spawnInfo.Player.ZoneDirtLayerHeight)
            {
                chance = .033f;
            }
            if (spawnInfo.Player.ZoneMeteor && !Main.dayTime && spawnInfo.Player.ZoneRockLayerHeight)
            {
                chance = .04f;
            }

            return chance;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            if (Main.rand.NextBool(5))
            {
                target.AddBuff(BuffID.BrokenArmor, 180 * 60, true);
            }
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            for (int i = 0; i < 25; i++)
            {
                int DustType = 109;
                int dustIndex = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustType);
                Dust dust = Main.dust[dustIndex];
                dust.velocity.X += Main.rand.Next(-50, 51) * 0.04f;
                dust.velocity.Y += Main.rand.Next(-50, 51) * 0.04f;
                dust.scale *= .8f + Main.rand.Next(-30, 31) * 0.01f;
                dust.noGravity = false;
                dust.alpha = 120;
            }
            if (NPC.life <= 0)
            {
                for (int i = 0; i < 40; i++) // I'm not convinced this is actually working
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, 109, Main.rand.Next(-10, 10), Main.rand.Next(-10, 10), 120, default(Color), .8f);
                }
            }
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            IItemDropRule hmCondition = new LeadingConditionRule(new Conditions.IsHardmode());
            hmCondition.OnSuccess(ItemDropRule.Common(ItemID.SoulofFlight));
            npcLoot.Add(hmCondition);
        }
}
}