using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    /// <summary>
    /// The Elemental Necromancer's Death Sigil: a flying death skull released from its palm. Straight at 8px/t, stops at
    /// walls, dies on the first player it hits (+15 Blight, nothing else). 112t life, the last 16t a harmless fade, so the
    /// damaging reach is 96t x 8 = 768px. Every ending bursts ~24 violet/wraith motes.
    /// Art: the shared EnemySpellSuddenDeathStrike sheet (44x480, twelve 44x40 frames), looping every 4 ticks, drawn
    /// upright and mirrored to its travel direction. That projectile class itself is never touched by this one.
    /// </summary>
    public class ElementalNecromancerDeathSigil : ModProjectile
    {
        public const int BlightPerHit = 15;
        internal const int FrameCount = 12;
        internal const int FrameHeight = 40;
        const int LifetimeTicks = 112;
        const int HarmlessFadeTicks = 16;
        const int TicksPerFrame = 4;

        // Deliberate, permanent sharing: the skull art IS the shared death-strike sheet.
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(EnemySpellSuddenDeathStrike));

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = FrameCount;
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = LifetimeTicks;
            Projectile.aiStyle = -1;
        }

        public override bool CanHitPlayer(Player target)
        {
            return Projectile.timeLeft > HarmlessFadeTicks;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            // Runs on the victim's machine; the kill is relayed to the server by tsorcGlobalProjectile.
            BlightBuildup.Apply(target, BlightPerHit);
            Projectile.Kill();
        }

        public override void AI()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= TicksPerFrame)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % FrameCount;
            }

            if (Projectile.velocity.X != 0f)
            {
                Projectile.spriteDirection = Projectile.velocity.X > 0f ? 1 : -1;
            }

            // Harmless fade over the last 16 ticks: alpha 0 -> 255.
            if (Projectile.timeLeft <= HarmlessFadeTicks)
            {
                float fade = 1f - Projectile.timeLeft / (float)HarmlessFadeTicks;
                Projectile.alpha = (int)(fade * 255f);
            }

            if (Main.dedServ)
            {
                return;
            }

            Lighting.AddLight(Projectile.Center, 0.35f, 0.15f, 0.5f);
            if (Projectile.timeLeft % 3 == 0)
            {
                Dust mote = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(8f, 8f), DustID.Shadowflame,
                    -Projectile.velocity * 0.1f, 100, default, 0.9f);
                mote.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Rectangle frame = new Rectangle(0, Projectile.frame * FrameHeight, texture.Width, FrameHeight);
            Vector2 origin = frame.Size() * 0.5f;
            SpriteEffects effects = SpriteEffects.None;
            if (Projectile.spriteDirection < 0)
            {
                effects = SpriteEffects.FlipHorizontally;
            }

            // Spell light, not lit by the surroundings; alpha carries the end fade.
            Color drawColor = Color.White * Projectile.Opacity;
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, drawColor, 0f, origin, Projectile.scale, effects, 0f);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < 24; i++)
            {
                int dustType = DustID.Shadowflame;
                if (i % 3 == 0)
                {
                    dustType = DustID.Wraith;
                }

                Dust burst = Dust.NewDustPerfect(Projectile.Center, dustType, Main.rand.NextVector2Circular(4f, 4f), 90, default, 1.3f);
                burst.noGravity = true;
            }
        }
    }
}
