using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy.RoyalKnightBolts
{
    /// <summary>The Royal Knight's thunder arrow: a straight, gravity-free electric bolt (16 px/t). Leaves the bow's nock,
    /// dies on tiles and players with a 30-dust gold/electric burst, and fades over its last 12 ticks if it reaches
    /// 75 ticks (1,200 px) without hitting anything. Applies Electrified 3 s on hit.
    /// ai[0] = 1: the visual-only skyward Grounding Shot arrow (no damage, no tile collision, flies off screen).</summary>
    public class RoyalThunderArrow : ModProjectile
    {
        // Art: ArcherBolt 10x30, tip at the TOP edge, so rotation = travel direction + 90 degrees.
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(ArcherBolt));

        public const int LifetimeTicks = 75;
        const int FadeTicks = 12;
        public const int ElectrifiedTicks = 3 * 60;
        static readonly Color BoltTint = new Color(255, 235, 150);

        bool VisualOnly => Projectile.ai[0] > 0f;

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = true;
            Projectile.penetrate = 1;
            Projectile.aiStyle = -1;
            Projectile.timeLeft = LifetimeTicks;
            Projectile.extraUpdates = 0;
        }

        public override bool? CanDamage()
        {
            if (VisualOnly)
            {
                return false;
            }

            return null;
        }

        public override void AI()
        {
            // Deliberate: the Grounding Shot arrow streaks straight up off screen; it passes through tiles and never
            // damages anything (the strike it calls is a separate projectile). Set every tick on every machine, because
            // hostile and tileCollide are not synced but ai[0] is. Its lifetime is the 75t default: 1,200 px up.
            if (VisualOnly)
            {
                Projectile.tileCollide = false;
                Projectile.hostile = false;
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            // Timeout fade: alpha ramps 0 -> 255 over the last FadeTicks, so an arrow that runs out mid-air dims away.
            if (Projectile.timeLeft < FadeTicks)
            {
                Projectile.alpha = (FadeTicks - Projectile.timeLeft) * 255 / FadeTicks;
            }

            Lighting.AddLight(Projectile.Center, 0.25f, 0.35f, 0.55f);

            if (Main.dedServ)
            {
                return;
            }

            // Trail: 1 Electric every 2 ticks and 1 gold every 4, drifting slightly behind the bolt.
            if (Projectile.timeLeft % 2 == 0)
            {
                Dust spark = Dust.NewDustPerfect(Projectile.Center, DustID.Electric, -Projectile.velocity * 0.1f, 100, default, 0.9f);
                spark.noGravity = true;
            }

            if (Projectile.timeLeft % 4 == 0)
            {
                Dust ember = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, -Projectile.velocity * 0.05f, 100, default, 1.0f);
                ember.noGravity = true;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            // Runs on the hit player's machine; the buff is player-local.
            target.AddBuff(BuffID.Electrified, ElectrifiedTicks);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ || VisualOnly)
            {
                return;
            }

            // Every death path (tile, player, timeout) bursts: 18 Electric, 8 gold, 4 white-hot cores.
            SoundEngine.PlaySound(SoundID.Item93 with { Volume = 0.5f, PitchVariance = 0.15f }, Projectile.Center);

            for (int i = 0; i < 18; i++)
            {
                Dust spark = Dust.NewDustPerfect(Projectile.Center, DustID.Electric, Main.rand.NextVector2Circular(3.5f, 3.5f), 80, default, 1.0f);
                spark.noGravity = true;
            }

            for (int i = 0; i < 8; i++)
            {
                Dust ember = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, Main.rand.NextVector2Circular(2.5f, 2.5f), 80, default, 1.2f);
                ember.noGravity = true;
            }

            for (int i = 0; i < 4; i++)
            {
                Dust core = Dust.NewDustPerfect(Projectile.Center, DustID.AncientLight, Main.rand.NextVector2Circular(1.5f, 1.5f), 60, Color.White, 0.9f);
                core.noGravity = true;
            }
        }

        // The sprite is 10x30 with the tip at the top; draw it centred on the hitbox, tinted gold, full-bright.
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() / 2f;
            float opacity = 1f - Projectile.alpha / 255f;
            Color drawColor = BoltTint * opacity;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            Main.EntitySpriteDraw(texture, drawPosition, null, drawColor, Projectile.rotation, origin, 1f, SpriteEffects.None, 0);
            return false;
        }
    }
}
