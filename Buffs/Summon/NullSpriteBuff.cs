using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Summon;

namespace tsorcRevamp.Buffs.Summon
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
            if (player.HeldItem.type == ModContent.ItemType<NullSpriteStaff>())
            {
                player.maxMinions += 1;
            }

            if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Summon.NullSprite>()] > 0)
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
