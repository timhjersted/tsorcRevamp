using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Players;

namespace tsorcRevamp.Buffs.Armor
{
    public class DragonForm : ModBuff
    {
        public override string Texture => "tsorcRevamp/Buffs/Armor/DragonForm";

        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.GetModPlayer<CursedDragonArmorPlayer>().CursedDragonVisuals = true;
        }
    }
}