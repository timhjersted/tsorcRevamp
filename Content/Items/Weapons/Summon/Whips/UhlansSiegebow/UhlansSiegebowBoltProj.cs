using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Systems;

namespace tsorcRevamp.Content.Items.Weapons.Summon.Whips.UhlansSiegebow
{
    public class UhlansSiegebowBoltProj : ModProjectile
    {
        public const float MaximumTravel = UhlansSiegebowItem.MaxRangeTiles * 16f;
        private const float LodgedProximityRadius = 44f;

        // ai[1] state machine: 0 = flying, 1 = max-range airburst, 2 = lodged in tile, 3 = expired/evicted silently.
        private const float StateFlying = 0f;
        private const float StateAirburst = 1f;
        private const float StateLodged = 2f;
        private const float StateFizzled = 3f;

        private byte reinforcementLevel;

        public override void OnSpawn(IEntitySource source)
        {
            Player owner = Main.player[Projectile.owner];
            if (owner != null && owner.active)
            {
                reinforcementLevel = WeaponReinforcement.GetDisplayedLevel(owner.HeldItem);
            }
        }

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.SummonMeleeSpeed;
            Projectile.penetrate = 1;
            Projectile.tileCollide = true;
            Projectile.timeLeft = 180;
            Projectile.aiStyle = -1;
            // Required for DrawBehind so lodged bolts sit cleanly behind the terrain tile they are stuck in.
            Projectile.hide = true;
        }

        // The explosion owns 100% of the damage hit so direct impacts never double-dip.
        public override bool? CanDamage()
        {
            return false;
        }

