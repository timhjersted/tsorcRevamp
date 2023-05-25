using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent.ItemDropRules;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
    class CorruptedElemental : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 15;
            NPCDebuffImmunityData debuffData = new NPCDebuffImmunityData
            {
                SpecificallyImmuneTo = new int[]
                {
                    BuffID.OnFire,
                    BuffID.Poisoned,
                    BuffID.Venom,
                    BuffID.CursedInferno,
                    BuffID.Ichor
                }
            };
            NPCID.Sets.DebuffImmunitySets.Add(Type, debuffData);
        }
        public override void SetDefaults()
        {
            NPC.npcSlots = 1;
            NPC.width = 18;
            NPC.height = 40;
            AnimationType = 120;
            NPC.knockBackResist = 0.1f;

            NPC.aiStyle = 3;
            NPC.timeLeft = 750;
            NPC.damage = 50;
            NPC.defense = 32;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.lifeMax = 2600;
            NPC.value = 1300;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.CorruptedElementalBanner>();
        }

        //Spawns in the Underground and Cavern before 3.5/10ths and after 7.5/10ths (Width). Does not Spawn in the Jungle, Meteor, or if there are Town NPCs.


        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            if (tsorcRevampWorld.SuperHardMode)
            {
                if ((spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson) && (spawnInfo.Player.position.Y / 16) < Main.rockLayer && (spawnInfo.Player.position.Y / 16) < Main.maxTilesY - 200 && !spawnInfo.Player.ZoneDungeon)
                {
                    return 0.5f;
                }
                else return 0;
            }
            else return 0;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            target.AddBuff(BuffID.Battle, 60 * 60, false); //battle
            target.AddBuff(BuffID.Weak, 60 * 60, false); //weak
        }

        public override void AI()
        {
            tsorcRevampAIs.FighterAI(NPC, 2.8f, 0.08f, canTeleport: true, enragePercent: 0.2f, enrageTopSpeed: 3.6f);
            tsorcRevampAIs.LeapAtPlayer(NPC, 6, 5, 2, 128);

        }

        public override void OnKill()
        {
            Dust.NewDust(NPC.position, NPC.width, NPC.height, 4, 0.3f, 0.3f, 200, default(Color), 1f);
            Dust.NewDust(NPC.position, NPC.height, NPC.width, 4, 0.2f, 0.2f, 200, default(Color), 2f);
            Dust.NewDust(NPC.position, NPC.width, NPC.height, 4, 0.2f, 0.2f, 200, default(Color), 2f);
            Dust.NewDust(NPC.position, NPC.height, NPC.width, 4, 0.2f, 0.2f, 200, default(Color), 3f);
            Dust.NewDust(NPC.position, NPC.height, NPC.width, 4, 0.2f, 0.2f, 200, default(Color), 2f);
            Dust.NewDust(NPC.position, NPC.width, NPC.height, 4, 0.2f, 0.2f, 200, default(Color), 2f);
            Dust.NewDust(NPC.position, NPC.height, NPC.width, 4, 0.2f, 0.2f, 200, default(Color), 2f);
            Dust.NewDust(NPC.position, NPC.height, NPC.width, 4, 0.2f, 0.2f, 200, default(Color), 2f);
            Dust.NewDust(NPC.position, NPC.height, NPC.width, 4, 0.2f, 0.2f, 200, default(Color), 2f);
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot) 
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.WhiteTitanite>(), 1, 1, 3));
        }
    }
}