using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    class RTQ2 : ModBuff
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("RTQ2");
            Description.SetDefault("RTQ2 is following you");
            Main.buffNoTimeDisplay[Type] = true;
            Main.vanityPet[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.buffTime[buffIndex] = 18000;
            player.GetModPlayer<tsorcRevampPlayer>().RTQ2 = true;
            bool petProjectileNotSpawned = player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Pets.RTQ2>()] <= 0;
            if (petProjectileNotSpawned && player.whoAmI == Main.myPlayer)
            {
                Projectile.NewProjectile(player.GetSource_Buff(Type), player.position.X + player.width / 2, player.position.Y + player.height / 2, 0f, 0f, ModContent.ProjectileType<Projectiles.Pets.RTQ2>(), 0, 0f, player.whoAmI, 0f, 0f);
            }
        }

    }
}
