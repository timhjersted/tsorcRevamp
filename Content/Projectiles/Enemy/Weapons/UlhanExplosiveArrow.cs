using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy.Weapons
{
    public class UlhanExplosiveArrow : ModProjectile
    {
        public const float MaximumTravel = 15f * 16f;
        // PLACEHOLDER sprite: copy of Content/Items/Ammo/CruelArrow.png; 14x44, top leads.
        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.tileCollide = true;
            Projectile.timeLeft = 120;
            Projectile.aiStyle = -1;
        }

        // The blast owns the entire 75-damage hit. Player intersection is detected on authority,
        // since hostile OnHitPlayer runs on the victim's client and cannot spawn a server blast.
        public override bool? CanDamage() => false;

        public override void AI()
        {
            if (Projectile.ai[1] == 1f)
            {
                Projectile.Kill();
                return;
            }
            if (++Projectile.ai[2] > 15f)
                Projectile.velocity.Y += 0.1f;
            float remaining = MaximumTravel - Projectile.ai[0];
            float step = Projectile.velocity.Length();
            if (step >= remaining)
            {
                Projectile.velocity *= remaining / step;
                step = remaining;
                Projectile.ai[1] = 1f;
            }
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                float firstContact = step;
                bool hitPlayer = false;
                foreach (Player player in Main.ActivePlayers)
                {
                    if (player.dead || player.ghost)
                        continue;
                    float contact = 0f;
                    if (Projectile.Hitbox.Intersects(player.Hitbox) || Collision.CheckAABBvLineCollision(player.position, player.Size, Projectile.Center,
                        Projectile.Center + Projectile.velocity, Projectile.width, ref contact))
                    {
                        firstContact = MathHelper.Clamp(contact, 0f, firstContact);
                        hitPlayer = true;
                    }
                }
                if (hitPlayer)
                {
                    Projectile.Center += Projectile.velocity.SafeNormalize(Vector2.Zero) * firstContact;
                    Projectile.Kill();
                    return;
                }
            }
            Projectile.ai[0] += step;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            if (Main.dedServ)
                return;
            Lighting.AddLight(Projectile.Center, 0.55f, 0.22f, 0.03f);
            Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.OrangeTorch,
                -Projectile.velocity * 0.1f, 80, default, 0.7f);
            dust.noGravity = true;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<UlhanFireExplosion>(), Projectile.damage, Projectile.knockBack, Main.myPlayer);
        }
    }
}
