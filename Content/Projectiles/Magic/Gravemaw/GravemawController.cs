using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Magic;

namespace tsorcRevamp.Content.Projectiles.Magic.Gravemaw
{
    ///<summary>
    ///The Gravemaw Tome's held brain (modelled on HeartOfWinterController). Watches TAP (released within
    ///18 ticks) vs HOLD and routes to the four casts. ai[0] = 0 cursor high, 1 cursor low. Deals no
    ///damage itself — children inherit its damage.
    ///</summary>
    class GravemawController : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        const int TapWindow = 18;
        const int ChargeTicks = 55;      // cursor-high hold: charge before the Reliquary Nova
        const int NovaManaCost = 16;

        int Mode => (int)Projectile.ai[0];
        bool UsesRightClick => Projectile.ai[1] == 1f;
        float Timer => Projectile.localAI[0];
        // ai[2] snapshots effective use time, including prefixes and use-speed hooks.
        float EffectiveUseTime => Projectile.ai[2] > 0f ? Projectile.ai[2] : GravemawTome.BaseUseTime;
        int UseTicks => Math.Max(1, (int)EffectiveUseTime);
        int ScaledChargeTicks => ScaleInterval(ChargeTicks);
        bool castFinished;
        bool novaRepeating;

        int ScaleInterval(int ticks) => Math.Max(1, (int)(ticks * EffectiveUseTime / GravemawTome.BaseUseTime));

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

        public override bool? CanDamage() => false;
        public float LeftTapDmgMod = 0.5f;
        public float LeftHoldDmgMod = 2.5f;
        public float RightTapDmgMod = 0.4f;
        public float RightHoldDmgMod = 0.55f;

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (!player.active || player.dead || player.noItems || player.CCed) { Projectile.Kill(); return; }
            Projectile.Center = player.Center;
            Projectile.timeLeft = 60;
            Projectile.localAI[0]++;
            bool channeling = UsesRightClick ? player.controlUseTile : player.channel;

            // A resolved cast can only wait out its cooldown; repressing cannot turn it into a hold.
            if (castFinished)
            {
                int remaining = Math.Max(channeling ? 2 : 0, UseTicks - (int)Timer);
                if (Main.myPlayer == Projectile.owner && player.HeldItem.type == ModContent.ItemType<GravemawTome>())
                {
                    player.itemTime = remaining;
                    player.itemAnimation = remaining;
                }
                if (Main.myPlayer == Projectile.owner && !channeling && Timer >= UseTicks)
                    Projectile.Kill();
                return;
            }

            if (channeling)
            {
                player.heldProj = Projectile.whoAmI;
                player.itemTime = 2;
                player.itemAnimation = 2;
                if (Main.myPlayer == Projectile.owner)
                    player.ChangeDir(Main.MouseWorld.X > player.Center.X ? 1 : -1);
            }

            // Tap window: releasing casts the tap spell.
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

