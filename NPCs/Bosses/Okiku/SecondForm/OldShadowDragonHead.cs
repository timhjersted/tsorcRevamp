using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses.Okiku.SecondForm
{
    public class OldShadowDragonHead : ModNPC
    {
        //private bool initiate;

        public int TimerHeal;

        public float TimerAnim;

        public override void SetStaticDefaults()
        {
            NPCID.Sets.NPCBestiaryDrawModifiers hiddenFromBestiary = new NPCID.Sets.NPCBestiaryDrawModifiers();
            hiddenFromBestiary.Hide = true;
            NPCID.Sets.NPCBestiaryDrawOffset.Add(NPC.type, hiddenFromBestiary);
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
        }
        public override void SetDefaults()
        {
            NPC.width = 42;
            NPC.height = 42;
            NPC.aiStyle = 6;
            NPC.damage = 160;
            NPC.defense = 20;
            NPC.boss = true;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.lifeMax = 126000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0f;
            NPC.value = 0;
            despawnHandler = new NPCDespawnHandler(DustID.PurpleCrystalShard);

            DrawOffsetY = 45;

            bodyTypes = new int[] {
            ModContent.NPCType<OldShadowDragonBody>(), ModContent.NPCType<OldShadowDragonBody>(), ModContent.NPCType<OldShadowDragonBody>(),
            ModContent.NPCType<OldShadowDragonBody>(), ModContent.NPCType<OldShadowDragonLegs>(), ModContent.NPCType<OldShadowDragonBody>(), ModContent.NPCType<OldShadowDragonBody>(),
            ModContent.NPCType<OldShadowDragonBody>(), ModContent.NPCType<OldShadowDragonBody>(), ModContent.NPCType<OldShadowDragonLegs>(), ModContent.NPCType<OldShadowDragonBody>(),
            ModContent.NPCType<OldShadowDragonBody>(), ModContent.NPCType<OldShadowDragonBody>(), ModContent.NPCType<OldShadowDragonBody>(), ModContent.NPCType<OldShadowDragonLegs>(),
            ModContent.NPCType<OldShadowDragonBody>(), ModContent.NPCType<OldShadowDragonBody>(), ModContent.NPCType<OldShadowDragonBody>(), ModContent.NPCType<OldShadowDragonBody>(),
            ModContent.NPCType<OldShadowDragonLegs>(), ModContent.NPCType<OldShadowDragonBody>(), ModContent.NPCType<OldShadowDragonBody2>(), ModContent.NPCType<OldShadowDragonBody3>()
            };
        }
        public static int[] bodyTypes;
        NPCDespawnHandler despawnHandler;


        public override void AI()
        {
            despawnHandler.TargetAndDespawn(NPC.whoAmI);
            tsorcRevampGlobalNPC.AIWorm(NPC, ModContent.NPCType<OldShadowDragonHead>(), bodyTypes, ModContent.NPCType<OldShadowDragonTail>(), 25, 0f, 16f, 0.33f, true, false, true, false, false);

            if (Main.rand.NextBool(3))
            {
                int dust = Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y), NPC.width, NPC.height, 62, 0.8f, 0f, 100, Color.White, 2f);
                Main.dust[dust].noGravity = true;
            }
        }


        public override bool CheckActive()
        {
            return false;
        }

        // Comparison fight: skipping the kill transaction means no loot, potions, defeated flag or world progression.
        public override bool PreKill()
        {
            return false;
        }
    }
}
