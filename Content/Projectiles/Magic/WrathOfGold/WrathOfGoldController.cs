using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using WrathOfGoldItem = tsorcRevamp.Content.Items.Weapons.Magic.WrathOfGold;
using tsorcRevamp.Systems.ArcaneSorcery;

namespace tsorcRevamp.Content.Projectiles.Magic.WrathOfGold
{
    ///<summary>
    ///Wrath of Gold's held brain: watches whether the button is TAPPED (released within 18 ticks)
    ///or HELD, and routes to the right spell. ai[0] = 0 for cursor high (tap: heavenly spears at the
    ///cursor / hold: halo sun channel), 1 for cursor low (tap: sun pillar at the cursor / hold:
    ///charged golden sweep). Deals no damage itself — children inherit its damage.
    ///</summary>
    class WrathOfGoldController : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        const int TapWindow = 18;
        const int ChargeTicks = 50;    // cursor-low hold: charge time after the window before the sweep
        const int HaloSpawnInterval = 15;
        const int HaloManaCost = 7;    // per sun spawned during the cursor-high hold channel
        const int SweepManaCost = 25;  // surcharge on releasing the sweep
        const int MaxHaloSuns = 8;

        int Mode => (int)Projectile.ai[0];
        bool UsesRightClick => Projectile.ai[1] == 1f;
        float Timer => Projectile.localAI[0];

        // ai[2] snapshots effective use time, including prefixes and attack-speed hooks.
        float EffectiveUseTime => Projectile.ai[2] > 0f ? Projectile.ai[2] : WrathOfGoldItem.BaseUseTime;
        int UseTicks => Math.Max(1, (int)EffectiveUseTime);
        int ScaledChargeTicks => ScaleInterval(ChargeTicks);
        int ScaledHaloSpawnInterval => ScaleInterval(HaloSpawnInterval);

        bool castFinished;
        bool sweepRepeating;

