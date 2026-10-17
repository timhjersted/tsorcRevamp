using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Weapons.Summon.NullSpriteStaff
{
    public class NullSpriteBuff : ModBuff
    {

        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            if (player.HeldItem.type == ModContent.ItemType<NullSpriteStaffItem>())
            {
                player.maxMinions += 1;
            }

            if (player.ownedProjectileCounts[ModContent.ProjectileType<NullSpriteProj>()] > 0)
            {
                player.buffTime[buffIndex] = 18000;
            }
            else
            {
                player.DelBuff(buffIndex);
                buffIndex--;
            }
        }
    }
}
