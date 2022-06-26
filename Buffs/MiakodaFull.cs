using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    class MiakodaFull : ModBuff
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Miakoda - Full Moon Form");
            Description.SetDefault("An ancient being freed from Skeletron.");
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
                Projectile.NewProjectile(player.GetSource_Buff(buffIndex), player.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.Pets.MiakodaFull>(), 0, 0f, player.whoAmI);
            }
            player.endurance += 0.03f;
        }
    }
}
