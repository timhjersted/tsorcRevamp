using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Melee.Broadswords
{
    /// <summary>
    /// The Sword of Lord Gwyn combo finisher's payoff: a sunlight bolt that crashes onto the slam point the instant
    /// the blade lands, then sends sparks racing along the floor both ways. The bolt is the boss's own
    /// GwynSunlightJudgment column shader, filling the exact collision column. ai[0] = 1 when empowered (Lord Gwyn
    /// set above half mana): wider sparks and a longer Daybreak.
    /// </summary>
    class SwordOfLordGwynSlamLightning : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        const int Lifetime = 18;
        const int StrikeTicks = 10;
        const int ColumnWidth = 56;
        const int ColumnHeight = 260;
        const int GroundSearchTiles = 12;
        const float SparkSpeed = 9f;
        const float EmpoweredSparkSpeed = 10.5f;
        const float SparkDamageScale = 0.35f;
        const string TextureRoot = "tsorcRevamp/Textures/Noise/";

        static Asset<Effect> judgmentEffect;
        static Asset<Texture2D> smoothNoise;
        static Asset<Texture2D> brokenNoise;
        static Asset<Texture2D> impactFlare;

        bool Empowered => Projectile.ai[0] == 1f;
        int Age => Lifetime - Projectile.timeLeft;

        public override void SetDefaults()
        {
            Projectile.width = ColumnWidth;
            Projectile.height = ColumnHeight;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            // Spawned at the slam point. Sit the column's base on the floor beneath it (or on the slam point
            // itself when airborne). Only the owner runs this; position reaches other clients in the spawn packet.
            Vector2 slamPoint = Projectile.Center;
            float groundY = FindGroundY(slamPoint);
            if (groundY < 0f)
            {
                groundY = slamPoint.Y;
            }

            Projectile.position = new Vector2(slamPoint.X - ColumnWidth / 2f, groundY - ColumnHeight);
        }

        public override bool? CanDamage()
        {
            if (Age >= StrikeTicks)
            {
                return false;
            }

            return null;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            float groundY = Projectile.position.Y + Projectile.height;
            Vector2 groundPoint = new Vector2(Projectile.Center.X, groundY);

            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                StrikeEffects(groundPoint);

                if (Projectile.owner == Main.myPlayer)
                {
                    SpawnFloorSparks(groundPoint);
                }
            }

            float fade = Projectile.timeLeft / (float)Lifetime;
            for (int segment = 0; segment < 4; segment++)
            {
                float height = Projectile.height * (segment + 0.5f) / 4f;
                Lighting.AddLight(new Vector2(Projectile.Center.X, groundY - height), 1f * fade, 0.85f * fade, 0.4f * fade);
            }

            if (Main.dedServ || Age >= StrikeTicks)
            {
                return;
            }

            // The shader is the bolt; a few pinprick sparks only, so the column keeps a crisp silhouette.
            for (int i = 0; i < 3; i++)
            {
                Vector2 position = new Vector2(Projectile.Center.X + Main.rand.NextFloat(-12f, 12f), Projectile.position.Y + Main.rand.NextFloat(Projectile.height));
                Dust spark = Dust.NewDustPerfect(position, DustID.Electric, Main.rand.NextVector2Circular(1.2f, 1.8f), 80, new Color(255, 211, 84), Main.rand.NextFloat(0.7f, 1.05f));
                spark.noGravity = true;
            }
        }

        void StrikeEffects(Vector2 groundPoint)
        {
            if (Main.dedServ)
            {
                return;
            }

            SoundEngine.PlaySound(SoundID.Thunder with { Volume = 0.6f, Pitch = 0.25f }, groundPoint);
            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.55f, Pitch = 0.1f }, groundPoint);
            UsefulFunctions.ScreenShake(groundPoint, 5f, 12, distanceFalloff: 520f);

            // Splash of gold sparks thrown up and out from where the bolt meets the floor.
            for (int i = 0; i < 18; i++)
            {
                Vector2 velocity = new Vector2(Main.rand.NextFloat(-5.5f, 5.5f), Main.rand.NextFloat(-6f, -1.5f));
                int type = DustID.GoldFlame;
                if (Main.rand.NextBool(3))
                {
                    type = DustID.Electric;
                }

                Dust spark = Dust.NewDustPerfect(groundPoint - Vector2.UnitY * 4f, type, velocity, 60, new Color(255, 214, 96), Main.rand.NextFloat(0.9f, 1.4f));
                spark.noGravity = type == DustID.Electric;
            }
        }

        void SpawnFloorSparks(Vector2 groundPoint)
        {
            float speed = SparkSpeed;
            if (Empowered)
            {
                speed = EmpoweredSparkSpeed;
            }

            int sparkDamage = (int)(Projectile.damage * SparkDamageScale);
            Vector2 sparkOrigin = groundPoint - Vector2.UnitY * 10f;
            for (int direction = -1; direction <= 1; direction += 2)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), sparkOrigin, new Vector2(speed * direction, 0f),
                    ModContent.ProjectileType<SwordOfLordGwynImpactLightning>(), sparkDamage, Projectile.knockBack * 0.5f,
                    Projectile.owner, Projectile.ai[0]);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            int daybreakTicks = 3 * 60;
            if (Empowered)
            {
                daybreakTicks = 5 * 60;
            }

            target.AddBuff(BuffID.Daybreak, daybreakTicks);
        }

        static float FindGroundY(Vector2 worldPosition)
        {
            int tileX = (int)(worldPosition.X / 16f);
            int tileY = (int)(worldPosition.Y / 16f);
            if (tileX < 5 || tileX > Main.maxTilesX - 5 || tileY < 5)
            {
                return -1f;
            }

            for (int depth = 0; depth <= GroundSearchTiles; depth++)
            {
                int y = tileY + depth;
                if (y >= Main.maxTilesY - 5)
                {
                    break;
                }

                Tile tile = Main.tile[tileX, y];
                bool solid = tile.HasTile && !tile.IsActuated && (Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]);
                if (solid)
                {
                    return y * 16f;
                }
            }

            return -1f;
        }

        static void LoadAssets()
        {
            judgmentEffect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/GwynSunlightJudgment", AssetRequestMode.ImmediateLoad);
            smoothNoise ??= ModContent.Request<Texture2D>(TextureRoot + "T_VFX_NoiseF1", AssetRequestMode.ImmediateLoad);
            brokenNoise ??= ModContent.Request<Texture2D>(TextureRoot + "T_VFX_Noise41", AssetRequestMode.ImmediateLoad);
            impactFlare ??= ModContent.Request<Texture2D>(TextureRoot + "T_VFX_Flare_666", AssetRequestMode.ImmediateLoad);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            LoadAssets();

            // progress 0..1 across the whole life; the column is at full strength through the strike, then thins out.
            float progress = Age / (float)Lifetime;
            float opacity = MathHelper.Clamp(Projectile.timeLeft / 6f, 0f, 1f);
            if (!Empowered)
            {
                opacity *= 0.9f;
            }

            float groundY = Projectile.position.Y + Projectile.height;
            Vector2 groundPosition = new Vector2(Projectile.Center.X, groundY - 6f) - Main.screenPosition;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            // Ground flash: bright on the first ticks, gone by the end of the strike.
            float flash = 1f - MathHelper.Clamp(Age / (float)StrikeTicks, 0f, 1f);
            if (flash > 0f)
            {
                Texture2D flare = impactFlare.Value;
                float flareScale = MathHelper.Lerp(0.13f, 0.08f, flash);
                Main.EntitySpriteDraw(flare, groundPosition, null, new Color(255, 211, 84) * flash, 0f, flare.Size() * 0.5f, flareScale, SpriteEffects.None, 0);
            }

            Effect effect = judgmentEffect.Value;
            Rectangle source = new Rectangle(0, 0, Projectile.width, Projectile.height);
            GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
            Texture previousTexture = graphicsDevice.Textures[1];
            SamplerState previousSampler = graphicsDevice.SamplerStates[1];

            try
            {
                graphicsDevice.Textures[1] = brokenNoise.Value;
                graphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;

                effect.CurrentTechnique = effect.Techniques["GwynSunlightJudgmentColumn"];
                effect.Parameters["GoldColor"].SetValue(new Color(255, 147, 22).ToVector3());
                effect.Parameters["HotColor"].SetValue(new Color(255, 221, 92).ToVector3());
                effect.Parameters["CoreColor"].SetValue(new Color(255, 252, 218).ToVector3());
                effect.Parameters["Opacity"].SetValue(opacity);
                effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                effect.Parameters["DrawSize"].SetValue(source.Size());
                effect.Parameters["PrimaryTextureSize"].SetValue(smoothNoise.Value.Size());
                effect.Parameters["Progress"].SetValue(progress);
                effect.Parameters["Active"].SetValue(1f);
                effect.Parameters["Direction"].SetValue(1f);
                effect.CurrentTechnique.Passes[0].Apply();

                Main.EntitySpriteDraw(smoothNoise.Value, Projectile.Center - Main.screenPosition, source, Color.White, 0f,
                    source.Size() * 0.5f, 1f, SpriteEffects.None, 0);
            }
            finally
            {
                graphicsDevice.Textures[1] = previousTexture;
                graphicsDevice.SamplerStates[1] = previousSampler;
            }

            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            return false;
        }
    }
}