            if (Mode == 0) RunNovaCharge(player, channeling);
            else RunMawHold(player, channeling);
        }

        #region Tap casts

        void CastTap(Player player)
        {
            if (Mode == 0)
            {
                // Soulspit: a fan of homing soul-skulls toward the cursor.
                SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.7f, Pitch = 0.3f }, player.Center);
                Vector2 origin = player.Center;
                float baseAng = (Main.MouseWorld - origin).ToRotation();
                int dmg = (int)(Projectile.damage * LeftTapDmgMod);
                for (int i = -1; i <= 1; i++)
                {
                    Vector2 vel = (baseAng + MathHelper.ToRadians(i * 11f)).ToRotationVector2() * 10f;
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), origin, vel,
                        ModContent.ProjectileType<GravemawSoulBolt>(), dmg, Projectile.knockBack, Projectile.owner, 0.04f);
                }
            }
            else
            {
                // Gravemaw Orb: lob one orb toward the cursor.
                SoundEngine.PlaySound(SoundID.NPCDeath6 with { Volume = 0.6f, Pitch = -0.2f }, player.Center);
                Vector2 vel = (Main.MouseWorld - player.Center).SafeNormalize(Vector2.UnitX * player.direction) * 8f;
                int dmg = (int)(Projectile.damage * RightTapDmgMod);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), player.Center, vel,
                    ModContent.ProjectileType<GravemawOrb>(), dmg, Projectile.knockBack, Projectile.owner);
            }
        }

        #endregion

        #region Hold casts

        void FinishCast(Player player, bool channeling)
        {
            if (Main.myPlayer != Projectile.owner) return;
            castFinished = true;
            Projectile.netUpdate = true;
            int remaining = Math.Max(channeling ? 2 : 0, UseTicks - (int)Timer);
            player.itemTime = remaining;
            player.itemAnimation = remaining;
            if (!channeling && Timer >= UseTicks) Projectile.Kill();
        }

        // Cursor high hold — charge, then release the expanding Reliquary Nova.
        void RunNovaCharge(Player player, bool channeling)
        {
            // A continuous hold has already passed input detection; subsequent charges start immediately.
            float charge = Timer - (novaRepeating ? 0 : TapWindow);
            if (!channeling && charge < ScaledChargeTicks)
            {
                for (int i = 0; i < 8; i++)
                {
                    int d = Dust.NewDust(player.position, player.width, player.height, DustID.PurpleTorch, 0f, -1f, 130, default, 1f);
                    Main.dust[d].noGravity = true;
                }
                FinishCast(player, channeling);
                return;
            }
            float progress = MathHelper.Min(1f, charge / ScaledChargeTicks);
            int count = 1 + (int)(progress * 3f);
            for (int i = 0; i < count; i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                float radius = MathHelper.Lerp(90f, 15f, progress) + Main.rand.NextFloat(12f);
                Vector2 pos = player.Center + angle.ToRotationVector2() * radius;
                int d = Dust.NewDust(pos, 4, 4, Main.rand.NextBool(3) ? DustID.Shadowflame : DustID.PurpleTorch, 0f, 0f, 100, default, 1.1f);
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity = (player.Center - pos) * 0.08f;
            }
            Lighting.AddLight(player.Center, 0.4f * progress, 0.1f * progress, 0.55f * progress);

            if (charge >= ScaledChargeTicks)
            {
                if (Main.myPlayer != Projectile.owner) return;
                if (player.CheckMana(NovaManaCost, true))
                {
                    SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.9f, Pitch = 0.1f }, player.Center);
                    int dmg = (int)(Projectile.damage * LeftHoldDmgMod);
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), player.Center, Vector2.Zero,
                        ModContent.ProjectileType<GravemawNova>(), dmg, Projectile.knockBack, Projectile.owner, 360f);

                    // Each repeated cast also pays the item-use mana normally charged when spawning a controller.
                    if (channeling && player.HeldItem.type == ModContent.ItemType<GravemawTome>()
                        && player.CheckMana(player.HeldItem, pay: true))
                    {
                        novaRepeating = true;
                        Projectile.localAI[0] = 0f;
                        Projectile.netUpdate = true;
                        return;
                    }
                }
                FinishCast(player, channeling);
            }
        }

        // Cursor low hold — hand off to the persistent Hungering Maw channel projectile.
        void RunMawHold(Player player, bool channeling)
        {
            if (!channeling) { FinishCast(player, channeling); return; }
            if (Main.myPlayer == Projectile.owner
                && player.ownedProjectileCounts[ModContent.ProjectileType<GravemawMaw>()] == 0)
            {
                int dmg = (int)(Projectile.damage * RightHoldDmgMod);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), player.Center, Vector2.Zero,
                    ModContent.ProjectileType<GravemawMaw>(), dmg,
                    Projectile.knockBack * 0.2f, Projectile.owner, UsesRightClick ? 1f : 0f,
                    ScaleInterval(12));
            }
        }

        #endregion
    }
}
