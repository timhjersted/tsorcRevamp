using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Debuffs
{
    public class Frostbite : ModBuff
    {
        // Every instance of the Chilled buff in the original source should be this one, not the vanilla one
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.moveSpeed *= 0.9f;
            player.velocity *= 0.95f;
            player.jump /= 2;
            player.statDefense -= 20;
            player.GetAttackSpeed(DamageClass.Melee) *= 0.5f;

            if (Main.GameUpdateCount % 12 == 0)
            {
                var dust = Dust.NewDustDirect(player.position, player.width, player.height, DustID.TintableDustLighted, Scale: 0.8f);
                dust.noGravity = true;
                dust.velocity = player.velocity * 0.1f;
            }
        }
    }
}