using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Content.Projectiles.Enemy.Darkmoon
{
    public class DarkmoonDeathCloud : ModProjectile
    {
        const int Lifetime = 84;
        const int FormTicks = 22;
        const int FadeTicks = 24;
        const float FullRadius = 50f;
        // The mantle mask reaches about 62% of the original quad height from center. A 35% larger quad clears it.
        const float SmokePadding = 0.35f;

        static readonly Color Shadow = new Color(15, 28, 95);
        static readonly Color Body = new Color(47, 84, 169);
        static readonly Color Edge = new Color(81, 129, 219);
        static readonly Color Highlight = new Color(128, 170, 240);
        readonly HashSet<int> affectedPlayers = new();

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        int Age => Lifetime - Projectile.timeLeft;
        float Formation => MathHelper.Clamp(Age / (float)FormTicks, 0f, 1f);
        float Dissipation => MathHelper.Clamp(Projectile.timeLeft / (float)FadeTicks, 0f, 1f);
        float Radius => MathHelper.Lerp(18f, FullRadius, Formation);

        public override void SetDefaults()
        {
            Projectile.width = (int)(FullRadius * 2f);
            Projectile.height = Projectile.width;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Lifetime;
        }

        public override void AI()
        {
            bool forming = Age < FormTicks;
            int emissionInterval = forming ? 2 : 5;
            if (!Main.dedServ && Projectile.timeLeft > FadeTicks && Age % emissionInterval == 0)
            {
                // Scatter flecks as the front expands, then leave a few slow wisps in the active cloud.
                int wispCount = forming ? 3 : 1;
                float speed = forming ? 2.3f : 0.85f;
                for (int i = 0; i < wispCount; i++)
                {
                    Vector2 outward = Main.rand.NextVector2Unit();
                    Vector2 position = Projectile.Center + outward * Radius * 0.65f;
                    Vector2 velocity = outward * Main.rand.NextFloat(0.4f, 1f) * speed + Projectile.velocity;
                    Dust wisp = Dust.NewDustPerfect(position, DustID.TintableDustLighted, velocity, 145, Edge,
                        Main.rand.NextFloat(0.7f, 1.15f));
                    wisp.noGravity = true;
                    wisp.noLight = true;
                }
            }

            if (Main.netMode == NetmodeID.MultiplayerClient || Age < FormTicks || Projectile.timeLeft <= FadeTicks)
            {
                return;
            }

            // Curse each player once while the core is fully formed. The thinner fringe can drift through terrain.
            int buffType = ModContent.BuffType<WeightOfShadow>();
            float radiusSquared = Radius * Radius;
            foreach (Player target in Main.ActivePlayers)
            {
                if (target.dead || affectedPlayers.Contains(target.whoAmI) ||
                    Vector2.DistanceSquared(Projectile.Center, target.Center) > radiusSquared ||
                    !Collision.CanHitLine(Projectile.Center, 1, 1, target.Center, 1, 1))
                {
                    continue;
                }

                affectedPlayers.Add(target.whoAmI);
                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.AddPlayerBuff, target.whoAmI, number: target.whoAmI, number2: buffType,
                        number3: ArtoriasCurseCloud.WeightOfShadowTicks);
                }
                else
                {
                    target.AddBuff(buffType, ArtoriasCurseCloud.WeightOfShadowTicks);
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float radius = Radius;
            Vector2 size = new Vector2(radius * 2.65f, radius * 2.4f);
            float opacity = MathHelper.Lerp(0.28f, 1.65f, Formation) * Dissipation;
            ArtoriasVFX.DrawMantle(Projectile.Center, size, opacity, 1.45f, -1f, Shadow, Body, Edge, Highlight,
                fullTexture: true, padding: SmokePadding);
            return false;
        }
    }
}
