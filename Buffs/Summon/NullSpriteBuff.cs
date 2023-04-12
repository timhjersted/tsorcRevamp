using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Summon
{
    public class NullSpriteBuff : ModBuff
    {

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Null Sprite");
            // Description.SetDefault("The null sprite will fight for you");
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
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
