using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Melee.Broadswords;
using tsorcRevamp.Items.Armors;

namespace tsorcRevamp.Projectiles.Melee.Broadswords
{
    class SwordOfLordGwynController : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        const int TapWindow = 14;
        const int NovaChargeTicks = 54;
        const int DashInvulnerabilityTicks = 18;
        const int ThrowFollowThroughTicks = 12;

        int Mode => (int)Projectile.ai[0]; //0 = cursor high, 1 = cursor low
        bool UsesRightClick => Projectile.ai[1] == 1f;
        ref float ThrowTimer => ref Projectile.ai[2];
        float Timer => Projectile.localAI[0];

        bool IsInputHeld(Player player) => UsesRightClick ? player.controlUseTile : player.channel;

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 60;
            Projectile.hide = true;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Melee;
        }

        public override bool? CanDamage()
        {
            return false;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (!player.active || player.dead || player.noItems || player.CCed)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = player.Center;
            Projectile.timeLeft = 60;
            Projectile.localAI[0]++;

            if (ThrowTimer > 0f)
            {
                RunThrowFollowThrough(player);
                return;
            }

            bool channeling = IsInputHeld(player);

            if (channeling)
            {
                player.heldProj = Projectile.whoAmI;
                player.itemTime = 2;
                player.itemAnimation = 2;
                if (Main.myPlayer == Projectile.owner)
                {
                    player.ChangeDir(Main.MouseWorld.X > player.Center.X ? 1 : -1);
                }
            }

            if (Timer <= TapWindow)
            {
                if (!channeling)
                {
                    if (Main.myPlayer == Projectile.owner)
                    {
                        CastTap(player);
                    }
                    Projectile.Kill();
                }
                return;
            }

            if (Mode == 0)
            {
                // Hold pose: keep the spear raised overhead in the hand while the button is held,
                // and only throw it on RELEASE (the old behavior fired instantly past the tap window).
                if (channeling)
                {
                    HoldSpearPose(player);
                }
                else
                {
                    bool threwSpear = true;
                    if (Main.myPlayer == Projectile.owner)
                    {
                        threwSpear = CastSunlightSpear(player);
                    }

                    if (threwSpear)
                    {
                        ThrowTimer = 1f;
                        Projectile.netUpdate = true;
                    }
                    else
                    {
                        Projectile.Kill();
                    }
                }
            }
            else
            {
                RunCinderNovaCharge(player, channeling);
            }
        }

        Vector2 HeldAim(Player player) =>
            Projectile.velocity.SafeNormalize(Vector2.UnitX * player.direction);

        void HoldSpearPose(Player player)
        {
            if (Main.myPlayer == Projectile.owner)
            {
                Vector2 aim = (Main.MouseWorld - player.MountedCenter)
                    .SafeNormalize(Vector2.UnitX * player.direction);
                if (Vector2.DistanceSquared(Projectile.velocity, aim) > 0.0004f)
                {
                    Projectile.velocity = aim;
                    Projectile.netUpdate = true;
                }
            }

            // Use Terraria's raised first use frame so armor shoulders are oriented correctly;
            // the spear itself still rotates independently through the full cursor aim range.
            Projectile.hide = false;
            Vector2 hand = SwordOfLordGwynPlayerAnimation.SetUseFrame(player, 0);

            // Gentle charge shimmer around the raised hand so the hold reads as "powering up".
            if (Main.rand.NextBool(3))
            {
                int dust = Dust.NewDust(hand - new Vector2(4f), 8, 8, DustID.GoldFlame, 0f, -1.2f, 120, default, 1.05f);
                Main.dust[dust].noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Only the spear-hold state draws anything (the controller is otherwise invisible).
            Player player = Main.player[Projectile.owner];
            if (Mode != 0 || ThrowTimer > 0f || !IsInputHeld(player) || Timer <= TapWindow)
            {
                return false;
            }

            Vector2 hand = SwordOfLordGwynPlayerAnimation.GetHandPosition(player, 0);

            Texture2D tex = ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Enemy/Gwyn/GwynLightningSpear").Value;
            int frameHeight = tex.Height / GwynLightningSpearFrames.FrameCount;
            int frameIndex = (int)(Timer / 5f) % GwynLightningSpearFrames.FrameCount;
            Rectangle frame = new Rectangle(0, frameIndex * frameHeight, tex.Width, frameHeight);
            Vector2 origin = GwynLightningSpearFrames.GetVisualOrigin(frameIndex);
            Vector2 aim = HeldAim(player);
            float rotation = aim.ToRotation();
            SpriteEffects effects = aim.X < 0f ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            if (effects == SpriteEffects.FlipHorizontally)
            {
                origin.X = tex.Width - origin.X;
            }
            Main.EntitySpriteDraw(tex, hand - Main.screenPosition, frame, Color.White, rotation,
                origin, 0.6f, effects, 0);
            return false;
        }

        void CastTap(Player player)
        {
            if (Mode == 0)
            {
                SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.9f, Pitch = -0.25f }, player.Center);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), player.Center, Vector2.Zero,
                    ModContent.ProjectileType<SwordOfLordGwynSlash>(), Projectile.damage, Projectile.knockBack, Projectile.owner, 0f, player.direction);
            }
            else
            {
                CastRisingDash(player);
            }
        }

        bool CastSunlightSpear(Player player)
        {
            if (!player.CheckMana(SwordOfLordGwyn.SpearManaCost, true))
            {
                return false;
            }

            player.manaRegenDelay = 180;
            Vector2 origin = SwordOfLordGwynPlayerAnimation.GetHandPosition(player, 0);
            Vector2 aim = (Main.MouseWorld - origin).SafeNormalize(Vector2.UnitX * player.direction);
            SoundEngine.PlaySound(SoundID.Item92 with { Volume = 0.85f, Pitch = 0.1f }, origin);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), origin, aim * 16f,
                ModContent.ProjectileType<SwordOfLordGwynSunlightSpear>(), (int)(Projectile.damage * 0.95f), Projectile.knockBack * 0.8f, Projectile.owner, player.GetModPlayer<LordGwynSetPlayer>().AboveHalfMana ? 1f : 0f);
            return true;
        }

        void RunThrowFollowThrough(Player player)
        {
            Projectile.hide = true;
            player.heldProj = Projectile.whoAmI;
            player.itemTime = 2;
            player.itemAnimation = 2;

            float progress = MathHelper.Clamp((ThrowTimer - 1f) / ThrowFollowThroughTicks, 0f, 0.999f);
            SwordOfLordGwynPlayerAnimation.SetUseFrame(player, (int)(progress * 4f));

            ThrowTimer++;
            if (ThrowTimer > ThrowFollowThroughTicks)
            {
                Projectile.Kill();
            }
        }

        void CastRisingDash(Player player)
        {
            if (Main.myPlayer == Projectile.owner && !player.CheckMana(SwordOfLordGwyn.DashManaCost, true))
            {
                return;
            }

            player.manaRegenDelay = 180;
            bool empowered = player.GetModPlayer<LordGwynSetPlayer>().AboveHalfMana;
            player.direction = Main.MouseWorld.X > player.Center.X ? 1 : -1;
            player.velocity = new Vector2(player.direction * 17f, -8.25f);
            player.immune = true;
            player.immuneTime = System.Math.Max(player.immuneTime, DashInvulnerabilityTicks);
            player.ResetMeleeHitCooldowns();

            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.75f, Pitch = -0.35f }, player.Center);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), player.Center, Vector2.Zero,
                ModContent.ProjectileType<SwordOfLordGwynSlash>(), (int)(Projectile.damage * (empowered ? 1.55f : 1.35f)), Projectile.knockBack * (empowered ? 2.1f : 1.8f), Projectile.owner, 1f, player.direction, empowered ? 1f : 0f);

            if (Main.netMode != NetmodeID.SinglePlayer)
            {
                NetMessage.SendData(MessageID.SyncPlayer, -1, -1, null, player.whoAmI);
            }
        }

        void RunCinderNovaCharge(Player player, bool channeling)
        {
            float charge = Timer - TapWindow;
            if (!channeling && charge < NovaChargeTicks)
            {
                for (int i = 0; i < 10; i++)
                {
                    int dust = Dust.NewDust(player.position, player.width, player.height, DustID.GoldFlame, 0f, -1f, 100, default, 1.2f);
                    Main.dust[dust].noGravity = true;
                }
                Projectile.Kill();
                return;
            }

            float progress = MathHelper.Clamp(charge / NovaChargeTicks, 0f, 1f);
            for (int i = 0; i < 2 + (int)(progress * 4f); i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                float radius = MathHelper.Lerp(110f, 20f, progress) + Main.rand.NextFloat(18f);
                Vector2 pos = player.Center + angle.ToRotationVector2() * radius;
                int type = Main.rand.NextBool() ? DustID.Torch : DustID.GoldFlame;
                int dust = Dust.NewDust(pos, 4, 4, type, 0f, 0f, 70, default, 1.25f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = (player.Center - pos) * 0.08f;
            }
            Lighting.AddLight(player.Center, 0.8f * progress, 0.45f * progress, 0.15f * progress);

            if (charge >= NovaChargeTicks)
            {
                if (Main.myPlayer == Projectile.owner && player.CheckMana(SwordOfLordGwyn.NovaManaCost, true))
                {
                    player.manaRegenDelay = 180;
                    SoundEngine.PlaySound(SoundID.Item74 with { Volume = 1f, Pitch = -0.55f }, player.Center);
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), player.Center, Vector2.Zero,
                        ModContent.ProjectileType<SwordOfLordGwynCinderNova>(), (int)(Projectile.damage * 1.65f), Projectile.knockBack * 1.6f, Projectile.owner, player.GetModPlayer<LordGwynSetPlayer>().AboveHalfMana ? 430f : 360f);
                }
                Projectile.Kill();
            }
        }
    }
}
