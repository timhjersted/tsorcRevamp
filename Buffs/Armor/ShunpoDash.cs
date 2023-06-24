using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Armor
{
    public class ShunpoDash : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }
        public const float ShunpoDashDuration = 0.3f;
        public const int Cooldown = 20;
        public override void Update(Player player, ref int buffIndex)
        {
            player.GetModPlayer<tsorcRevampPlayer>().ShunpoTimer -= 0.0167f;
            player.immune = true;
            if (player.GetModPlayer<tsorcRevampPlayer>().ShunpoTimer > 0f)
            {
                player.velocity = player.DirectionTo(Main.MouseWorld) * 15f;
            }
            if (player.velocity.X > 0)
            {
                player.direction = 1;
            }
            else
            { player.direction = -1; }
        }
    }
}
