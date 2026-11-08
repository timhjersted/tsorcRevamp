using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Content.Projectiles.Enemy.Basilisk
{
    /// <summary>
    /// Basilisk-only copy of HypnoticDisrupter (Kraken/TaurusKnight still share the original). Homing shadowflame
    /// orb; its one debuff is Crippled. ai[0]/ai[1]: a locked target point (X, -Y) when ai[1] &lt; -1, else a player index.
    /// </summary>
    class BasiliskHypnoticDisrupter : ModProjectile
    {
        // Halved outside Expert, which doubles most debuff durations on its own.
        public const int CrippledTicks = 5 * 60;
        private float previousMovementScale = 1f;

        // Intentional share: same orb sprite as the generic disrupter.
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(HypnoticDisrupter));

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 6;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.penetrate = 1;
            Projectile.hostile = true;
            Projectile.timeLeft = 360;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.scale = 1f;
            Projectile.tileCollide = false;
            Projectile.light = 1;
        }

        public override void AI()
        {
            // Restore unobstructed velocity before the legacy steering. Only displacement is slowed;
            // repeatedly scaling the steering state would otherwise compound the 40% terrain penalty.
            Projectile.velocity /= previousMovementScale;
            if (Projectile.ai[1] > 0 && Projectile.timeLeft > 300)
            {
                Projectile.timeLeft = 300;
                Projectile.tileCollide = true;
            }

            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 3)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;

                if (Projectile.frame >= Main.projFrames[Type])
                {
                    Projectile.frame = 0;
                }
            }

            // Homing: accelerate toward the target, faster rightward (0.2) than in other directions (0.1), capped at 10.
            Vector2 targetPosition = GetTargetPosition();

            if (targetPosition.X < Projectile.position.X && Projectile.velocity.X > -10)
            {
                Projectile.velocity.X -= 0.1f;
            }

            if (targetPosition.X > Projectile.position.X && Projectile.velocity.X < 10)
            {
                Projectile.velocity.X += 0.2f;
            }

            if (targetPosition.Y < Projectile.position.Y && Projectile.velocity.Y > -10)
            {
                Projectile.velocity.Y -= 0.1f;
            }

            if (targetPosition.Y > Projectile.position.Y && Projectile.velocity.Y < 10)
            {
                Projectile.velocity.Y += 0.1f;
            }

            if (Projectile.velocity.LengthSquared() > 0.01f)
            {
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            }

            previousMovementScale = 1f;
            if (Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height))
            {
                previousMovementScale = 0.6f;
            }

            Projectile.velocity *= previousMovementScale;
            if (Projectile.timeLeft <= 15)
            {
                Projectile.alpha = 255 - Projectile.timeLeft * 255 / 15;
            }

            if (!Main.dedServ)
            {
                Dust shadowflame = Dust.NewDustDirect(Projectile.position - new Vector2(0f, 10f), Projectile.width, Projectile.height, DustID.Shadowflame, 0, 0, 50, default, 2.0f);
                shadowflame.noGravity = true;

                if (Main.rand.NextBool(2))
                {
                    Lighting.AddLight(Projectile.Center, 0.7f, 0.2f, 0.2f);
                }
            }
        }

        public override bool? CanDamage()
        {
            if (Projectile.timeLeft <= 15)
            {
                return false;
            }

            return null;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(previousMovementScale);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            previousMovementScale = reader.ReadSingle();
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < 40; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(4f, 4f);
                Dust mote = Dust.NewDustPerfect(Projectile.Center, DustID.Shadowflame, velocity, 60, Color.Purple, Main.rand.NextFloat(0.8f, 1.5f));
                mote.noGravity = true;
            }

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item24 with { Volume = 0.3f }, Projectile.Center);
        }

        private Vector2 GetTargetPosition()
        {
            if (Projectile.ai[1] < -1f)
            {
                return new Vector2(Projectile.ai[0], -Projectile.ai[1]);
            }

            int targetPlayer = (int)Projectile.ai[0];
            if (targetPlayer < 0 || targetPlayer >= Main.maxPlayers || !Main.player[targetPlayer].active)
            {
                targetPlayer = Player.FindClosest(Projectile.position, Projectile.width, Projectile.height);
            }

            return Main.player[targetPlayer].Center;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            int frameHeight = texture.Height / Main.projFrames[Type];
            Rectangle sourceRectangle = new Rectangle(0, Projectile.frame * frameHeight, texture.Width, frameHeight);
            Vector2 origin = sourceRectangle.Size() / 2f;

            Color color = Projectile.GetAlpha(lightColor);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, sourceRectangle, color, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            int crippledTicks = CrippledTicks;
            if (!Main.expertMode)
            {
                crippledTicks /= 2;
            }

            // ai[1] < 1: aimed shots (locked point or player index). Shots fired with a positive ai[1] skip the cripple.
            if (Projectile.ai[1] < 1)
            {
                target.AddBuff(ModContent.BuffType<Crippled>(), crippledTicks, false);
            }
        }
    }
}
