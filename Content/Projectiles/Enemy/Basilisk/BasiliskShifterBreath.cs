using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Content.Projectiles.Enemy.Basilisk
{
    /// <summary>
    /// Basilisk Shifter's plum curse breath (was the shared EnemyCursedBreathCollides). A visible blob inside
    /// plum flame dust. Stopped by tiles. Hits apply Cursed Flames; standing in it builds powerful curse every tick.
    /// </summary>
    class BasiliskShifterBreath : ModProjectile
    {
        public const int CursedFlamesTicks = 3 * 60;
        // Powerful curse meter is 500 and decays 2/s, so 2/tick fills it in ~4.2s of continuous contact.
        public const int PowerfulCurseBuildupPerTick = 2;
        public const int PowerfulCurseMarkerTicks = 600 * 60;

        static ulong nextTileImpactSoundFrame;
        // Last tick the local player gained breath curse. The breath spews one projectile per tick and they overlap,
        // so this caps the buildup at one award per tick no matter how many puffs touch the player. Local player only.
        static ulong lastPowerfulCurseTick;

        // PLACEHOLDER sprite (copy of EnemyCursedBreathCollides, still green) — replace with the plum retint.

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.aiStyle = 8;
            AIType = 96;
            Projectile.timeLeft = 60;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = 3;
            Projectile.tileCollide = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation += 3f;

            if (!Main.dedServ)
            {
                // Pink flame multiplied by the plum meter colour; a fainter curse-powder wisp marks it as a curse.
                Dust flame = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.PinkTorch, 0, 0, 50, PowerfulCurseBuildup.PowerfulCurseColor, 3.0f);
                flame.noGravity = true;

                if (Main.rand.NextBool(4))
                {
                    Dust wisp = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.VilePowder, 0, 0, 150, PowerfulCurseBuildup.PowerfulCurseColor, 1.2f);
                    wisp.noGravity = true;
                }

                Lighting.AddLight(Projectile.Center, PowerfulCurseBuildup.PowerfulCurseColor.ToVector3() * 0.8f);
            }

            // Per-tick contact buildup, separate from hits (i-frames gate OnHitPlayer, not this).
            Player localPlayer = Main.LocalPlayer;
            bool touchingLocalPlayer = !Main.dedServ && localPlayer.active && !localPlayer.dead && Projectile.Hitbox.Intersects(localPlayer.Hitbox);
            bool alreadyBuiltThisTick = lastPowerfulCurseTick == Main.GameUpdateCount;

            if (touchingLocalPlayer && !alreadyBuiltThisTick)
            {
                lastPowerfulCurseTick = Main.GameUpdateCount;

                // AddBuff only when the marker is missing: ReApply would add its generic 160 and burst dust every tick.
                int powerfulCurseType = ModContent.BuffType<PowerfulCurseBuildup>();
                if (!localPlayer.HasBuff(powerfulCurseType))
                {
                    localPlayer.AddBuff(powerfulCurseType, PowerfulCurseMarkerTicks, false);
                }

                localPlayer.GetModPlayer<tsorcRevampPlayer>().PowerfulCurseLevel += PowerfulCurseBuildupPerTick;
            }
        }

        public override bool PreKill(int timeLeft)
        {
            return false;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Main.GameUpdateCount >= nextTileImpactSoundFrame)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.15f, Pitch = 0.1f }, Projectile.Center);
                nextTileImpactSoundFrame = Main.GameUpdateCount + 30;
            }

            Projectile.active = false;
            Projectile.netUpdate = true;
            return false;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.CursedInferno, CursedFlamesTicks, false);
        }
    }
}
