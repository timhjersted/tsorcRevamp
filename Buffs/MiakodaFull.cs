using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    class MiakodaFull : ModBuff
    {
        public override void SetDefaults()
        {
            DisplayName.SetDefault("Miakoda - Full Moon Form");
            Description.SetDefault("An ancient being freed from Gravelord Nito.");
            Main.buffNoTimeDisplay[Type] = true;
            Main.vanityPet[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.buffTime[buffIndex] = 18000;
            player.GetModPlayer<tsorcRevampPlayer>().MiakodaFull = true;
            bool petProjectileNotSpawned = player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Pets.MiakodaFull>()] <= 0;
            if (petProjectileNotSpawned && player.whoAmI == Main.myPlayer)
            {
                Projectile.NewProjectile(player.position.X + player.width / 2, player.position.Y + player.height / 2, 0f, 0f, ModContent.ProjectileType<Projectiles.Pets.MiakodaFull>(), 0, 0f, player.whoAmI, 0f, 0f);
            }
            player.endurance += 0.03f;
        }
    }
}
