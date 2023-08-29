using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs;

class MagicBarrier : ModBuff
{
    public override void SetStaticDefaults()
    {
        DisplayName.SetDefault("Magic Barrier");
        Description.SetDefault("Defense is increased by 20!");
        Main.debuff[Type] = false;
        Main.buffNoTimeDisplay[Type] = false;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        player.statDefense += 20;            
        Projectile.NewProjectile(player.GetSource_Buff(buffIndex), player.position.X + (float)(player.width / 2), player.position.Y + (float)(player.height / 2), player.velocity.X, player.velocity.Y, ModContent.ProjectileType<Projectiles.Barrier>(), 0, 0f, player.whoAmI, 0f, 0f);
        Lighting.AddLight(player.Center, .450f, .450f, .600f);
    }
}