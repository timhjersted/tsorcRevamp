using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
    class DestroyerLaserProbe : ModNPC
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Laser Probe");
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers(0) {
                Hide = true
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(NPC.type, value);
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
            NPC.buffImmune[BuffID.Confused] = true;
            NPC.buffImmune[BuffID.OnFire] = true;
            NPC.buffImmune[BuffID.Poisoned] = true;
            NPC.buffImmune[BuffID.CursedInferno] = true;
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