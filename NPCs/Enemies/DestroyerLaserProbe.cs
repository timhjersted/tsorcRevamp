using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
    class DestroyerLaserProbe : ModNPC
    {
        public override void SetStaticDefaults()
        {
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers(0)
            {
                Hide = true
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(NPC.type, value);
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
        }

        public override void SetDefaults()
        {
            NPC.CloneDefaults(NPCID.Probe);
            NPC.lifeMax = 75;
            NPC.damage = 48;
            NPC.scale = 1f;
            NPC.knockBackResist = 0;
            NPC.value = 0;
            NPC.defense = 0;
            NPC.lavaImmune = true;
        }

        public override void AI()
        {
            base.AI();
        }

        public override bool PreKill()
        {
            NPC.type = NPCID.Probe;
            return true;
        }

    }
}