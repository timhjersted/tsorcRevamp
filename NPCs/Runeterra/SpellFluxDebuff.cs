using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Runeterra;

public class SpellFluxDebuff : ModBuff
{
    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        var modPlayer = player.GetModPlayer<SpellFluxPlayer>();
        modPlayer.SpellFlux = true;
    }

    public override void Update(NPC npc, ref int buffIndex)
    {
        var globalNpc = npc.GetGlobalNPC<SpellFluxNpc>();
        globalNpc.SpellFlux = true;
    }
}

public class SpellFluxPlayer : ModPlayer
{
    public bool SpellFlux = false;
    public override void ResetEffects()
    {
        SpellFlux = false;
    }
}

public class SpellFluxNpc : GlobalNPC
{
    public bool SpellFlux = false;
    public override bool InstancePerEntity => true;

    public override void ResetEffects(NPC npc)
    {
        SpellFlux = false;
    }
}