        public override void AI()
        {
            if (Projectile.ai[1] == StateAirburst)
            {
                Projectile.Kill();
                return;
            }

            bool isLodged = Projectile.ai[1] == StateLodged;

            if (isLodged)
            {
                Projectile.velocity = Vector2.Zero;
                Projectile.ai[2]++;

                int maxLodgedTicks = UhlansSiegebowItem.LodgedDurationSeconds * 60;
                if (Projectile.ai[2] >= maxLodgedTicks)
                {
                    if (Main.myPlayer == Projectile.owner)
                    {
                        Projectile.ai[1] = StateFizzled;
                        Projectile.netUpdate = true;
                        Projectile.Kill();
                    }

                    return;
                }

                Projectile.timeLeft = 2;
            }

            if (Main.myPlayer == Projectile.owner)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC candidate = Main.npc[i];
                    bool isValidTarget = candidate.active
                        && !candidate.friendly
                        && !candidate.dontTakeDamage
                        && (candidate.CanBeChasedBy(Projectile) || candidate.type == NPCID.TargetDummy);

                    if (!isValidTarget)
                    {
                        continue;
                    }

                    bool triggered = false;
                    if (isLodged)
                    {
                        triggered = UhlansSiegebowExplosionProj.IsWithinRadius(Projectile.Center, candidate.Hitbox, LodgedProximityRadius);
                    }
                    else
                    {
                        triggered = Projectile.Hitbox.Intersects(candidate.Hitbox);
                    }

                    if (triggered)
                    {
                        Main.player[Projectile.owner].MinionAttackTargetNPC = candidate.whoAmI;
                        Projectile.Kill();
                        return;
                    }
                }
            }

            if (isLodged)
            {
                if (!Main.dedServ)
                {
                    Lighting.AddLight(Projectile.Center, 0.5f, 0.2f, 0.04f);

                    if (Main.GameUpdateCount % 6 == 0)
                    {
                        Dust ember = Dust.NewDustPerfect(
                            Projectile.Center + Main.rand.NextVector2Circular(3f, 3f),
                            DustID.OrangeTorch,
                            new Vector2(Main.rand.NextFloat(-0.3f, 0.3f), -Main.rand.NextFloat(0.6f, 1.3f)),
                            45,
                            default,
                            0.95f);
                        ember.noGravity = true;
                    }
                }

                return;
            }

            // Flying state: straight for 14 ticks, then heavy crossbow drop; airbursts cleanly at 20 tiles.
            Projectile.ai[2]++;
            if (Projectile.ai[2] > 14f)
            {
                Projectile.velocity.Y += 0.1f;
            }

            float remaining = MaximumTravel - Projectile.ai[0];
            float step = Projectile.velocity.Length();
            if (step >= remaining && step > 0.001f)
            {
                Projectile.velocity *= remaining / step;
                step = remaining;
                Projectile.ai[1] = StateAirburst;
            }

            Projectile.ai[0] += step;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (Main.dedServ)
            {
                return;
            }

            Lighting.AddLight(Projectile.Center, 0.6f, 0.26f, 0.05f);

            // Interpolate 2 ember motes per tick along the bolt's step so the flaming trail stays continuous.
            for (int i = 0; i < 2; i++)
            {
                float stepFraction = i * 0.5f;
                Vector2 samplePos = Vector2.Lerp(Projectile.oldPosition, Projectile.position, stepFraction) + Projectile.Size * 0.5f;
                Dust trail = Dust.NewDustPerfect(
                    samplePos + Main.rand.NextVector2Circular(2f, 2f),
                    DustID.OrangeTorch,
                    -Projectile.velocity * 0.12f,
                    45,
                    default,
                    Main.rand.NextFloat(0.85f, 1.1f));
                trail.noGravity = true;
            }
        }

        public override void DrawBehind(
            int index,
            List<int> behindNPCsAndTiles,
            List<int> behindNPCs,
            List<int> behindProjectiles,
            List<int> overPlayers,
            List<int> overWiresUI)
        {
            if (Projectile.ai[1] == StateLodged)
            {
                behindNPCsAndTiles.Add(index);
            }
            else
            {
                behindProjectiles.Add(index);
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // Lodge cleanly into terrain as a proximity explosive trap (capped at MaxLodgedBolts per owner).
            Projectile.velocity = Vector2.Zero;
            Projectile.position += oldVelocity * 0.35f;

            if (Main.myPlayer == Projectile.owner)
            {
                int lodgedCount = 0;
                int oldestIndex = -1;
                float oldestAge = -1f;

                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile other = Main.projectile[i];
                    if (other.active
                        && other.whoAmI != Projectile.whoAmI
                        && other.owner == Projectile.owner
                        && other.type == Type
                        && other.ai[1] == StateLodged)
                    {
                        lodgedCount++;
                        if (other.ai[2] > oldestAge)
                        {
                            oldestAge = other.ai[2];
                            oldestIndex = i;
                        }
                    }
                }

                if (lodgedCount >= UhlansSiegebowItem.MaxLodgedBolts && oldestIndex >= 0)
                {
                    Projectile oldestBolt = Main.projectile[oldestIndex];
                    oldestBolt.ai[1] = StateFizzled;
                    oldestBolt.netUpdate = true;
                    oldestBolt.Kill();
                }

                Projectile.ai[1] = StateLodged;
                Projectile.ai[2] = 0f;
                Projectile.timeLeft = 2;
                Projectile.netUpdate = true;
            }

            if (!Main.dedServ)
            {
                for (int i = 0; i < 5; i++)
                {
                    Vector2 sparkVel = -oldVelocity * 0.15f + Main.rand.NextVector2Circular(1.8f, 1.8f);
                    Dust spark = Dust.NewDustPerfect(Projectile.Center, DustID.Torch, sparkVel, 45, default, 0.9f);
                    spark.noGravity = true;
                }
            }

            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (Projectile.ai[1] == StateFizzled)
            {
                if (!Main.dedServ)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        Vector2 smokeVel = Main.rand.NextVector2Circular(1.5f, 1.5f) + new Vector2(0f, -0.6f);
                        Dust smoke = Dust.NewDustPerfect(Projectile.Center, DustID.Smoke, smokeVel, 140, new Color(115, 95, 85), 0.7f);
                        smoke.noGravity = true;
                        smoke.noLight = true;
                        smoke.fadeIn = 1.05f;
                    }
                }

                return;
            }

            if (Main.myPlayer == Projectile.owner)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_Death(),
                    Projectile.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<UhlansSiegebowExplosionProj>(),
                    Projectile.damage,
                    Projectile.knockBack,
                    Projectile.owner,
                    0f,
                    reinforcementLevel);
            }
        }
    }
}
