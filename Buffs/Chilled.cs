using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    public class Chilled : ModBuff
    { //every instance of the Chilled buff in the original source should be this one, not the vanilla one
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Frostbite");
            Description.SetDefault("Defense and speed reduced");
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.moveSpeed *= 0.8f;
            player.velocity.X *= 0.9f;
            player.jump = (int)(player.jump * 0.5);
            player.GetAttackSpeed(DamageClass.Melee) *= 0.5f;
            player.statDefense -= 20;
            if (Main.GameUpdateCount % 12 == 0)
            {
                int dust = Dust.NewDust(player.position, player.width, player.height, 43, 0, 0, 0, default, 0.8f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = player.velocity * 0.1f;
            }
        }
    }
}