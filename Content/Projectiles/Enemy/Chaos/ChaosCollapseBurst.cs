using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Content.Projectiles.Enemy.Chaos
{
    ///<summary>
    ///One hit of the gale ring's collapse detonation (see ChaosGaleRing.TriggerCollapseExplosion and
    ///ChaosCollapseExplosionController, which spawn these in three staggered waves to cover the blast). Each
    ///instance deals its own damage on a circular radius — "each sprite does the damage" — rather than the
    ///whole detonation being one hit somewhere else. Reuses EnemySpellAbyssStormExplosion's animated burst art
    ///unmodified: same frames, same colour, no tint.
    ///</summary>
    class ChaosCollapseBurst : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Content/Projectiles/Enemy/EnemySpellAbyssStormExplosion";

        const int FrameTicks = 5;
        const float HitRadius = 100f; //matches the sprite's own calibration (see EnemySpellAbyssStormExplosion)
        // ai[0] is the player whose Gale ring collapsed; other players do not see or hear this ring's bursts.

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 7;
        }

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.width = (int)HitRadius * 2;
            Projectile.height = (int)HitRadius * 2;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Main.projFrames[Type] * FrameTicks;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            if (!Main.dedServ && Main.myPlayer == (int)Projectile.ai[0])
            {
                Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/HollowKnight/flamebearer_shoot") with { Volume = 0.7f, PitchVariance = 0.2f }, Projectile.Center);
            }
        }

        public override void AI()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= FrameTicks)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = System.Math.Min(Projectile.frame + 1, Main.projFrames[Projectile.type] - 1);
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 nearest = Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight());
            return Vector2.DistanceSquared(Projectile.Center, nearest) <= HitRadius * HitRadius;
        }

        public override bool CanHitPlayer(Player target)
        {
            return target.whoAmI == (int)Projectile.ai[0];
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<DarkInferno>(), 180);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.myPlayer != (int)Projectile.ai[0])
            {
                return false;
            }

            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
            int frameHeight = texture.Height / Main.projFrames[Type];
            Rectangle source = new Rectangle(0, Projectile.frame * frameHeight, texture.Width, frameHeight);
            Vector2 origin = source.Size() * 0.5f;

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, source, Color.White,
                Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);

            return false;
        }
    }
}
