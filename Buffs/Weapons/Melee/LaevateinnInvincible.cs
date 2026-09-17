using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Melee.Shortswords;

namespace tsorcRevamp.Buffs.Weapons.Melee
{
    public class LaevateinnInvincible : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            if (player.HeldItem.type == ModContent.ItemType<Laevateinn>() || player.HeldItem.type == ModContent.ItemType<ClaiomhSolais>())
            {
                player.immune = true;
                player.SetImmuneTimeForAllTypes(1);
            }
        }
    }
}
