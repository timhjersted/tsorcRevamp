using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
    class PrimeLaserProbe : ModNPC
    {
        public override void SetStaticDefaults()
        {
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
        }

        public override void SetDefaults()
        {
            NPC.CloneDefaults(NPCID.Probe);
            NPC.lifeMax = 75;
            NPC.damage = 48;
            NPC.scale = 1f;
            NPC.knockBackResist = 0;
            NPC.dontTakeDamage = true;
            NPC.value = 0;
            NPC.defense = 0;
            NPC.lavaImmune = true;
        }

        public override void AI()
        {

        }

        public override bool PreKill()
        {
            NPC.type = NPCID.Probe;
            return true;
        }

    }
}