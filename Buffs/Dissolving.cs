using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    public class Dissolving : ModBuff
    {
        public const int MaxDissolvingStacks = 10;
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
            var modNPC = npc.GetGlobalNPC<tsrDissolvingNPC>();
            if (modNPC.DissolvingStacks < MaxDissolvingStacks)
                modNPC.DissolvingStacks++;
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
                modifiers.FinalDamage.Base *= multiplier;
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
                // Each stack is 1% extra dmg
                return 1 + (DissolvingStacks / 100f);
            }
        }
    }
}