        int ScaleInterval(int ticks)
        {
            return Math.Max(1, (int)(ticks * EffectiveUseTime / WrathOfGoldItem.BaseUseTime));
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Timer);
            writer.Write(castFinished);
            writer.Write(sweepRepeating);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.localAI[0] = reader.ReadSingle();
            castFinished = reader.ReadBoolean();
            sweepRepeating = reader.ReadBoolean();
        }

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
            Projectile.DamageType = DamageClass.Magic;
        }

        public override bool? CanDamage()
        {
            return false;
        }

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
            bool channeling = UsesRightClick ? player.controlUseTile : player.channel;

            // A resolved cast waits out its tap cooldown; repressing cannot convert it into a hold.
            if (castFinished)
            {
                int remaining = Math.Max(channeling ? 2 : 0, UseTicks - (int)Timer);
                if (Main.myPlayer == Projectile.owner && player.HeldItem.type == ModContent.ItemType<WrathOfGoldItem>())
                {
                    player.itemTime = remaining;
                    player.itemAnimation = remaining;
                }

                if (Main.myPlayer == Projectile.owner && !channeling && Timer >= UseTicks)
                {
                    Projectile.Kill();
                }

                return;
            }

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

            if (!sweepRepeating && Timer <= TapWindow)
            {
                if (!channeling)
                {
                    if (Main.myPlayer == Projectile.owner)
                    {
                        CastTap(player);
                        FinishCast(player, channeling);
                    }
                }

                return;
            }

            if (Mode == 0)
            {
                RunHaloChannel(player, channeling);
            }
            else
            {
                RunSweepCharge(player, channeling);
            }
        }

        #region Tap spells

        void CastTap(Player player)
        {
            if (Mode == 0)
            {
                // Heavenly Spears: three golden spears form over the cursor and lance down onto it.
                Vector2 cursor = Main.MouseWorld;
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.8f, Pitch = -0.2f }, player.Center);
                Span<Vector2> offsets = stackalloc Vector2[]
                {
                    new Vector2(-60f, -170f),
                    new Vector2(0f, -200f),
                    new Vector2(60f, -170f)
                };

                for (int i = 0; i < 3; i++)
                {
                    Vector2 spawnPos = cursor + offsets[i];
                    for (int tries = 0; tries < 12 && IsSolidAt(spawnPos); tries++)
                    {
                        spawnPos.Y += 16f;
                    }

                    Vector2 aimDir = (cursor - spawnPos).SafeNormalize(Vector2.UnitY);
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawnPos, aimDir,
                        ModContent.ProjectileType<TomeHeavenlySpear>(), Projectile.damage, Projectile.knockBack,
                        Projectile.owner, 18f + i * 8f);
                }
            }
            else
            {
                // Sun Pillar: a column of judgment light at the cursor's ground.
                Vector2 cursor = Main.MouseWorld;
                float groundY = FindGroundY(cursor - new Vector2(0f, 16f), 25);
                if (groundY < 0f)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        int dustIndex = Dust.NewDust(cursor - new Vector2(8f, 8f), 16, 16, DustID.GoldFlame, 0f, 0f, 60, default, 1f);
                        Main.dust[dustIndex].noGravity = true;
                    }

                    return;
                }

                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.7f, Pitch = -0.4f }, cursor);
                int pillarDamage = (int)(Projectile.damage * 0.9f);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(),
                    new Vector2(cursor.X, groundY - TomeSunPillar.PillarHeight / 2f), Vector2.Zero,
                    ModContent.ProjectileType<TomeSunPillar>(), pillarDamage, Projectile.knockBack,
                    Projectile.owner, 20f);
            }
        }

        #endregion

        #region Hold spells

        void FinishCast(Player player, bool channeling)
        {
            if (Main.myPlayer != Projectile.owner)
            {
                return;
            }

            castFinished = true;
            Projectile.netUpdate = true;

            int remaining = Math.Max(channeling ? 2 : 0, UseTicks - (int)Timer);
            player.itemTime = remaining;
            player.itemAnimation = remaining;

            if (!channeling && Timer >= UseTicks)
            {
                Projectile.Kill();
            }
        }

        void RunHaloChannel(Player player, bool channeling)
        {
            if (!channeling)
            {
                FinishCast(player, channeling);
                return;
            }

            int haloInterval = ScaledHaloSpawnInterval;
            int holdTicks = (int)Timer - TapWindow;

            if (Main.myPlayer == Projectile.owner && holdTicks > 0 && holdTicks % haloInterval == 0
                && player.ownedProjectileCounts[ModContent.ProjectileType<TomeHaloSun>()] < MaxHaloSuns)
            {
                if (!player.CheckMana(HaloManaCost, true))
                {
                    FinishCast(player, false);
                    return;
                }

                tsorcRevampPlayer tsorcPlayer = player.GetModPlayer<tsorcRevampPlayer>();
                tsorcRevampStaminaPlayer staminaPlayer = player.GetModPlayer<tsorcRevampStaminaPlayer>();

                if (!staminaPlayer.CanUseWeapon)
                {
                    FinishCast(player, false);
                    return;
                }

                if (tsorcPlayer.UsesWeaponStamina)
                {
                    bool manaBurnActive = player.HasBuff(ModContent.BuffType<ManaBurn>());
                    bool arcaneBurnThreshold = player.HasBuff(ModContent.BuffType<ArcaneSorcery>())
                        && staminaPlayer.staminaResourceCurrent <= staminaPlayer.staminaResourceMax2 * ArcaneSorceryPlayer.ManaBurnStaminaThreshold / 100f;

                    if (!manaBurnActive && !arcaneBurnThreshold)
                    {
                        Item heldItem = player.HeldItem;
                        int baseUseTime = heldItem.type == ModContent.ItemType<WrathOfGoldItem>() ? heldItem.useAnimation : WrathOfGoldItem.BaseUseTime;
                        int scaledUseAnimation = Math.Max(1, (int)(baseUseTime / player.GetAttackSpeed(DamageClass.Magic)));
                        float staminaMult = tsorcPlayer.WeaponStaminaMult(heldItem);
                        float pulseStaminaCost = tsorcRevampPlayer.ReduceStamina(scaledUseAnimation) * staminaMult * ((float)haloInterval / scaledUseAnimation);
                        staminaPlayer.staminaResourceCurrent -= pulseStaminaCost;
                    }
                }

                int slot = (holdTicks / haloInterval) % 6;
                int sunDamage = (int)(Projectile.damage * 0.9f);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), player.Center + new Vector2(0f, -60f), Vector2.Zero,
                    ModContent.ProjectileType<TomeHaloSun>(), sunDamage, Projectile.knockBack * 0.5f, Projectile.owner, slot);
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.4f, Pitch = 0.4f }, player.Center);
            }

            if (!Main.dedServ && Main.rand.NextBool(3))
            {
                int sparkle = Dust.NewDust(player.position, player.width, player.height, DustID.GoldCoin, 0f, -1f, 0, default, 0.9f);
                Main.dust[sparkle].noGravity = true;
                Main.dust[sparkle].velocity *= 0.3f;
            }
        }

        void RunSweepCharge(Player player, bool channeling)
        {
            // A continuous hold has already passed input detection; subsequent charges start immediately.
            float charge = Timer - (sweepRepeating ? 0 : TapWindow);
            int targetChargeTicks = ScaledChargeTicks;

            if (!channeling && charge < targetChargeTicks)
            {
                if (!Main.dedServ)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        int dustIndex = Dust.NewDust(player.position, player.width, player.height, DustID.GoldFlame, 0f, -1f, 60, default, 1f);
                        Main.dust[dustIndex].noGravity = true;
                    }
                }

                FinishCast(player, channeling);
                return;
            }

            float progress = MathHelper.Min(1f, charge / targetChargeTicks);
            if (!Main.dedServ)
            {
                int count = 1 + (int)(progress * 3f);
                for (int i = 0; i < count; i++)
                {
                    float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                    float radius = MathHelper.Lerp(90f, 15f, progress) + Main.rand.NextFloat(12f);
                    Vector2 pos = player.Center + angle.ToRotationVector2() * radius;
                    int dustIndex = Dust.NewDust(pos, 4, 4, DustID.GoldFlame, 0f, 0f, 100, default, 1.1f);
                    Main.dust[dustIndex].noGravity = true;
                    Main.dust[dustIndex].velocity = (player.Center - pos) * 0.08f;
                }

                Lighting.AddLight(player.Center, 0.5f * progress, 0.42f * progress, 0.18f * progress);
            }

            if (charge >= targetChargeTicks)
            {
                if (Main.myPlayer != Projectile.owner)
                {
                    return;
                }

                if (player.CheckMana(SweepManaCost, true))
                {
                    SoundEngine.PlaySound(SoundID.Thunder with { Volume = 0.4f, Pitch = 0.3f }, player.Center);
                    int facingDir = Main.MouseWorld.X > player.Center.X ? 1 : -1;
                    Vector2 beamCenter = new Vector2(
                        player.Center.X + facingDir * (20f + TomeGoldSweep.BeamLength / 2f),
                        player.Bottom.Y - TomeGoldSweep.BeamHeight / 2f);
                    int sweepDamage = (int)(Projectile.damage * 1.6f);

                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), beamCenter, Vector2.Zero,
                        ModContent.ProjectileType<TomeGoldSweep>(), sweepDamage, Projectile.knockBack * 1.5f,
                        Projectile.owner, facingDir);

                    // Each repeated cast also pays the item-use mana and stamina charged when spawning a controller.
                    tsorcRevampPlayer tsorcPlayer = player.GetModPlayer<tsorcRevampPlayer>();
                    tsorcRevampStaminaPlayer staminaPlayer = player.GetModPlayer<tsorcRevampStaminaPlayer>();
                    bool canRepeat = channeling
                        && player.HeldItem.type == ModContent.ItemType<WrathOfGoldItem>()
                        && staminaPlayer.CanUseWeapon
                        && player.CheckMana(player.HeldItem, pay: true);

                    if (canRepeat)
                    {
                        if (tsorcPlayer.UsesWeaponStamina)
                        {
                            bool manaBurnActive = player.HasBuff(ModContent.BuffType<ManaBurn>());
                            bool arcaneBurnThreshold = player.HasBuff(ModContent.BuffType<ArcaneSorcery>())
                                && staminaPlayer.staminaResourceCurrent <= staminaPlayer.staminaResourceMax2 * ArcaneSorceryPlayer.ManaBurnStaminaThreshold / 100f;

                            if (!manaBurnActive && !arcaneBurnThreshold)
                            {
                                Item heldItem = player.HeldItem;
                                float staminaMult = tsorcPlayer.WeaponStaminaMult(heldItem);
                                int scaledUseAnimation = (int)(heldItem.useAnimation / player.GetAttackSpeed(heldItem.DamageType));
                                staminaPlayer.staminaResourceCurrent -= tsorcRevampPlayer.ReduceStamina(scaledUseAnimation) * staminaMult;
                            }
                        }

                        sweepRepeating = true;
                        Projectile.localAI[0] = 0f;
                        Projectile.netUpdate = true;
                        return;
                    }
                }

                FinishCast(player, channeling);
            }
        }

        #endregion

        static bool IsSolidAt(Vector2 worldPos)
        {
            int tileX = (int)(worldPos.X / 16f);
            int tileY = (int)(worldPos.Y / 16f);
            if (tileX < 5 || tileX > Main.maxTilesX - 5 || tileY < 5 || tileY > Main.maxTilesY - 5)
            {
                return true;
            }

            Tile tile = Main.tile[tileX, tileY];
            return tile.HasTile && !tile.IsActuated && Main.tileSolid[tile.TileType];
        }

        static float FindGroundY(Vector2 worldPos, int maxTilesDown)
        {
            int tileX = (int)(worldPos.X / 16f);
            int tileY = (int)(worldPos.Y / 16f);
            if (tileX < 5 || tileX > Main.maxTilesX - 5)
            {
                return -1f;
            }

            for (int step = 0; step <= maxTilesDown; step++)
            {
                int y = tileY + step;
                if (y >= Main.maxTilesY - 5)
                {
                    break;
                }

                Tile tile = Main.tile[tileX, y];
                if (tile.HasTile && !tile.IsActuated && Main.tileSolid[tile.TileType])
                {
                    return y * 16f;
                }
            }

            return -1f;
        }
    }
}
