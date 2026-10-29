using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Content.Projectiles.Enemy.Weapons
{
    // Shadow Ninja's bow shot. Every death (player, tile, timeout) bursts green and leaves a
    // ShadowNinjaPoisonCloud behind, so a dodged arrow still denies the ground it lands on.
    public class ShadowNinjaPoisonArrow : EnemyTaintedArrow
    {
        // Half the 100-point Poison Buildup meter per direct hit; standing in the cloud tops it off.
        public const int DirectHitBuildup = 50;

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            // OnHitPlayer runs on the victim's machine, which is where PoisonBuildup.Apply must be called.
            PoisonBuildup.Apply(target, DirectHitBuildup);

            // Hostile shots pierce players unless they kill themselves; tsorcGlobalProjectile relays this local kill to the server in MP.
            Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            // Tile chips + dig sound from the base arrow.
            base.OnKill(timeLeft);

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<ShadowNinjaPoisonCloud>(), 0, 0f, Main.myPlayer);
            }

            if (Main.dedServ)
            {
                return;
            }

            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.45f, Pitch = 0.25f }, Projectile.Center);

            // Kill burst: ~70 dusts. Poisoned (46) is the dense green body, GreenTorch (75) the bright glints.
            for (int i = 0; i < 50; i++)
            {
                Vector2 burstVelocity = Main.rand.NextVector2Circular(3.5f, 3.5f);
                Dust body = Dust.NewDustPerfect(Projectile.Center, DustID.Poisoned, burstVelocity, 90, default, Main.rand.NextFloat(0.9f, 1.5f));
                body.noGravity = true;
            }

            for (int i = 0; i < 20; i++)
            {
                Vector2 glintVelocity = Main.rand.NextVector2Circular(5f, 5f);
                Dust glint = Dust.NewDustPerfect(Projectile.Center, DustID.GreenTorch, glintVelocity, 60, default, Main.rand.NextFloat(1f, 1.6f));
                glint.noGravity = true;
            }
        }
    }

    // Lingering gas left where a Shadow Ninja arrow dies. Grows from a 16px puff to 8 tiles (128px)
    // across over 3s, holds for 3s, then dissipates harmlessly over 0.75s. No hit damage: a player
    // inside the current radius gains 1 Poison Buildup per tick (a full meter in ~1.7s).
    // Same shape as Eland's ToxicGasNovaFog, scaled down; collision is a true circle, not the AABB.
    public class ShadowNinjaPoisonCloud : ModProjectile
    {
        // Dust + shader only; the shared transparent placeholder, same as ToxicGasNovaFog.
        public override string Texture => "tsorcRevamp/NPCs/Puppets/PuppetPlaceholder";

        public const int Diameter = 128;
        public const int BuildupPerTick = 1;

        private const float StartRadius = 8f;
        private const float MaxRadius = Diameter / 2f;
        private const int GrowTicks = 3 * 60;
        private const int LingerTicks = 3 * 60;
        private const int DissipateTicks = 45;
        private const int HarmfulTicks = GrowTicks + LingerTicks;
        private const int TotalTicks = HarmfulTicks + DissipateTicks;

        // ElandToxicField's alpha holds full from progress 0.11 to 0.72, then fades to zero at 1.0.
        private const float ShaderHoldStart = 0.12f;
        private const float ShaderFadeStart = 0.72f;

        private int Age => TotalTicks - Projectile.timeLeft;

        // Ease-out growth: bursts open quickly, then creeps to the full 8 tiles at 3s.
        private float CurrentRadius
        {
            get
            {
                float grow = MathHelper.Clamp(Age / (float)GrowTicks, 0f, 1f);
                grow = grow * (2f - grow);
                return MathHelper.Lerp(StartRadius, MaxRadius, grow);
            }
        }

        public override void SetDefaults()
        {
            // Max extent for broadphase/culling; the real test is IsInsideCloud.
            Projectile.width = Diameter;
            Projectile.height = Diameter;
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.damage = 0;
            Projectile.timeLeft = TotalTicks;
        }

        public override void AI()
        {
            float radius = CurrentRadius;
            bool harmful = Age < HarmfulTicks;

            // Buildup is owned by the victim's client (PoisonBuildup.Apply ignores everyone else), so
            // every machine checks only its own local player. The server has no local player and skips.
            if (harmful && !Main.dedServ)
            {
                Player local = Main.LocalPlayer;
                if (local.active && !local.dead && IsInsideCloud(local.Hitbox, radius))
                {
                    PoisonBuildup.Apply(local, BuildupPerTick);
                }
            }

            Lighting.AddLight(Projectile.Center, 0.08f, 0.32f, 0.08f);

            if (Main.dedServ)
            {
                return;
            }

            // Rising churn inside the current radius. One puff per tick while live, one every 3rd
            // tick while dissipating so the dust thins out together with the shader.
            bool spawnPuff = harmful || Main.GameUpdateCount % 3 == 0;
            if (spawnPuff)
            {
                Vector2 spot = Projectile.Center + Main.rand.NextVector2Circular(radius, radius);
                Vector2 drift = new Vector2(Main.rand.NextFloat(-0.25f, 0.25f), Main.rand.NextFloat(-0.5f, -0.1f));
                Dust puff = Dust.NewDustPerfect(spot, DustID.Poisoned, drift, 140, default, Main.rand.NextFloat(1f, 1.5f));
                puff.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Map our timeline onto the shader's hold curve: full strength for the whole harmful
            // window, then its 0.72 -> 1.0 fade lands exactly on the dissipate ticks.
            float shaderProgress;
            if (Age < HarmfulTicks)
            {
                float lifeFraction = Age / (float)HarmfulTicks;
                shaderProgress = MathHelper.Lerp(ShaderHoldStart, ShaderFadeStart, lifeFraction);
            }
            else
            {
                float dissipateFraction = MathHelper.Clamp((Age - HarmfulTicks) / (float)DissipateTicks, 0f, 1f);
                shaderProgress = MathHelper.Lerp(ShaderFadeStart, 1f, dissipateFraction);
            }

            float rotation = Projectile.whoAmI * 1.7f;
            EnemyVFX.DrawElandToxicField(Projectile.Center, Vector2.One * CurrentRadius * 2f, shaderProgress, true, rotation);
            return false;
        }

        public override bool? CanDamage() => false;

        private bool IsInsideCloud(Rectangle targetHitbox, float radius)
        {
            Vector2 closest = Vector2.Clamp(Projectile.Center,
                new Vector2(targetHitbox.Left, targetHitbox.Top),
                new Vector2(targetHitbox.Right, targetHitbox.Bottom));
            return Vector2.DistanceSquared(Projectile.Center, closest) <= radius * radius;
        }
    }
}
