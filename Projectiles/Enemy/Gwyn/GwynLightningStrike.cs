using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///Stage 2 of Gwyn's Spear of the First Sun: the delayed judgment bolt. Spawned at the thrown
    ///spear's impact, it snaps down to the floor below, marks the spot with a golden rune circle
    ///(GwynStrikeMark.png) that spins and brightens for a ~40t telegraph, then a lightning bolt
    ///crashes down (flash + thunder) and electricity races along the floor both ways
    ///(GwynFloorSpark). Damage only in the brief strike window — the mark itself is a warning.
    ///</summary>
    class GwynLightningStrike : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Gwyn/GwynStrikeMark";

        const int TelegraphTicks = 40;
        const int StrikeTicks = 12;
        const int FloorSparkTiles = 12;
        const string TextureRoot = "tsorcRevamp/Textures/Noise/";

        static Asset<Effect> judgmentEffect;
        static Asset<Texture2D> smoothNoise;
        static Asset<Texture2D> brokenNoise;
        static Asset<Texture2D> impactFlare;

        int Timer => (int)Projectile.localAI[0];
        bool Striking => Timer > TelegraphTicks;

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 60;
            Projectile.height = 220; // a tall column at the strike; collides only while striking
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = TelegraphTicks + StrikeTicks;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            //Snap the column's base to the floor under the impact
            float groundY = FindGroundY(Projectile.Center, 20);
            if (groundY > 0f)
            {
                Projectile.position.Y = groundY - Projectile.height;
                Projectile.position.X = Projectile.Center.X - Projectile.width / 2f;
            }
        }

        public override bool? CanDamage()
        {
            return Striking;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Projectile.localAI[0]++;
            float groundY = Projectile.position.Y + Projectile.height;

            if (!Striking)
            {
                //Rune telegraph: rising gold motes through the column + a glint on the ground mark
                if (Main.rand.NextBool(2))
                {
                    Vector2 pos = new Vector2(Projectile.Center.X + Main.rand.NextFloat(-30f, 30f), groundY - Main.rand.NextFloat(Projectile.height));
                    int mote = Dust.NewDust(pos, 4, 4, DustID.GoldFlame, 0f, -2f, 100, default, 0.9f);
                    Main.dust[mote].noGravity = true;
                }
                Lighting.AddLight(new Vector2(Projectile.Center.X, groundY - 20f), 0.4f, 0.35f, 0.15f);

                if (Timer == TelegraphTicks)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Thunder with { Volume = 0.7f, Pitch = 0.2f }, Projectile.Center);
                    UsefulFunctions.ScreenShake(new Vector2(Projectile.Center.X, groundY), 5f, 12);
                    //Electricity races along the floor both ways from the strike point
                    SpawnFloorPulse(groundY, false);
                }
                return;
            }

            if (Timer == TelegraphTicks + 4 || Timer == TelegraphTicks + 8)
                SpawnFloorPulse(groundY, true);

            //The shader is the bolt. Keep only a few pinprick sparks so the silhouette never
            //turns back into the broad gold cloud that used to obscure the impact.
            for (int i = 0; i < 3; i++)
            {
                Vector2 pos = new Vector2(Projectile.Center.X + Main.rand.NextFloat(-12f, 12f), Projectile.position.Y + Main.rand.NextFloat(Projectile.height));
                int dust = Dust.NewDust(pos, 2, 2, DustID.Electric, 0f, 0f, 80, new Color(255, 211, 84), Main.rand.NextFloat(0.7f, 1.05f));
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = Main.rand.NextVector2Circular(1.2f, 1.8f);
            }
            for (int seg = 0; seg < 4; seg++)
            {
                Lighting.AddLight(new Vector2(Projectile.Center.X, Projectile.position.Y + Projectile.height * (seg + 0.5f) / 4f), 1f, 0.9f, 0.4f);
            }
        }

        void SpawnFloorPulse(float groundY, bool visualOnly)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int dir = -1; dir <= 1; dir += 2)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), new Vector2(Projectile.Center.X, groundY - 16f), Vector2.Zero,
                    ModContent.ProjectileType<GwynFloorSpark>(), visualOnly ? 0 : Projectile.damage, 3f,
                    Projectile.owner, dir, FloorSparkTiles, visualOnly ? 1f : 0f);
            }
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire, 5 * 60);
        }

        static void LoadAssets()
        {
            judgmentEffect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/GwynSunlightJudgment", AssetRequestMode.ImmediateLoad);
            smoothNoise ??= ModContent.Request<Texture2D>(TextureRoot + "T_VFX_NoiseF1", AssetRequestMode.ImmediateLoad);
            brokenNoise ??= ModContent.Request<Texture2D>(TextureRoot + "T_VFX_Noise41", AssetRequestMode.ImmediateLoad);
            impactFlare ??= ModContent.Request<Texture2D>(TextureRoot + "T_VFX_Flare_666", AssetRequestMode.ImmediateLoad);
        }

        ///<summary>The rune remains the non-damaging ground warning. A descending filament builds
        ///above it, then the shader fills the exact 60x220 collision column during the strike.</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            LoadAssets();

            float groundY = Projectile.position.Y + Projectile.height;
            Vector2 groundPosition = new Vector2(Projectile.Center.X, groundY - 6f) - Main.screenPosition;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float progress = Striking
                ? MathHelper.Clamp((Timer - TelegraphTicks) / (float)StrikeTicks, 0f, 1f)
                : MathHelper.Clamp(Timer / (float)TelegraphTicks, 0f, 1f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            if (!Striking)
                DrawRune(groundPosition, progress);
            else
                DrawImpactFlare(groundPosition, progress);

            DrawJudgmentColumn(drawPosition, progress);

            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            return false;
        }

        void DrawRune(Vector2 groundPosition, float progress)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            float scale = 0.28f;
            float alpha = 0.3f + 0.6f * progress;
            Main.EntitySpriteDraw(texture, groundPosition, null, new Color(255, 220, 90) * alpha,
                Main.GlobalTimeWrappedHourly * 2f, texture.Size() * 0.5f, scale, SpriteEffects.None, 0);
        }

        void DrawImpactFlare(Vector2 groundPosition, float progress)
        {
            float flash = 1f - MathHelper.Clamp(progress / 0.55f, 0f, 1f);
            if (flash <= 0f)
                return;

            Texture2D texture = impactFlare.Value;
            Main.EntitySpriteDraw(texture, groundPosition, null, new Color(255, 211, 84) * flash,
                0f, texture.Size() * 0.5f, MathHelper.Lerp(0.07f, 0.12f, progress), SpriteEffects.None, 0);
        }

        void DrawJudgmentColumn(Vector2 drawPosition, float progress)
        {
            Effect effect = judgmentEffect.Value;
            Rectangle source = new Rectangle(0, 0, Projectile.width, Projectile.height);
            float opacity = Striking
                ? MathHelper.Clamp(Projectile.timeLeft / 4f, 0.55f, 1f)
                : 0.42f + progress * 0.38f;
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
                effect.Parameters["Active"].SetValue(Striking ? 1f : 0f);
                effect.Parameters["Direction"].SetValue(1f);
                effect.CurrentTechnique.Passes[0].Apply();

                Main.EntitySpriteDraw(smoothNoise.Value, drawPosition, source, Color.White, 0f,
                    source.Size() * 0.5f, 1f, SpriteEffects.None, 0);
            }
            finally
            {
                graphicsDevice.Textures[1] = previousTexture;
                graphicsDevice.SamplerStates[1] = previousSampler;
            }
        }

        static float FindGroundY(Vector2 worldPos, int maxTilesDown)
        {
            int tx = (int)(worldPos.X / 16f);
            int ty = (int)(worldPos.Y / 16f);
            if (tx < 5 || tx > Main.maxTilesX - 5)
            {
                return -1f;
            }
            for (int d = 0; d <= maxTilesDown; d++)
            {
                int y = ty + d;
                if (y >= Main.maxTilesY - 5)
                {
                    break;
                }
                Tile tile = Main.tile[tx, y];
                if (tile.HasTile && !tile.IsActuated && Main.tileSolid[tile.TileType])
                {
                    return y * 16f;
                }
            }
            return -1f;
        }
    }
}
