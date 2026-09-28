using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Buffs;

namespace tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Projectiles
{

    public class SpiritRushVisual : ModProjectile
    {
        public const int Frames = 24;
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = Frames;
        }

        public override void SetDefaults()
        {
            Projectile.netImportant = true; // This ensures that the projectile is synced when other players join the world.
            Projectile.width = 90; // The width of your projectile
            Projectile.height = 90; // The height of your projectile
            Projectile.friendly = true; // Deals damage to enemies
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;
            Projectile.aiStyle = -1;
        }
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            var modPlayer = player.GetModPlayer<RuneterraOrbPlayer>();
            if (player.HasBuff(ModContent.BuffType<OrbOfSpiritualityDash>()))
            {
                Projectile.timeLeft = 2;
            }
            Projectile.Center = player.Center;
            Lighting.AddLight(Projectile.position, Color.LightSteelBlue.ToVector3() * 2f);
            Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.BlueFlare, 0, 0, 150, default, 2f);
            int framesPerCharge = Frames / 3;
            float fadingFrames = MathF.Max(modPlayer.SpiritRushCooldown / (RuneterraOrbPlayer.SpiritRushCooldownTime / (float)framesPerCharge), 0);
            Projectile.frame = Math.Min(Math.Max(Frames - (modPlayer.SpiritRushCharges * framesPerCharge) - (int)fadingFrames, 0), 23);
        }
        public override bool? CanDamage()
        {
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return UsefulFunctions.RenderTransparentTexture(Projectile, TransparentTextureHandler.TransparentTextureType.SpiritRushVisual, lightColor);
        }
    }
}