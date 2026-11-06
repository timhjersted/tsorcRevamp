using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Armor;
using tsorcRevamp.Content.Items.Weapons.Melee.Broadswords;
using tsorcRevamp.Content.Projectiles.Enemy.Gwyn;
using SwordOfLordGwyn = tsorcRevamp.Content.Items.Weapons.Melee.Broadswords.SwordOfLordGwyn;

namespace tsorcRevamp.Content.Projectiles.Melee.Broadswords
{
    class SwordOfLordGwynController : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

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

            // A hold that began mid-combo waits for the swing in progress to finish, so the spear or nova charge
            // never appears in the middle of a cut. Letting go before then still counts as a tap.
            if (player.ownedProjectileCounts[ModContent.ProjectileType<GreatswordSwing>()] > 0)
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

            // Keep the sunlight-spear branch visually electric rather than borrowing fire from
            // the sword's normal cinder attacks.
            if (Main.rand.NextBool(3))
            {
                Vector2 velocity = Main.rand.NextVector2Circular(1.4f, 1.4f) - Vector2.UnitY * 0.65f;
                int dust = Dust.NewDust(hand - new Vector2(4f), 8, 8, DustID.Electric,
                    velocity.X, velocity.Y, 90, new Color(255, 226, 116), Main.rand.NextFloat(0.65f, 0.95f));
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

            Texture2D tex = ModContent.Request<Texture2D>(UsefulFunctions.RefactorableFilepath(typeof(GwynLightningSpear))).Value;
            int frameHeight = tex.Height / GwynLightningSpearFrames.FrameCount;
            int frameIndex = (int)(Timer / 5f) % GwynLightningSpearFrames.FrameCount;
            Rectangle frame = new Rectangle(0, frameIndex * frameHeight, tex.Width, frameHeight);
            Vector2 origin = GwynLightningSpearFrames.GetVisualOrigin(frameIndex);
            Vector2 aim = HeldAim(player);
            // The sprite's head points RIGHT (+X): rotating to the aim alone points it at the cursor either way.
            float rotation = aim.ToRotation();
            Main.EntitySpriteDraw(tex, hand - Main.screenPosition, frame, Color.White, rotation,
                origin, 0.6f, SpriteEffects.None, 0);
            return false;
        }

        void CastTap(Player player)
        {
            if (Mode == 0)
            {
                // Next step of the greatsword combo. A tap during a swing was already paid for by this item use,
                // so it is queued and starts the moment the current swing ends.
                if (player.HeldItem.ModItem is not SwordOfLordGwyn sword)
                {
                    return;
                }

                if (player.ownedProjectileCounts[ModContent.ProjectileType<GreatswordSwing>()] > 0)
                {
                    player.GetModPlayer<GreatswordComboPlayer>().QueueSwing();
                    return;
                }

                int facing = 1;
                if (Main.MouseWorld.X < player.Center.X)
                {
                    facing = -1;
                }

                sword.StartSwing(player, Projectile.GetSource_FromThis(), Projectile.damage, Projectile.knockBack, facing);
            }
            else
            {
                // The dash cancels a combo swing still in progress rather than carrying it along.
                int swingType = ModContent.ProjectileType<GreatswordSwing>();
                foreach (Projectile swing in Main.ActiveProjectiles)
                {
                    if (swing.owner == player.whoAmI && swing.type == swingType)
                    {
                        swing.Kill();
                    }
                }

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
            // Rolling FireBreath flames spiral in toward Gwyn, more and larger as the charge fills.
            if (Main.rand.NextFloat() < 0.35f + progress * 0.65f)
            {
                float flameAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                float flameRadius = MathHelper.Lerp(96f, 40f, progress) + Main.rand.NextFloat(16f);
                Vector2 inward = -flameAngle.ToRotationVector2();
                Vector2 tangent = inward.RotatedBy(MathHelper.PiOver2);
                Vector2 flamePosition = player.Center - inward * flameRadius;
                Vector2 flameVelocity = inward * flameRadius * 0.05f + tangent * 1.2f;
                float flameScale = MathHelper.Lerp(0.45f, 0.85f, progress);
                global::tsorcRevamp.VFX.VFX.FlameLobe(flamePosition, flameVelocity, flameScale, 16, 0f);
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
