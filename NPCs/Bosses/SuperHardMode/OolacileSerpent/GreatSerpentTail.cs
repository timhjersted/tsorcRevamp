using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode.OolacileSerpent
{
    class GreatSerpentTail : ModNPC
    {
        public override void SetStaticDefaults()
        {
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers()
            {
                Hide = true
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(NPC.type, value);
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire3] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Daybreak] = true;
        }
        public override void SetDefaults()
        {
            NPC.netAlways = true;
            NPC.npcSlots = 2;
            //Tight-cropped sprite (GreatSerpentTail.png is actually 10x98 -- long and narrow; these dims were
            //stale copy-paste from Body2's 22x44 and didn't match the real art at all, which is very likely why
            //the tail read as bent/misaligned/wrong-facing: physics packed it at a ~44px link length while the
            //ACTUAL drawn sprite was more than double that, so the visible tail always overshot well past where
            //the chain math thought it ended). Height is the along-chain link length.
            NPC.width = 10;
            NPC.height = 98;
            DrawOffsetY = 0;
            NPC.aiStyle = -1; // fully custom AI (SerpentAI); -1 stops vanilla worm AI + its dig sound
            NPC.knockBackResist = 0;
            NPC.scale = 1f;
            NPC.timeLeft = 22750;
            NPC.damage = 0; //0 except during the overhead tail stab, where SerpentAI sets it (see TailStab)
            NPC.defense = 40;
            NPC.HitSound = SoundID.NPCHit13;
            NPC.DeathSound = SoundID.NPCDeath8;
            NPC.lifeMax = 20000;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.behindTiles = true;
            NPC.value = 0;
        }
        public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
        {
            return false;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            //The tail stab injects venom (matches the pounce's toxin theme)
            target.AddBuff(BuffID.Venom, 8 * 60, false);
        }
        public override void AI()
        {
            int[] bodyTypes = SerpentAI.BuildBodyTypes();
            SerpentAI.Run(NPC, ModContent.NPCType<GreatSerpentHead>(), bodyTypes, ModContent.NPCType<GreatSerpentTail>(), GreatSerpentHead.TotalSegmentCount, 18f);

            if (!Main.npc[(int)NPC.ai[1]].active)
            {
                NPC.life = 0;
                NPC.HitEffect(0, 10.0);
                NPC.active = false;
            }
        }

        public override bool CheckActive()
        {
            return false;
        }
    }
}
