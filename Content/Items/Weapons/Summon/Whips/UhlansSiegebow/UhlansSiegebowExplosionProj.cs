using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Weapons.Summon.Whips.UhlansSiegebow
{
    public class UhlansSiegebowExplosionProj : ModProjectile
    {
        public const float FullDamageRadius = 52f;
        public const float MiniPopDamageRadius = 42f;

        // ai[0]: 0 = primary bolt blast (applies summon tag), 1 = minion-triggered powder pop.
        private bool IsMiniPop => Projectile.ai[0] >= 0.5f;

        public static bool IsWithinRadius(Vector2 center, Rectangle targetHitbox, float radius)
        {
            float clampedX = MathHelper.Clamp(center.X, targetHitbox.Left, targetHitbox.Right);
            float clampedY = MathHelper.Clamp(center.Y, targetHitbox.Top, targetHitbox.Bottom);
            Vector2 closestPoint = new Vector2(clampedX, clampedY);
            return Vector2.DistanceSquared(center, closestPoint) <= radius * radius;
        }

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 5;
        }

        public override void SetDefaults()
        {
            Projectile.width = 52;
            Projectile.height = 52;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.SummonMeleeSpeed;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 20;
            Projectile.aiStyle = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool? CanDamage()
        {
            if (Projectile.timeLeft > 12)
            {
                return null;
            }

            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float activeRadius = FullDamageRadius;
            if (IsMiniPop)
            {
                activeRadius = MiniPopDamageRadius;
            }

            return IsWithinRadius(Projectile.Center, targetHitbox, activeRadius);
        }

        public override void OnSpawn(IEntitySource source)
        {
            if (IsMiniPop)
            {
                Projectile.scale = 0.72f;
            }

            if (Main.dedServ)
            {
                return;
            }

            float volume = 0.55f;
            float pitch = 0.05f;
            int fireCount = 22;
            int smokeCount = 8;

            if (IsMiniPop)
            {
                volume = 0.35f;
                pitch = 0.3f;
                fireCount = 12;
                smokeCount = 4;
            }

            SoundEngine.PlaySound(SoundID.Item14 with { Volume = volume, Pitch = pitch }, Projectile.Center);

            for (int i = 0; i < smokeCount; i++)
            {
                Vector2 smokePos = Projectile.Center + Main.rand.NextVector2Circular(14f, 14f);
                Vector2 smokeVel = Main.rand.NextVector2Circular(2.2f, 2.2f) + new Vector2(0f, -0.5f);
                Dust smoke = Dust.NewDustPerfect(smokePos, DustID.Smoke, smokeVel, 145, new Color(95, 70, 58), Main.rand.NextFloat(0.75f, 0.95f));
                smoke.noGravity = true;
                smoke.noLight = true;
                smoke.fadeIn = Main.rand.NextFloat(1.15f, 1.4f);
            }

            for (int i = 0; i < fireCount; i++)
            {
                int dustType = DustID.Torch;
                if (i % 2 == 0)
                {
                    dustType = DustID.OrangeTorch;
                }

                Vector2 firePos = Projectile.Center + Main.rand.NextVector2Circular(16f, 16f);
                Vector2 fireVel = Main.rand.NextVector2Circular(3.6f, 3.6f);
                Dust fire = Dust.NewDustPerfect(firePos, dustType, fireVel, 45, default, Main.rand.NextFloat(0.95f, 1.3f));
                fire.noGravity = true;
            }
        }

        public override void AI()
        {
            Projectile.frame = (20 - Projectile.timeLeft) / 4;
            if (Projectile.frame >= 5)
            {
                Projectile.frame = 4;
            }

            if (!Main.dedServ)
            {
                float lightScale = Projectile.timeLeft / 20f;
                Lighting.AddLight(Projectile.Center, new Vector3(0.85f, 0.35f, 0.06f) * lightScale);
            }
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return Color.White;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // +2 milestone perk (ai[1] >= 2) upgrades blast burn from On Fire! to Hellfire.
            int fireDebuffId = BuffID.OnFire;
            if (Projectile.ai[1] >= 2f)
            {
                fireDebuffId = BuffID.OnFire3;
            }

            target.AddBuff(fireDebuffId, 4 * 60);

            if (IsMiniPop)
            {
                return;
            }

            Player owner = Main.player[Projectile.owner];
            float tagDurationMult = owner.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration;
            int tagTicks = (int)(UhlansSiegebowItem.TagDurationSeconds * 60 * tagDurationMult);
            target.AddBuff(ModContent.BuffType<UhlansSiegebowDebuff>(), tagTicks);
            target.GetGlobalNPC<NPCs.tsorcRevampGlobalNPC>().uhlansSiegebowReinforcementLevel = (byte)Projectile.ai[1];
            owner.MinionAttackTargetNPC = target.whoAmI;
        }
    }
}
