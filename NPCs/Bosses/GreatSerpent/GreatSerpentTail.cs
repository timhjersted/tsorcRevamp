using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.NPCs.Bosses.GreatSerpent
{
    class GreatSerpentTail : ModNPC
    {
        public Vector2 PreviousStrikeCenter;
        public float PreviousStrikeRotation;
        public bool SweepReady;

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
            NPC.hide = true;
            NPC.canDisplayBuffs = false;
            NPC.value = 0;
        }
        public override void DrawBehind(int index)
        {
            Main.instance.DrawCacheNPCsBehindNonSolidTiles.Add(index);
        }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            int headIndex = (int)NPC.ai[3];
            if (NPC.damage <= 0 || headIndex < 0 || headIndex >= Main.maxNPCs)
            {
                return false;
            }

            NPC head = Main.npc[headIndex];
            if (!head.active || head.ModNPC is not GreatSerpentHead headData)
            {
                return false;
            }

            return !headData.IsDying && headData.TailStab == GreatSerpentHead.TailStabState.Stabbing && headData.TailStabDamaging;
        }

        public override bool ModifyCollisionData(Rectangle victimHitbox, ref int immunityCooldownSlot, ref MultipliableFloat damageMultiplier, ref Rectangle npcHitbox)
        {
            //NPC contact boxes never rotate. Test the actual 98px blade and its last-tick sweep instead,
            //so a horizontal sting can hit where its visible tip passes rather than only at its center.
            Vector2 bladeDirection = new Vector2((float)Math.Sin(NPC.rotation), -(float)Math.Cos(NPC.rotation));
            float bladeHalfLength = NPC.height * NPC.scale * 0.5f;
            Vector2 bladeRoot = NPC.Center + bladeDirection * (bladeHalfLength - 4f);
            Vector2 bladeTip = NPC.Center - bladeDirection * (bladeHalfLength - 2f);
            Vector2 victimPosition = victimHitbox.TopLeft();
            Vector2 victimSize = victimHitbox.Size();
            float bladeWidth = 8f * NPC.scale;
            float collisionPoint = 0f;
            bool intersects = Collision.CheckAABBvLineCollision(victimPosition, victimSize, bladeRoot, bladeTip, bladeWidth, ref collisionPoint);

            if (!intersects && SweepReady && NPC.damage > 0)
            {
                Vector2 previousDirection = new Vector2((float)Math.Sin(PreviousStrikeRotation), -(float)Math.Cos(PreviousStrikeRotation));
                Vector2 previousRoot = PreviousStrikeCenter + previousDirection * (bladeHalfLength - 4f);
                Vector2 previousTip = PreviousStrikeCenter - previousDirection * (bladeHalfLength - 2f);
                intersects = Collision.CheckAABBvLineCollision(victimPosition, victimSize, previousTip, bladeTip, bladeWidth, ref collisionPoint);
                if (!intersects)
                {
                    intersects = Collision.CheckAABBvLineCollision(victimPosition, victimSize, previousRoot, bladeRoot, bladeWidth, ref collisionPoint);
                }
            }

            npcHitbox = Rectangle.Empty;
            if (intersects && NPC.damage > 0)
            {
                //The precise line test already succeeded; this per-victim broadphase box lets vanilla
                //apply its normal roll immunity, damage, and OnHitPlayer venom exactly once.
                npcHitbox = victimHitbox;
            }

            return false;
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
