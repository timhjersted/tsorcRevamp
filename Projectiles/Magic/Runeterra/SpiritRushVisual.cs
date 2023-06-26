using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Runeterra.Magic;

namespace tsorcRevamp.Projectiles.Magic.Runeterra
{

    public class SpiritRushVisual : ModProjectile
    {
		public override void SetStaticDefaults()
		{
            Main.projFrames[Projectile.type] = 4;
        }

		public override void SetDefaults()
		{
			Projectile.netImportant = true; // This ensures that the projectile is synced when other players join the world.
			Projectile.width = 90; // The width of your projectile
			Projectile.height = 90; // The height of your projectile
			Projectile.friendly = true; // Deals damage to enemies
			Projectile.DamageType = DamageClass.Magic;
			Projectile.usesLocalNPCImmunity = true; // Used for hit cooldown changes in the ai hook
			Projectile.localNPCHitCooldown = 10; // This facilitates custom hit cooldown logic
			Projectile.tileCollide = false;
			Projectile.aiStyle = -1;
			Projectile.extraUpdates = 5;
		}
        public override void AI()
		{
			Player player = Main.player[Projectile.owner];
			if (player.HasBuff(ModContent.BuffType<OrbOfSpiritualityDash>()))
			{
				Projectile.timeLeft = 2;
			}
			Projectile.position = player.Center - new Vector2(player.width * 2.25f, player.height);
            Lighting.AddLight(Projectile.position, Color.LightSteelBlue.ToVector3() * 2f);
            Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.BlueFlare, 0, 0, 150, default, 2f);
			Projectile.frame = 3 - player.GetModPlayer<tsorcRevampPlayer>().SpiritRushCharges;
        }
        public override bool? CanDamage()
        {
            return false;
        }
    }
}