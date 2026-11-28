using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using HeartOfWinterItem = tsorcRevamp.Content.Items.Weapons.Magic.HeartOfWinter;
using tsorcRevamp.Systems.ArcaneSorcery;

namespace tsorcRevamp.Content.Projectiles.Magic.HeartOfWinter
{
    ///<summary>
    ///Heart of Winter's held brain: spawned on every use, watches whether the button is TAPPED
    ///(released within 18 ticks) or HELD, and routes to the right spell. ai[0] = 0 for cursor high
    ///(tap: icicle trio / hold: frost breath channel), 1 for cursor low (tap: cursor ground
    ///spikes / hold: charged freeze ring). Deals no damage itself — children inherit its damage.
    ///</summary>
    class HeartOfWinterController : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        const int TapWindow = 18;           // released before this = the tap spell
        const int ChargeTicks = 60;         // cursor-low hold: charge time after the window before the nova
        const int BreathManaInterval = 9;   // base ticks between breath mana/stamina upkeep pulses
        const int BreathSpawnInterval = 3;  // base ticks between frost puff volleys
        const int BreathManaCost = 6;       // per breath upkeep pulse
        const int NovaManaCost = 20;        // surcharge on releasing the freeze ring

        int Mode => (int)Projectile.ai[0];
        bool UsesRightClick => Projectile.ai[1] == 1f;
        float Timer => Projectile.localAI[0];

        // ai[2] snapshots effective use time, including prefixes and attack-speed hooks.
        float EffectiveUseTime => Projectile.ai[2] > 0f ? Projectile.ai[2] : HeartOfWinterItem.BaseUseTime;
        int UseTicks => Math.Max(1, (int)EffectiveUseTime);
        int ScaledChargeTicks => ScaleInterval(ChargeTicks);
        int ScaledBreathManaInterval => ScaleInterval(BreathManaInterval);
        int ScaledBreathSpawnInterval => ScaleInterval(BreathSpawnInterval);

        bool castFinished;
        bool novaRepeating;

        int ScaleInterval(int ticks)
        {
            return Math.Max(1, (int)(ticks * EffectiveUseTime / HeartOfWinterItem.BaseUseTime));
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Timer);
            writer.Write(castFinished);
            writer.Write(novaRepeating);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.localAI[0] = reader.ReadSingle();
            castFinished = reader.ReadBoolean();
            novaRepeating = reader.ReadBoolean();
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
                if (Main.myPlayer == Projectile.owner && player.HeldItem.type == ModContent.ItemType<HeartOfWinterItem>())
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

