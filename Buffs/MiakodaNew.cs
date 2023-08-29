using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs;

class MiakodaNew : ModBuff
{
    public override void SetStaticDefaults()
    {
        DisplayName.SetDefault("Miakoda - New Moon Form");
        Description.SetDefault("An ancient being freed from Skeletron.");
        Main.buffNoTimeDisplay[Type] = true;
        Main.vanityPet[Type] = true;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        player.buffTime[buffIndex] = 18000;
        player.GetModPlayer<tsorcRevampPlayer>().MiakodaNew = true;
        bool petProjectileNotSpawned = player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Pets.MiakodaNew>()] <= 0;
        if (petProjectileNotSpawned && player.whoAmI == Main.myPlayer)
        {
            Projectile.NewProjectile(player.GetSource_Buff(buffIndex), player.position.X + player.width / 2, player.position.Y + player.height / 2, 0f, 0f, ModContent.ProjectileType<Projectiles.Pets.MiakodaNew>(), 0, 0f, player.whoAmI, 0f, 0f);
        }
        player.moveSpeed += 0.05f;
    }
}
