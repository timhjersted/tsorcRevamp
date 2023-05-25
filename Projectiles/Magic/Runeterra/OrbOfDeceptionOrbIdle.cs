using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.GameContent;
using ReLogic.Content;
using tsorcRevamp.Items.Weapons.Magic.Runeterra;
using Terraria.DataStructures;

namespace tsorcRevamp.Projectiles.Magic.Runeterra
{

    public class OrbOfDeceptionOrbIdle : ModProjectile
    {

		public override void SetStaticDefaults()
		{
			// These lines facilitate the trail drawing
			ProjectileID.Sets.TrailCacheLength[Projectile.type] = 6;
			ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            Main.projFrames[Projectile.type] = 4;
        }

		public override void SetDefaults()
		{
			Projectile.netImportant = true; // This ensures that the projectile is synced when other players join the world.
            Projectile.aiStyle = ProjAIStyleID.FloatInFrontPet;
			Projectile.width = 50; // The width of your projectile
			Projectile.height = 50; // The height of your projectile
			Projectile.friendly = true; // Deals damage to enemies
			Projectile.penetrate = -1; // Infinite pierce
			Projectile.DamageType = DamageClass.Magic;
			Projectile.tileCollide = false;
			Projectile.aiStyle = -1;

		}

        public override void OnSpawn(IEntitySource source)
        {
            Player player = Main.player[Projectile.owner];
        }

        public override void AI()
		{
			Player player = Main.player[Projectile.owner];
            if (player.ownedProjectileCounts[ModContent.ProjectileType<OrbOfDeceptionOrb>()] != 0 && player.ownedProjectileCounts[ModContent.ProjectileType<OrbOfDeceptionOrbFilled>()] != 0)
            {
                Projectile.Kill();
            }
			Visuals();
		}
        private void Visuals()
        {
            int frameSpeed = 5;

            Projectile.frameCounter++;

            if (Projectile.frameCounter >= frameSpeed)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;

                if (Projectile.frame >= Main.projFrames[Projectile.type])
                {
                    Projectile.frame = 0;
                }
            }

            Lighting.AddLight(Projectile.Center, Color.LightSteelBlue.ToVector3() * 0.78f);
            Dust.NewDust(Projectile.Center, 2, 2, DustID.MagicMirror, 0, 0, 150, default, 0.5f);
        }
    }
}