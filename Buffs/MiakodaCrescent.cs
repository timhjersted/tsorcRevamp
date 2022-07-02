using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    class MiakodaCrescent : ModBuff
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Miakoda - Crescent Moon Form");
            Description.SetDefault("An ancient being freed from Skeletron." +
                                   "\nIt is said to possess a divine smile");
            Main.buffNoTimeDisplay[Type] = true;
            Main.vanityPet[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.buffTime[buffIndex] = 18000;
            player.GetModPlayer<tsorcRevampPlayer>().MiakodaCrescent = true;
            bool petProjectileNotSpawned = player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Pets.MiakodaCrescent>()] <= 0;
            if (petProjectileNotSpawned && player.whoAmI == Main.myPlayer)
            {
                Projectile.NewProjectile(player.GetSource_Buff(buffIndex), player.position.X + player.width / 2, player.position.Y + player.height / 2, 0f, 0f, ModContent.ProjectileType<Projectiles.Pets.MiakodaCrescent>(), 0, 0f, player.whoAmI, 0f, 0f);
            }
            player.GetDamage(DamageClass.Generic) *= 1.03f;
        }
    }
}
