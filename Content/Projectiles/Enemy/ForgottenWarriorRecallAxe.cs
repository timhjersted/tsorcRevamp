using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Enemies.GhostFighter;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    public class ForgottenWarriorRecallAxe : ModProjectile
    {
        Vector2 _launchDirection;
        bool _fading;
        readonly byte[] _hitLegs = new byte[Main.maxPlayers];
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(EnemyEphemeralThrowingAxeProj));
        int OutTicks => Math.Max(1, (int)Math.Ceiling(2f * Projectile.ai[2] / 14f));
        bool Returning => Projectile.ai[1] > OutTicks + 10;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 4;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 52;
            Projectile.hostile = true;
            Projectile.penetrate = -1; // Deliberate: one hit per player on each leg; the weapon must return.
            Projectile.tileCollide = false; // A spectral axe passes through walls on both legs.
            Projectile.timeLeft = 240;
            Projectile.DamageType = DamageClass.Ranged;
        }

        GhostOfTheForgottenWarrior Owner()
        {
            int index = (int)Projectile.ai[0];
            if (index < 0 || index >= Main.maxNPCs || !Main.npc[index].active) return null;
            return Main.npc[index].ModNPC as GhostOfTheForgottenWarrior;
        }
        public override void AI()
        {
            if (_launchDirection == Vector2.Zero)
                _launchDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Projectile.ai[1]++;
            var owner = Owner();
            if (!_fading && ((Projectile.timeLeft <= 15 || Projectile.ai[1] >= 225f)
                || (Main.netMode != NetmodeID.MultiplayerClient && (owner == null || !owner.OwnsRecallAxe(Projectile)))))
            {
                _fading = true;
                Projectile.timeLeft = Math.Min(Projectile.timeLeft, 15);
                Projectile.netUpdate = true;
            }
            Projectile.rotation += 0.4f * (_launchDirection.X < 0f ? -1f : 1f);
            if (_fading)
            {
                Projectile.hostile = false;
                Projectile.velocity *= 0.9f;
                Projectile.alpha = (int)(255f * (1f - Projectile.timeLeft / 15f));
                return;
            }

            int age = (int)Projectile.ai[1];
            if (age <= OutTicks)
            {
                // Midpoint samples integrate the linear deceleration to exactly D in OutTicks updates.
                float speed = 2f * Projectile.ai[2] / OutTicks * (1f - (age - 0.5f) / OutTicks);
                Projectile.velocity = _launchDirection * speed;
            }
            else if (!Returning)
                Projectile.velocity = Vector2.Zero;
            else if (owner != null)
            {
                Vector2 toHand = owner.RecallHandPosition - Projectile.Center;
                float speed = Math.Min(15f, (age - OutTicks - 10) * 0.5f);
                Projectile.velocity = toHand.SafeNormalize(Vector2.UnitX) * speed;
                if (toHand.LengthSquared() <= 16f * 16f && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    owner.FinishRecall();
                    Projectile.Kill();
                    return;
                }
            }
            if (!Main.dedServ && age % 3 == 0)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(14f, 14f);
                Dust mote = Dust.NewDustPerfect(Projectile.Center + offset, DustID.Torch,
                    Returning || age <= OutTicks ? -Projectile.velocity * 0.08f : -offset * 0.12f,
                    80, Color.Orange, 1.1f);
                mote.noGravity = true;
            }
        }
        public override bool? CanDamage() => _fading || ((int)Projectile.ai[1] > OutTicks && !Returning) ? false : null;
        public override bool CanHitPlayer(Player target) => (_hitLegs[target.whoAmI] & (Returning ? 2 : 1)) == 0;
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            _hitLegs[target.whoAmI] |= (byte)(Returning ? 2 : 1);
            target.AddBuff(BuffID.Ichor, 10 * 60);
        }
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(_launchDirection.X); writer.Write(_launchDirection.Y); writer.Write(_fading);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            _launchDirection = new Vector2(reader.ReadSingle(), reader.ReadSingle()); _fading = reader.ReadBoolean();
        }
        public override void OnKill(int timeLeft)
        {
            var owner = Owner();
            bool caught = !_fading && owner != null && Vector2.DistanceSquared(owner.RecallHandPosition, Projectile.Center) <= 32f * 32f;
            if (owner != null && owner.OwnsRecallAxe(Projectile)) owner.FinishRecall();
            if (Main.dedServ) return;
            SoundEngine.PlaySound(caught ? SoundID.Dig : SoundID.Item8, Projectile.Center);
            for (int i = 0; i < (caught ? 20 : 40); i++)
            {
                Dust mote = Dust.NewDustPerfect(Projectile.Center, DustID.Torch,
                    Main.rand.NextVector2Circular(4f, 4f), 80, Color.Orange, 1.2f);
                mote.noGravity = true;
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D glow = ModContent.Request<Texture2D>(Texture + "_Glowmask").Value;
            float opacity = 1f - Projectile.alpha / 255f;
            for (int i = 3; i > 0; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero) continue;
                Main.EntitySpriteDraw(glow, Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition,
                    null, Color.White * (opacity * 0.2f * (1f - i / 6f)), Projectile.oldRot[i], glow.Size() / 2f,
                    Projectile.scale, SpriteEffects.None, 0);
            }
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, lightColor * opacity,
                Projectile.rotation, texture.Size() / 2f, Projectile.scale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(glow, Projectile.Center - Main.screenPosition, null, Color.White * (0.2f * opacity),
                Projectile.rotation, glow.Size() / 2f, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }
}
