using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy.Chaos
{
    ///<summary>
    ///The boundary Wing Buffet shoves you toward: a large ring of dark fire that fades in, holds at a FIXED
    ///radius, and fades out. Not an expanding wave — it does not move, so "safe" is learnable.
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
        const float RingHalfThickness = 26f;
        //Margin between the outer lip and the quad edge, so the shader's feathering has somewhere to land
        //instead of being sliced flat by the boundary.
        const float QuadPadding = 40f;
        const float TrailLength = 44f;
        const string TextureRoot = "tsorcRevamp/Textures/Noise/";

        static Asset<Effect> ringEffect;
        static Asset<Texture2D> cellNoise;
        static Asset<Texture2D> crackNoise;

        float Radius => Projectile.ai[0] > 0f ? Projectile.ai[0] : 810f;
        int HoldTicks => Projectile.ai[1] > 0f ? (int)Projectile.ai[1] : 180;
        int TotalTicks => FadeTicks * 2 + HoldTicks;
        int Age => TotalTicks - Projectile.timeLeft;

        ///<summary>The only player who can see this ring or be hit by it. Index 0 is the default, which is also
        ///the single-player case, so ai[2] never needs to be synced for it to be right.</summary>
        int OwnerIndex => (int)Projectile.ai[2];
        bool OwnedByThisMachine => Main.myPlayer == OwnerIndex;

        ///<summary>0 while fading in or out, 1 while held. Drives the shader opacity, the dust density and
        ///whether the ring bites, so all three agree by construction.</summary>
        float Presence
        {
            get
            {
                if (Age < FadeTicks)
                {
                    return Age / (float)FadeTicks;
                }

                if (Age >= FadeTicks + HoldTicks)
                {
                    return 1f - (Age - FadeTicks - HoldTicks) / (float)FadeTicks;
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
            //Harmless while it is still forming or already dissolving — you can only be hit by a ring you see.
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

            //Dust, light and draw are all owner-only, so a four-player fight is not four overlapping rings of
            //dust on everyone's screen.
            if (Main.dedServ || !OwnedByThisMachine)
            {
                return;
            }

            float presence = Presence;
            bool fadingOut = Age >= FadeTicks + HoldTicks;

            //The instant it starts dissolving, throw the band apart. Damage switches off within a few ticks of
            //this (CanDamage needs presence > 0.9), so the burst is also the "it is safe now" cue.
            if (Age == FadeTicks + HoldTicks)
            {
                SpawnDissipationBurst();
            }

            //Dust laid around the circumference on top of the shader, thickening as the ring arms itself and
            //again as it comes apart.
            int motes = 10 + (int)(presence * 16f);
            if (fadingOut)
            {
                motes += 12;
            }

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

                //While dissolving the motes drift outward and off; while holding they just shimmer in place.
                Vector2 velocity = outward * 0.8f;
                if (fadingOut)
                {
                    velocity = outward * Main.rand.NextFloat(1.5f, 4.5f);
                }

                Dust mote = Dust.NewDustPerfect(spot, dustType, velocity, 90, default, Main.rand.NextFloat(1.1f, 1.7f) * (0.5f + presence));
                mote.noGravity = true;
            }

            Lighting.AddLight(Projectile.Center, 0.3f * presence, 0.08f * presence, 0.45f * presence);
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
