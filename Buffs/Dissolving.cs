using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    public class Dissolving : ModBuff
    {
        public override string Texture => "Terraria/Images/Buff"; //enemy only

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            npc.buffTime[buffIndex] = 2;
        }

        public override bool ReApply(NPC npc, int time, int buffIndex)
        {
            npc.GetGlobalNPC<tsrDissolvingNPC>().DissolvingStacks++;
            return false;
        }

        private class tsrDissolvingNPC : GlobalNPC
        {
            public override bool InstancePerEntity => true;

            public int DissolvingStacks = 1;

            public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
            {
                if (!npc.HasBuff(ModContent.BuffType<Dissolving>()))
                {
                    return;
                }

                float multiplier = DissolveMultiplier(DissolvingStacks);
                modifiers.TargetDamageMultiplier *= multiplier;
            }

            public override bool PreAI(NPC npc)
            {
                if (!npc.HasBuff(ModContent.BuffType<Dissolving>()))
                {
                    return base.PreAI(npc);
                }

                Dust dust = Dust.NewDustDirect(npc.position, npc.width, npc.height, DustID.VenomStaff);
                dust.noGravity = true;

                float speedMod = Vector2.Distance(dust.position, npc.Center);
                speedMod /= 12;
                dust.velocity = UsefulFunctions.Aim(dust.position, npc.Center, speedMod);

                return base.PreAI(npc);
            }

            private static float DissolveMultiplier(int DissolvingStacks)
            {
                // y=\left(\log_{1.02}\left(x+201\right)\right)-267.808
                float dissolveMod = (float)((Math.Log(DissolvingStacks + 201, 1.02)) - 267.808);
                return 1 + (dissolveMod / 250f);
            }
        }
    }
}
