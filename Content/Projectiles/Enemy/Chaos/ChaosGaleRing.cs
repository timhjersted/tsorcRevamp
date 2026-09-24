using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy.Chaos
{
    ///<summary>
    ///The boundary Wing Buffet shoves you toward: a large ring of dark fire that fades in and holds at a FIXED
    ///radius. At the verdict it expands to twice its radius before fading if escaped, or collapses if trapped.
    ///
    ///ONE RING PER PLAYER. Each is centred on its owner at the moment the gale begins and then left there
    ///(ShouldUpdatePosition false), and it is drawn by, and can only hit, that one player. Everybody therefore
    ///starts dead centre with a full radius of room to resist in, which a single shared ring cannot give —
    ///centred on Chaos your position in it was luck, and centred on one player it was meaningless to everyone
    ///else. The cost is that co-op players cannot see each other's rings.
    ///
    ///True annulus collision, so the middle is safe and the band is thin enough to dash through: the test is
    ///whether you resist the gust, not whether you can outrun a wall. Damage arms only once it has faded in.
    ///
    ///Rendered with the IceGigasFreezeRing shader, which already takes a radius, a half-thickness and an
    ///opacity. The half-thickness is fed the SAME constant as the hitbox, so the band that glows is exactly
    ///the band that hurts, and the opacity is driven straight off the fade.
    ///
    ///ai[0] = radius in px. ai[1] = hold ticks between the fades. ai[2] = owning player index.
    ///</summary>
    class ChaosGaleRing : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(Projectiles.InvisibleNothingProj));

        public const int FadeTicks = 40;
        const int ExpandTicks = 60;
        const float RingHalfThickness = 26f;
        //Margin between the outer lip and the quad edge, so the shader's feathering has somewhere to land
        //instead of being sliced flat by the boundary.
        const float QuadPadding = 40f;
        const float TrailLength = 44f;
        const string TextureRoot = "tsorcRevamp/Textures/Noise/";

        // Endgame verdict — decided once, the instant the hold ends (see BeginEndgame). Escaped: the ring
        // was beaten, so it balloons out to twice its radius, then fades. Trapped: it closes in on its owner and
        // detonates, the punish for never leaving the boundary.
        const int CollapseTicks = 90;

        static Asset<Effect> ringEffect;
        static Asset<Texture2D> cellNoise;
        static Asset<Texture2D> crackNoise;

        bool endgameStarted;
        bool playerEscaped;
        float endgameBaseRadius;

        float Radius
        {
            get
            {
                float baseRadius = Projectile.ai[0] > 0f ? Projectile.ai[0] : 810f;

                if (!endgameStarted)
                {
                    return baseRadius;
                }

                if (playerEscaped)
                {
                    float expandProgress = MathHelper.Clamp(Projectile.localAI[0] / (float)ExpandTicks, 0f, 1f);
                    return MathHelper.Lerp(endgameBaseRadius, endgameBaseRadius * 2f, expandProgress);
                }

                float collapseProgress = MathHelper.Clamp(Projectile.localAI[0] / (float)CollapseTicks, 0f, 1f);
                return MathHelper.Lerp(endgameBaseRadius, 0f, collapseProgress);
            }
        }

        int HoldTicks => Projectile.ai[1] > 0f ? (int)Projectile.ai[1] : 180;
        int TotalTicks => FadeTicks * 2 + HoldTicks;
        int Age => TotalTicks - Projectile.timeLeft;

        ///<summary>The only player who can see this ring or be hit by it. Index 0 is the default, which is also
        ///the single-player case, so ai[2] never needs to be synced for it to be right.</summary>
        int OwnerIndex => (int)Projectile.ai[2];
        bool OwnedByThisMachine => Main.myPlayer == OwnerIndex;

        ///<summary>0 while fading in, 1 while held or during the endgame. Drives the shader opacity, the dust
        ///density and whether the ring bites, so all three agree by construction.</summary>
        float Presence
        {
            get
            {
                if (endgameStarted)
                {
                    if (playerEscaped)
                    {
                        // Keep the growing boundary readable all the way to twice its radius. The fade starts
                        // only after the expansion finishes, so the full-size ring is actually visible.
                        return 1f - MathHelper.Clamp((Projectile.localAI[0] - ExpandTicks) / FadeTicks, 0f, 1f);
                    }

                    // Stays fully visible right up until it closes — this is a telegraphed detonation, not a
                    // fade-out, so it should never read as "safe now" the way the old dissolve did.
                    return 1f;
                }

                if (Age < FadeTicks)
                {
                    return Age / (float)FadeTicks;
                }

                return 1f;
            }
        }

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.width = 1672;    // default radius plus half thickness, doubled; refined in OnSpawn
            Projectile.height = 1672;
            Projectile.timeLeft = FadeTicks * 2 + 180;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = TotalTicks;

            //Resize the broadphase to the real radius, then put the centre back. NewProjectile positioned us
            //using the SetDefaults size, so changing width/height without restoring Center shifts the whole
            //ring by half its own diameter — which is why it was landing ~700px off and looking absent.
            Vector2 center = Projectile.Center;
            int span = (int)((Radius + RingHalfThickness) * 2f);
            Projectile.width = span;
            Projectile.height = span;
            Projectile.Center = center;
        }

        public override bool ShouldUpdatePosition()
        {
            return false;
        }

        public override bool? CanDamage()
        {
            //The expanding and collapsing bands can still hit, but the escape fade follows the original
            //visibility threshold so an almost invisible ring cannot hurt the player.
            if (endgameStarted)
            {
                return !playerEscaped || Presence > 0.9f;
            }

            return Presence > 0.9f;
        }

        ///<summary>Only its owner. Without this every player would be hit by every other player's ring, which
        ///they cannot even see.</summary>
        public override bool CanHitPlayer(Player target)
        {
            return target.whoAmI == OwnerIndex;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;

            //The owner left or died: nobody can see this or be hit by it any more, so stop burning a slot.
            if (OwnerIndex < 0 || OwnerIndex >= Main.maxPlayers || !Main.player[OwnerIndex].active)
            {
                Projectile.Kill();
                return;
            }

            //The server decides the verdict and keeps the ring alive through either ending. Its decision is
            //synced to the owner, who draws the ring and plays the verdict cue.
            if (!endgameStarted && Age >= FadeTicks + HoldTicks && Main.netMode != NetmodeID.MultiplayerClient)
            {
                BeginEndgame();
            }

            if (endgameStarted)
            {
                RunEndgame();
                return;
            }

            //Dust and light are owner-only, so co-op players do not see overlapping rings.
            if (Main.dedServ || !OwnedByThisMachine)
            {
                return;
            }

            float presence = Presence;

            //Dust laid around the circumference on top of the shader, thickening as the ring arms itself.
            int motes = 10 + (int)(presence * 16f);

            for (int i = 0; i < motes; i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 outward = angle.ToRotationVector2();
                Vector2 spot = Projectile.Center + outward * (Radius + Main.rand.NextFloat(-RingHalfThickness, RingHalfThickness));

                int dustType = DustID.Shadowflame;
                if (Main.rand.NextBool(3))
                {
                    dustType = DustID.DemonTorch;
                }

                Dust mote = Dust.NewDustPerfect(spot, dustType, outward * 0.8f, 90, default, Main.rand.NextFloat(1.1f, 1.7f) * (0.5f + presence));
                mote.noGravity = true;
            }

            Lighting.AddLight(Projectile.Center, 0.3f * presence, 0.08f * presence, 0.45f * presence);
        }

        ///<summary>The one-time verdict, decided the instant the hold ends: did the owner ever get pushed to
        ///(or past) the boundary, or are they still inside it? Escaped resizes the hitbox up front, since the
        ///visual is about to grow past the box NewProjectile originally sized for the base radius.</summary>
        void BeginEndgame()
        {
            // Order matters: Radius branches on endgameStarted, so this MUST be read before that flag flips —
            // otherwise it reads its own not-yet-initialized endgame math (endgameBaseRadius still 0, localAI[0]
            // not yet reset) and collapses to 0 on the spot. That silently broke both branches: playerEscaped
            // became "distance > 0", true almost always, and the dissipation burst drew at a ~0 radius instead
            // of the real one.
            endgameBaseRadius = Radius;
            endgameStarted = true;
            Projectile.localAI[0] = 0f;

            Player owner = Main.player[OwnerIndex];
            playerEscaped = owner.Distance(Projectile.Center) > endgameBaseRadius;

            if (playerEscaped)
            {
                ResizeForExpansion();
            }

            Projectile.netUpdate = true;
            if (!Main.dedServ && OwnedByThisMachine)
            {
                SpawnDissipationBurst();
                SpawnVerdictCue(owner);
            }
        }

        void ResizeForExpansion()
        {
            Vector2 center = Projectile.Center;
            int span = (int)((endgameBaseRadius * 2f + RingHalfThickness) * 2f);
            Projectile.width = span;
            Projectile.height = span;
            Projectile.Center = center;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(endgameStarted);
            if (endgameStarted)
            {
                writer.Write(playerEscaped);
                writer.Write(endgameBaseRadius);
                writer.Write(Projectile.localAI[0]);
            }
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            bool receivedEndgame = reader.ReadBoolean();
            if (!receivedEndgame)
            {
                return;
            }

            bool wasInEndgame = endgameStarted;
            endgameStarted = true;
            playerEscaped = reader.ReadBoolean();
            endgameBaseRadius = reader.ReadSingle();
            Projectile.localAI[0] = reader.ReadSingle();
            if (playerEscaped)
            {
                ResizeForExpansion();
            }

            if (!wasInEndgame && !Main.dedServ && OwnedByThisMachine)
            {
                SpawnDissipationBurst();
                SpawnVerdictCue(Main.player[OwnerIndex]);
            }
        }

        ///<summary>The ring's own geometry can be 800+px away from the player by the time it decides anything,
        ///which reads as nothing happening at all. This puts the verdict where the player is actually looking —
        ///a cool burst right on them for escaping, a harsher warning one for getting caught — independent of
        ///whether they can see the ring itself.</summary>
        void SpawnVerdictCue(Player owner)
        {
            if (Main.dedServ)
            {
                return;
            }

            int dustType = DustID.DemonTorch;
            Color soundTint = Color.MediumPurple;
            if (playerEscaped)
            {
                dustType = DustID.IceTorch;
                soundTint = Color.CadetBlue;
            }

            for (int i = 0; i < 40; i++)
            {
                Vector2 burst = Main.rand.NextVector2Circular(6f, 6f);
                Dust mote = Dust.NewDustPerfect(owner.Center, dustType, burst, 60, default, Main.rand.NextFloat(1.6f, 2.4f));
                mote.noGravity = true;
            }

            SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.7f, PitchVariance = 0.15f }, owner.Center);
            Lighting.AddLight(owner.Center, soundTint.ToVector3() * 0.6f);
        }

        ///<summary>Runs on the server and clients so neither ending expires at the old 40-tick fade limit.
        ///The escape branch grows for 60 ticks, then fades for 40; the trapped branch closes for 90.</summary>
        void RunEndgame()
        {
            Projectile.localAI[0]++;
            Projectile.timeLeft = 2;

            if (playerEscaped)
            {
                if (!Main.dedServ && OwnedByThisMachine)
                {
                    int motes = 16;
                    for (int i = 0; i < motes; i++)
                    {
                        float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                        Vector2 outward = angle.ToRotationVector2();
                        Vector2 spot = Projectile.Center + outward * (Radius + Main.rand.NextFloat(-RingHalfThickness, RingHalfThickness));

                        int dustType = DustID.Shadowflame;
                        if (Main.rand.NextBool(3))
                        {
                            dustType = DustID.DemonTorch;
                        }

                        Dust mote = Dust.NewDustPerfect(spot, dustType, outward * Main.rand.NextFloat(1.5f, 4.5f), 90, default, Main.rand.NextFloat(1.1f, 1.7f) * Presence);
                        mote.noGravity = true;
                    }

                    Lighting.AddLight(Projectile.Center, 0.3f * Presence, 0.08f * Presence, 0.45f * Presence);
                }

                if (Projectile.localAI[0] >= ExpandTicks + FadeTicks)
                {
                    Projectile.Kill();
                }

                return;
            }

            if (!Main.dedServ && OwnedByThisMachine)
            {
                //Trapped: dust converges INWARD toward the shrinking radius as the danger closes.
                for (int i = 0; i < 14; i++)
                {
                    float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                    Vector2 outward = angle.ToRotationVector2();
                    Vector2 spot = Projectile.Center + outward * (Radius + Main.rand.NextFloat(-RingHalfThickness, RingHalfThickness));

                    int dustType = DustID.Shadowflame;
                    if (Main.rand.NextBool(3))
                    {
                        dustType = DustID.DemonTorch;
                    }

                    Dust mote = Dust.NewDustPerfect(spot, dustType, -outward * Main.rand.NextFloat(2f, 6f), 60, default, Main.rand.NextFloat(1.3f, 2f));
                    mote.noGravity = true;
                }

                Lighting.AddLight(Projectile.Center, 0.5f, 0.15f, 0.7f);
            }

            if (Projectile.localAI[0] >= CollapseTicks)
            {
                TriggerCollapseExplosion();
                Projectile.Kill();
            }
        }

        ///<summary>One dense ring of dust thrown outward along the whole circumference as the band lets go.
        ///Evenly spaced rather than randomly scattered, so for one frame the full circle is unmistakable.</summary>
        void SpawnDissipationBurst()
        {
            const int BurstMotes = 150;

            for (int i = 0; i < BurstMotes; i++)
            {
                float angle = MathHelper.TwoPi * i / BurstMotes;
                Vector2 outward = angle.ToRotationVector2();
                Vector2 spot = Projectile.Center + outward * (Radius + Main.rand.NextFloat(-RingHalfThickness, RingHalfThickness));

                int dustType = DustID.Shadowflame;
                if (Main.rand.NextBool(3))
                {
                    dustType = DustID.DemonTorch;
                }

                Dust mote = Dust.NewDustPerfect(spot, dustType, outward * Main.rand.NextFloat(3f, 9f), 60, default, Main.rand.NextFloat(1.6f, 2.5f));
                mote.noGravity = true;
            }
        }

        ///<summary>The punish for never leaving the ring. Damage is no longer one manual hit here — each
        ///ChaosCollapseBurst is its own hostile hit, in three staggered waves (centre now, then a triangle and
        ///a circle further out, timed by ChaosCollapseExplosionController) so the detonation actually reads
        ///as a 600+px event instead of one small point-blank hit.</summary>
        void TriggerCollapseExplosion()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                SpawnCollapseBurst(Vector2.Zero);

                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<ChaosCollapseExplosionController>(), 0, 0f, Main.myPlayer,
                    ai0: Projectile.damage, ai1: OwnerIndex);
            }

            if (Main.dedServ || !OwnedByThisMachine)
            {
                return;
            }

            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.8f }, Projectile.Center);
            UsefulFunctions.ScreenShake(Projectile.Center, 8f, 20);

            for (int i = 0; i < 80; i++)
            {
                float angle = MathHelper.TwoPi * i / 80;
                Vector2 outward = angle.ToRotationVector2();
                Dust mote = Dust.NewDustPerfect(Projectile.Center, DustID.Shadowflame, outward * Main.rand.NextFloat(6f, 12f), 60, default, Main.rand.NextFloat(1.8f, 2.6f));
                mote.noGravity = true;
            }

            for (int i = 0; i < 20; i++)
            {
                Vector2 burst = Main.rand.NextVector2Circular(4f, 4f);
                Dust ember = Dust.NewDustPerfect(Projectile.Center, DustID.DemonTorch, burst, 60, default, Main.rand.NextFloat(1.5f, 2.2f));
                ember.noGravity = true;
            }

            Lighting.AddLight(Projectile.Center, 0.9f, 0.3f, 1.1f);
        }

        ///<summary>The centre hit of the detonation (ChaosCollapseBurst — the reused Abyss Storm explosion
        ///sprite, no tint; the wider triangle/circle waves are ChaosCollapseExplosionController's job).</summary>
        void SpawnCollapseBurst(Vector2 offset)
        {
            Vector2 spawnPosition = Projectile.Center + offset + Main.rand.NextVector2Circular(15f, 15f);
            int burst = Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawnPosition, Vector2.Zero,
                ModContent.ProjectileType<ChaosCollapseBurst>(), Projectile.damage, 0f, Main.myPlayer,
                ai0: OwnerIndex);
            Main.projectile[burst].scale = 2.1f;
            Main.projectile[burst].rotation = Main.rand.NextFloat(MathHelper.TwoPi);
        }

        static void LoadAssets()
        {
            ringEffect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/IceGigasFreezeRing", AssetRequestMode.ImmediateLoad);
            cellNoise ??= ModContent.Request<Texture2D>(TextureRoot + "VoronoiNoise", AssetRequestMode.ImmediateLoad);
            crackNoise ??= ModContent.Request<Texture2D>(TextureRoot + "Vein_07-512x512", AssetRequestMode.ImmediateLoad);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            //Your ring is yours alone. Drawing everyone's would put a wall of overlapping bands on screen that
            //mostly cannot hurt you.
            if (!OwnedByThisMachine)
            {
                return false;
            }

            LoadAssets();

            float drawRadius = Radius + QuadPadding;
            int diameter = (int)Math.Ceiling(drawRadius * 2f);
            if (diameter <= 0)
            {
                return false;
            }

            Texture2D primary = cellNoise.Value;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
            Texture previousTexture = graphicsDevice.Textures[1];
            SamplerState previousSampler = graphicsDevice.SamplerStates[1];
            try
            {
                graphicsDevice.Textures[1] = crackNoise.Value;
                graphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;

                Effect effect = ringEffect.Value;
                effect.CurrentTechnique = effect.Techniques["IceGigasFreezeRing"];
                //Chaos's violet, rather than the ice palette the shader was authored against.
                effect.Parameters["OuterColor"].SetValue(new Color(34, 6, 54).ToVector3());
                effect.Parameters["MiddleColor"].SetValue(new Color(142, 48, 214).ToVector3());
                effect.Parameters["CoreColor"].SetValue(new Color(236, 198, 255).ToVector3());
                effect.Parameters["Opacity"].SetValue(Presence);
                effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                effect.Parameters["DrawSize"].SetValue(new Vector2(diameter));
                effect.Parameters["RingRadius"].SetValue(Radius);
                effect.Parameters["RingHalfThickness"].SetValue(RingHalfThickness);
                effect.Parameters["TrailLength"].SetValue(TrailLength);
                //Pre-divided pixel grid (2px blocks), matching the mod's pixel-filter convention.
                effect.Parameters["PixelGrid"].SetValue(new Vector4(
                    diameter / 2f, diameter / 2f, 2f / diameter, 2f / diameter));
                effect.CurrentTechnique.Passes[0].Apply();

                Main.EntitySpriteDraw(primary, Projectile.Center - Main.screenPosition, null, Color.White, 0f,
                    primary.Size() * 0.5f, diameter / (float)primary.Width, SpriteEffects.None, 0);
            }
            finally
            {
                graphicsDevice.Textures[1] = previousTexture;
                graphicsDevice.SamplerStates[1] = previousSampler;
            }

            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            //True annulus: the band only, so standing in the middle is safe and a roll can cross it.
            //Hit when the hitbox's nearest point is inside the outer edge AND its farthest corner is outside
            //the inner edge — i.e. the box actually straddles the band.
            Vector2 center = Projectile.Center;

            Vector2 closest = new Vector2(
                MathHelper.Clamp(center.X, targetHitbox.Left, targetHitbox.Right),
                MathHelper.Clamp(center.Y, targetHitbox.Top, targetHitbox.Bottom));
            float closestDistance = Vector2.Distance(center, closest);

            float farthestX = MathHelper.Max(MathHelper.Distance(center.X, targetHitbox.Left), MathHelper.Distance(center.X, targetHitbox.Right));
            float farthestY = MathHelper.Max(MathHelper.Distance(center.Y, targetHitbox.Top), MathHelper.Distance(center.Y, targetHitbox.Bottom));
            float farthestDistance = (float)Math.Sqrt(farthestX * farthestX + farthestY * farthestY);

            return closestDistance <= Radius + RingHalfThickness && farthestDistance >= Radius - RingHalfThickness;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<Buffs.Debuffs.DarkInferno>(), 180);
        }
    }
}