            // Within the tap window: releasing casts the tap spell.
            if (!novaRepeating && Timer <= TapWindow)
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
                RunBreathChannel(player, channeling);
            }
            else
            {
                RunNovaCharge(player, channeling);
            }
        }

        #region Tap spells

        void CastTap(Player player)
        {
            if (Mode == 0)
            {
                // Icicle Crown: three icicles form overhead and lance at the cursor in sequence.
                SoundEngine.PlaySound(SoundID.Item28 with { Volume = 0.8f, Pitch = 0.2f }, player.Center);
                Span<Vector2> offsets = stackalloc Vector2[]
                {
                    new Vector2(-30f, -46f),
                    new Vector2(0f, -56f),
                    new Vector2(30f, -46f)
                };

                for (int i = 0; i < 3; i++)
                {
                    Vector2 spawnPos = player.Center + offsets[i];
                    Vector2 aimDir = (Main.MouseWorld - spawnPos).SafeNormalize(Vector2.UnitX * player.direction);
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawnPos, aimDir,
                        ModContent.ProjectileType<TomeIcicle>(), Projectile.damage, Projectile.knockBack,
                        Projectile.owner, 4f + i * 5f);
                }
            }
            else
            {
                // Rimefang: ground spikes erupt at the cursor (clamped to ~40 tiles of reach).
                Vector2 cursor = Main.MouseWorld;
                if (Vector2.Distance(cursor, player.Center) > 640f)
                {
                    cursor = player.Center + (cursor - player.Center).SafeNormalize(Vector2.UnitX) * 640f;
                }

                float groundY = FindGroundY(cursor - new Vector2(0f, 32f), 20);
                if (groundY < 0f)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        int dustIndex = Dust.NewDust(cursor - new Vector2(8f, 8f), 16, 16, DustID.Frost, 0f, 0f, 60, default, 1f);
                        Main.dust[dustIndex].noGravity = true;
                    }

                    return;
                }

                SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.7f, Pitch = -0.2f }, cursor);
                for (int i = -1; i <= 1; i++)
                {
                    float x = cursor.X + i * 26f;
                    float colGround = FindGroundY(new Vector2(x, groundY - 40f), 8);
                    if (colGround < 0f)
                    {
                        continue;
                    }

                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), new Vector2(x, colGround - 27f), Vector2.Zero,
                        ModContent.ProjectileType<TomeIceSpike>(), Projectile.damage, Projectile.knockBack,
                        Projectile.owner, 6f + (i + 1) * 4f, 1f);
                }
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

        void RunBreathChannel(Player player, bool channeling)
        {
            if (!channeling)
            {
                FinishCast(player, channeling);
                return;
            }

            if (Main.myPlayer == Projectile.owner)
            {
                int breathTicks = (int)Timer - TapWindow;
                int manaInterval = ScaledBreathManaInterval;

                if (breathTicks > 0 && breathTicks % manaInterval == 0)
                {
                    if (!player.CheckMana(BreathManaCost, true))
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
                            int baseUseTime = heldItem.type == ModContent.ItemType<HeartOfWinterItem>() ? heldItem.useAnimation : HeartOfWinterItem.BaseUseTime;
                            int scaledUseAnimation = Math.Max(1, (int)(baseUseTime / player.GetAttackSpeed(DamageClass.Magic)));
                            float staminaMult = tsorcPlayer.WeaponStaminaMult(heldItem);
                            float pulseStaminaCost = tsorcRevampPlayer.ReduceStamina(scaledUseAnimation) * staminaMult * ((float)manaInterval / scaledUseAnimation);
                            staminaPlayer.staminaResourceCurrent -= pulseStaminaCost;
                        }
                    }
                }

                int spawnInterval = ScaledBreathSpawnInterval;
                if (breathTicks % spawnInterval == 0)
                {
                    Vector2 mouth = player.Center + new Vector2(player.direction * 14f, -4f);
                    Vector2 aimDir = (Main.MouseWorld - mouth).SafeNormalize(Vector2.UnitX * player.direction);
                    int puffDamage = (int)(Projectile.damage * 0.5f);

                    for (int i = 0; i < 2; i++)
                    {
                        Vector2 vel = aimDir.RotatedBy(Main.rand.NextFloat(-0.12f, 0.12f)) * (7.5f + Main.rand.NextFloat(-1f, 1f));
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), mouth, vel,
                            ModContent.ProjectileType<TomeFrostPuff>(), puffDamage, Projectile.knockBack * 0.3f, Projectile.owner);
                    }
                }
            }

            if (!Main.dedServ && (int)Timer % 20 == 0)
            {
                SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.35f, Pitch = -0.3f }, player.Center);
            }
        }

        void RunNovaCharge(Player player, bool channeling)
        {
            // A continuous hold has already passed input detection; subsequent charges start immediately.
            float charge = Timer - (novaRepeating ? 0 : TapWindow);
            int targetChargeTicks = ScaledChargeTicks;

            if (!channeling && charge < targetChargeTicks)
            {
                if (!Main.dedServ)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        int dustIndex = Dust.NewDust(player.position, player.width, player.height, DustID.Frost, 0f, -1f, 60, default, 1f);
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
                    int dustIndex = Dust.NewDust(pos, 4, 4, DustID.Frost, 0f, 0f, 100, default, 1.1f);
                    Main.dust[dustIndex].noGravity = true;
                    Main.dust[dustIndex].velocity = (player.Center - pos) * 0.08f;
                }

                Lighting.AddLight(player.Center, 0.2f * progress, 0.35f * progress, 0.5f * progress);
            }

            if (charge >= targetChargeTicks)
            {
                if (Main.myPlayer != Projectile.owner)
                {
                    return;
                }

                if (player.CheckMana(NovaManaCost, true))
                {
                    SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.9f, Pitch = -0.6f }, player.Center);
                    int ringDamage = (int)(Projectile.damage * 1.5f);
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), player.Center, Vector2.Zero,
                        ModContent.ProjectileType<TomeFreezeRing>(), ringDamage, Projectile.knockBack * 1.5f,
                        Projectile.owner, 260f);

                    // Each repeated cast also pays the item-use mana and stamina charged when spawning a controller.
                    tsorcRevampPlayer tsorcPlayer = player.GetModPlayer<tsorcRevampPlayer>();
                    tsorcRevampStaminaPlayer staminaPlayer = player.GetModPlayer<tsorcRevampStaminaPlayer>();
                    bool canRepeat = channeling
                        && player.HeldItem.type == ModContent.ItemType<HeartOfWinterItem>()
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

                        novaRepeating = true;
                        Projectile.localAI[0] = 0f;
                        Projectile.netUpdate = true;
                        return;
                    }
                }

                FinishCast(player, channeling);
            }
        }

        #endregion

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
