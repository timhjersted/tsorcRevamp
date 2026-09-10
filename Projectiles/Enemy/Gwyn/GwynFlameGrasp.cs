using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///Gwyn's Lord's Grasp: a hand of the First Flame that reaches out and seizes. A shader-defined
    ///flaming claw flies toward the player; on contact it immolates and drags them into Gwyn's
    ///authored sword-finisher range before retracting. Bypasses the comfort of range: it's the
    ///shield-turtle / spacing punish. ai[0] = parent NPC whoAmI, ai[1] = state,
    ///ai[2] = grabbed player index + 1.
    ///</summary>
    class GwynFlameGrasp : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        const int StateFlying = 0;
        const int StateGrasping = 1;
        const int StateRetracting = 2;

        const int Lifetime = 180;
        const int MaxFlightTicks = 44;
        const int GraspTicks = 44;
        const float MaxLength = 620f;
        const float PullSpeed = 14f;
        const float CaptureStandoff = 72f;
        const int GrabPadding = 14;
        const int ShaderDrawSize = 58;
        const string TextureRoot = "tsorcRevamp/Textures/Noise/";

        static Asset<Effect> flameGraspEffect;
        static Asset<Texture2D> flameTexture;
        static Asset<Texture2D> smoothNoise;

        static readonly Vector2[] OutlineDirections =
        {
            new Vector2(-1f, -1f), new Vector2(0f, -1f), new Vector2(1f, -1f),
            new Vector2(-1f,  0f),                         new Vector2(1f,  0f),
            new Vector2(-1f,  1f), new Vector2(0f,  1f), new Vector2(1f,  1f),
        };

        int State => (int)Projectile.ai[1];
        int TargetWho => (int)Projectile.ai[2] - 1;
        float Age => Projectile.localAI[0];

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.width = 46;
            Projectile.height = 46;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = Lifetime;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.light = 0.7f;
        }

        public override void AI()
        {
            if (!TryGetOwner(out NPC owner))
            {
                Projectile.Kill();
                return;
            }

            Projectile.localAI[0]++;
            switch (State)
            {
                case StateGrasping:
                    GraspPlayer(owner);
                    break;
                case StateRetracting:
                    RetractToOwner(owner);
                    break;
                default:
                    FlyOut(owner);
                    break;
            }

            SpawnFire(owner);
            Lighting.AddLight(Projectile.Center, 1f, 0.5f, 0.15f);
        }

        void FlyOut(NPC owner)
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.velocity *= 0.995f;
            if (Age >= MaxFlightTicks || Projectile.Distance(GetOriginPosition(owner)) >= MaxLength)
            {
                StartRetract();
            }
        }

        void GraspPlayer(NPC owner)
        {
            int targetIndex = TargetWho;
            if (targetIndex < 0 || targetIndex >= Main.maxPlayers)
            {
                StartRetract();
                return;
            }

            Player target = Main.player[targetIndex];
            if (!target.active || target.dead)
            {
                StartRetract();
                return;
            }

            int direction = owner.spriteDirection == 0 ? owner.direction : owner.spriteDirection;
            Vector2 capturePoint = owner.Center + new Vector2(direction * CaptureStandoff, -4f);
            Vector2 toCapture = capturePoint - target.Center;
            if (toCapture.LengthSquared() > 9f)
            {
                float pullSpeed = MathHelper.Clamp(toCapture.Length() * 0.22f, 3f, PullSpeed);
                Vector2 desiredVelocity = toCapture.SafeNormalize(Vector2.Zero) * pullSpeed;
                target.velocity = Vector2.Lerp(target.velocity, desiredVelocity, 0.38f);
            }
            else
            {
                target.velocity *= 0.35f;
            }

            Projectile.Center = target.Center;
            Projectile.velocity = Vector2.Zero;
            Projectile.rotation = (target.Center - GetOriginPosition(owner))
                .SafeNormalize(new Vector2(direction, 0f)).ToRotation();

            if (++Projectile.localAI[1] >= GraspTicks)
            {
                target.velocity *= 0.35f;
                StartRetract();
            }
        }

        void RetractToOwner(NPC owner)
        {
            Vector2 origin = GetOriginPosition(owner);
            Vector2 toOrigin = origin - Projectile.Center;
            if (toOrigin.LengthSquared() <= 18f * 18f)
            {
                Projectile.Kill();
                return;
            }

            Projectile.velocity = Vector2.Lerp(Projectile.velocity,
                toOrigin.SafeNormalize(Vector2.Zero) * 18f, 0.34f);
            Projectile.rotation = (-toOrigin).ToRotation();
        }

        void SpawnFire(NPC owner)
        {
            if (Main.dedServ)
            {
                return;
            }

            int tipCount = State == StateGrasping ? 8 : 5;
            for (int i = 0; i < tipCount; i++)
            {
                Vector2 spread = Main.rand.NextVector2Circular(Projectile.width * 0.5f, Projectile.height * 0.5f);
                int type = Main.rand.NextBool() ? DustID.Torch : DustID.GoldFlame;
                Dust dust = Dust.NewDustPerfect(Projectile.Center + spread, type,
                    Projectile.velocity * 0.12f + Main.rand.NextVector2Circular(0.7f, 0.7f),
                    40, default, Main.rand.NextFloat(1.15f, 1.65f));
                dust.noGravity = true;
            }

            Vector2 origin = GetOriginPosition(owner);
            int segments = (int)MathHelper.Clamp(Vector2.Distance(origin, Projectile.Center) / 42f, 1f, 12f);
            for (int i = 1; i < segments; i++)
            {
                if (!Main.rand.NextBool(3))
                {
                    continue;
                }

                Vector2 position = Vector2.Lerp(origin, Projectile.Center, i / (float)segments)
                    + Main.rand.NextVector2Circular(5f, 5f);
                int type = Main.rand.NextBool(3) ? DustID.Torch : DustID.GoldFlame;
                Dust dust = Dust.NewDustPerfect(position, type,
                    Main.rand.NextVector2Circular(0.6f, 0.6f), 55, default,
                    Main.rand.NextFloat(0.85f, 1.25f));
                dust.noGravity = true;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (State != StateFlying)
            {
                return;
            }

            target.AddBuff(BuffID.OnFire, 10 * 60);
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (TryGetOwner(out NPC owner)
                    && owner.ModNPC is global::tsorcRevamp.NPCs.Puppets.PuppetNPC puppet)
                {
                    puppet.ReportAttackHit();
                }
                StartGrasp(target);
            }
        }

        public override bool CanHitPlayer(Player target)
        {
            return State == StateFlying;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (State != StateFlying)
            {
                return false;
            }

            Rectangle grabBox = Projectile.Hitbox;
            grabBox.Inflate(GrabPadding, GrabPadding);
            if (grabBox.Intersects(targetHitbox))
            {
                return true;
            }

            float collisionPoint = 0f;
            Vector2 previousCenter = Projectile.Center - Projectile.velocity;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                previousCenter, Projectile.Center, Projectile.width + GrabPadding * 2f,
                ref collisionPoint);
        }

        void StartGrasp(Player target)
        {
            Projectile.ai[1] = StateGrasping;
            Projectile.ai[2] = target.whoAmI + 1;
            Projectile.localAI[1] = 0f;
            Projectile.velocity = Vector2.Zero;
            Projectile.hostile = false;
            Projectile.netUpdate = true;
            if (!Main.dedServ)
            {
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.NPCHit13 with { Volume = 0.65f, Pitch = -0.1f }, Projectile.Center);
            }
        }

        void StartRetract()
        {
            if (State == StateRetracting)
            {
                return;
            }

            Projectile.ai[1] = StateRetracting;
            Projectile.hostile = false;
            Projectile.netUpdate = true;
        }

        bool TryGetOwner(out NPC owner)
        {
            int ownerIndex = (int)Projectile.ai[0];
            if (ownerIndex >= 0 && ownerIndex < Main.maxNPCs)
            {
                owner = Main.npc[ownerIndex];
                if (owner.active)
                {
                    return true;
                }
            }

            owner = null;
            return false;
        }

        static Vector2 GetOriginPosition(NPC owner)
        {
            if (owner.ModNPC is global::tsorcRevamp.NPCs.Bosses.SuperHardMode.Gwyn gwyn)
            {
                return gwyn.GraspHandPosition;
            }

            int direction = owner.spriteDirection == 0 ? owner.direction : owner.spriteDirection;
            return owner.Center + new Vector2(direction * (owner.width * 0.5f + 10f),
                -owner.height * 0.3f);
        }

        static void LoadAssets()
        {
            flameGraspEffect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/GwynFlameGrasp", AssetRequestMode.ImmediateLoad);
            flameTexture ??= ModContent.Request<Texture2D>(TextureRoot + "T_AuraC12", AssetRequestMode.ImmediateLoad);
            smoothNoise ??= ModContent.Request<Texture2D>(TextureRoot + "T_VFX_NoiseF1", AssetRequestMode.ImmediateLoad);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float fadeIn = MathHelper.Clamp((Age + 2f) / 5f, 0.55f, 1f);
            float fadeOut = MathHelper.Clamp(Projectile.timeLeft / 8f, 0f, 1f);
            float drawScale = State == StateGrasping ? 1.18f : 1f;
            DrawHandAura(Projectile.Center, Projectile.rotation, drawScale,
                fadeIn * fadeOut, drawUnblockableOutline: false);
            return false;
        }

        /// <summary>
        /// Shared First-Flame hand presentation. Lord's Embrace adds the same eight-direction red
        /// unblockable outline language used by Artorias, then covers its center with the ordinary
        /// orange grasp flame so the silhouette stays readable instead of becoming a solid red orb.
        /// </summary>
        internal static void DrawHandAura(Vector2 worldPosition, float rotation, float scale,
            float opacity, bool drawUnblockableOutline)
        {
            LoadAssets();

            Rectangle source = new Rectangle(0, 0, ShaderDrawSize, ShaderDrawSize);
            Vector2 drawPosition = worldPosition - Main.screenPosition;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Effect effect = flameGraspEffect.Value;
            GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
            Texture previousTexture = graphicsDevice.Textures[1];
            SamplerState previousSampler = graphicsDevice.SamplerStates[1];

            try
            {
                graphicsDevice.Textures[1] = smoothNoise.Value;
                graphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;

                effect.CurrentTechnique = effect.Techniques["GwynFlameGrasp"];
                effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                effect.Parameters["PrimaryTextureSize"].SetValue(flameTexture.Value.Size());

                if (drawUnblockableOutline)
                {
                    effect.Parameters["CinderColor"].SetValue(new Color(255, 4, 3).ToVector3());
                    effect.Parameters["FlameColor"].SetValue(new Color(255, 10, 6).ToVector3());
                    effect.Parameters["CoreColor"].SetValue(new Color(255, 196, 156).ToVector3());
                    float pulse = 0.5f + 0.5f
                        * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 8f);
                    effect.Parameters["Opacity"].SetValue(opacity * MathHelper.Lerp(0.76f, 1f, pulse));
                    effect.CurrentTechnique.Passes[0].Apply();

                    float outlineRadius = MathHelper.Lerp(1.6f, 2.8f, pulse)
                        * System.Math.Max(1f, scale);
                    foreach (Vector2 direction in OutlineDirections)
                    {
                        Main.EntitySpriteDraw(flameTexture.Value,
                            drawPosition + direction.SafeNormalize(Vector2.Zero) * outlineRadius,
                            source, Color.White, rotation, source.Size() * 0.5f, scale,
                            SpriteEffects.None, 0);
                    }
                }

                effect.Parameters["CinderColor"].SetValue(new Color(255, 38, 3).ToVector3());
                effect.Parameters["FlameColor"].SetValue(new Color(255, 118, 13).ToVector3());
                effect.Parameters["CoreColor"].SetValue(new Color(255, 229, 144).ToVector3());
                effect.Parameters["Opacity"].SetValue(opacity);
                effect.CurrentTechnique.Passes[0].Apply();

                Main.EntitySpriteDraw(flameTexture.Value, drawPosition, source, Color.White,
                    rotation, source.Size() * 0.5f, scale, SpriteEffects.None, 0);
            }
            finally
            {
                graphicsDevice.Textures[1] = previousTexture;
                graphicsDevice.SamplerStates[1] = previousSampler;
            }

            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
        }

        public override void OnKill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.5f, Pitch = 0.2f }, Projectile.Center);
            for (int i = 0; i < 14; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(4f, 4f);
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, vel.X, vel.Y, 40, default, 1.4f);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}
