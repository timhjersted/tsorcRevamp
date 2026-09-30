using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Bosses.SuperHardMode.Fiends;

namespace tsorcRevamp.Buffs
{
    public class ThermalRise : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // Refills wing/rocket boot flight time
            player.wingTime = 60;
            player.rocketTime = 300;
        }

        // Two sources grant this buff: the Destroyer's lasers (the default Description) and Marilith's abyss heat. Swap to the
        // abyss wording while Marilith is alive; AnyNPCs is cheap enough to run on every tooltip draw.
        public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
        {
            if (NPC.AnyNPCs(ModContent.NPCType<FireFiendMarilith>()))
            {
                tip = Language.GetTextValue("Mods.tsorcRevamp.Buffs.ThermalRise.AbyssDescription");
            }
        }
    }
}
