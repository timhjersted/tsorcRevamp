using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Bosses.SuperHardMode;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    // Purely visual: the actual stab/bonus damage and heal are applied directly by Artorias
    // (OnPierceContact) the instant the dash connects. This projectile just tracks the sword tip
    // position Artorias computes (GetSwordTipWorldPosition), anchors the impaled player to it (turned sideways,
    // see GetImpalePlayerRotation) and draws the blade + impale VFX OVER the player so the sword reads as
    // driven into their midsection.
    public class ArtoriasImpalingSword : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(Content.Items.Weapons.Melee.Broadswords.ArtoriasGreatsword));

        int OwnerWhoAmI => (int)Projectile.ai[0];
        int TargetWhoAmI => (int)Projectile.ai[1];

        // ── Entry-side blood spray ──────────────────────────────────────────────────────────────
        // The exit-side spray (out the far side of the target) is plain Dust in
        // Artorias.DoPierceStabHoldTick - Dust always draws over the player, which is exactly right
        // for that half. This half sprays back toward Artorias (into the "entry wound") and has to
        // draw BEHIND the player instead, which Dust cannot do, so it's a manual particle list drawn
        // from this projectile's own already-behind-the-player PreDraw.
        struct BackBloodDrop
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public float Life;
            public float MaxLife;
            public float Scale;
        }

        readonly List<BackBloodDrop> _backBlood = new();
        int _backBloodSpawnTimer;

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 70;
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 600;
        }

        public override void AI()
        {
            if (OwnerWhoAmI < 0 || OwnerWhoAmI >= Main.maxNPCs || !Main.npc[OwnerWhoAmI].active
                || Main.npc[OwnerWhoAmI].ModNPC is not Artorias artorias)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 600; // owner-driven lifetime; Artorias kills this explicitly on release

            Lighting.AddLight(Projectile.Center, Color.White.ToVector3() * 1.25f);

            Vector2 tip = artorias.GetSwordTipWorldPosition();
            Projectile.Center = tip;
            Projectile.rotation = (tip - Main.npc[OwnerWhoAmI].Center).ToRotation() + MathHelper.PiOver2;

            UpdateBackBlood();

            if (TargetWhoAmI < 0 || TargetWhoAmI >= Main.maxPlayers)
            {
                return;
            }

            Player target = Main.player[TargetWhoAmI];
            if (!target.active || target.dead)
            {
                return;
            }

            var modPlayer = target.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.ImpaleFreezeTimer = 10;
            modPlayer.ImpaleWorldPosition = tip;
            modPlayer.ImpaleDrawRotation = artorias.GetImpalePlayerRotation();

            if (!Main.dedServ && --_backBloodSpawnTimer <= 0)
            {
                _backBloodSpawnTimer = 3; // matches DoPierceStabHoldTick's exit-side spray cadence
                // Toward Artorias, i.e. the opposite of the exit-side spray's direction.
                Vector2 backDirection = (Main.npc[OwnerWhoAmI].Center - target.Center)
                    .SafeNormalize(new Vector2(-artorias.NPC.direction, 0f));
                for (int i = 0; i < 2; i++)
                {
                    _backBlood.Add(new BackBloodDrop
                    {
                        Position = target.Center + Main.rand.NextVector2Circular(7f, 10f),
                        Velocity = backDirection.RotatedByRandom(0.4f) * Main.rand.NextFloat(1.8f, 4.8f),
                        MaxLife = Main.rand.NextFloat(24f, 36f),
                        Life = 0f,
                        Scale = Main.rand.NextFloat(2.4f, 4.2f),
                    });
                }
            }
        }

        void UpdateBackBlood()
        {
            for (int i = _backBlood.Count - 1; i >= 0; i--)
            {
                BackBloodDrop drop = _backBlood[i];
                drop.Life++;
                drop.Position += drop.Velocity;
                drop.Velocity.Y += 0.15f; // gravity, matching the exit-side Dust.Blood (noGravity = false)
                drop.Velocity *= 0.96f;
                if (drop.Life >= drop.MaxLife)
                {
                    _backBlood.RemoveAt(i);
                    continue;
                }
                _backBlood[i] = drop;
            }
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            // Over the players: the blade goes in FRONT of the impaled player (he hangs on it, sideways), and the
            // impale burst + blood draw on top of the blade. Artorias's own pass (behind players) still draws the
            // sword too; the redraw below is pixel-identical so it just covers the player where they overlap.
            overPlayers.Add(index);
        }

        // The held greatsword already follows this exact raise-and-flick pose, but the puppet draws it BEHIND
        // players. Redrawing Artorias's final weapon DrawData here (same texture/position/rotation/scale, so no
        // "duplicate sword") puts the blade over the player. Order: blade -> impale burst shader -> blood.
        public override bool PreDraw(ref Color lightColor)
        {
            if (OwnerWhoAmI >= 0 && OwnerWhoAmI < Main.maxNPCs && Main.npc[OwnerWhoAmI].active
                && Main.npc[OwnerWhoAmI].ModNPC is Artorias artorias && artorias.IsImpaleHoldActive)
            {
                DrawBladeOverPlayer(artorias);

                if (TargetWhoAmI >= 0 && TargetWhoAmI < Main.maxPlayers && Main.player[TargetWhoAmI].active)
                {
                    Player impaled = Main.player[TargetWhoAmI];

                    // The blade points from Artorias THROUGH the impaled target - the wind wisps
                    // stream on out that far side, not radially.
                    Vector2 windDirection = (impaled.Center - artorias.NPC.Center)
                        .SafeNormalize(new Vector2(artorias.NPC.direction, 0f));
                    ArtoriasVFX.DrawImpaleTendrils(
                        impaled.Center, windDirection, artorias.GetImpaleRaiseProgress01(), 0.86f);
                }
            }

            DrawBackBlood();
            return false;
        }

        void DrawBladeOverPlayer(Artorias artorias)
        {
            // Only trust a capture from this same update; a stale one would draw the blade where it WAS.
            if (artorias.LastHeldWeaponDrawUpdate != Main.GameUpdateCount)
            {
                return;
            }

            DrawData bladeDraw = artorias.LastHeldWeaponDraw;
            Texture2D bladeTexture = bladeDraw.texture;
            if (bladeTexture == null || bladeDraw.sourceRect.HasValue)
            {
                return;
            }

            // Skip the grip: re-covering it would paint the blade over Artorias's own hand (the arm is drawn after
            // the weapon in his pass). origin is the handle pixel in pre-flip texture space, the blade runs up-and-
            // away from it (away = +X unflipped, -X flipped), so keep only the quadrant beyond a margin from it.
            // The player is ~70px down the blade, so the cut is never near the overlap and the seam is invisible.
            float gripMargin = 0.12f * Math.Max(bladeTexture.Width, bladeTexture.Height);
            bool flippedHorizontally = bladeDraw.effect.HasFlag(SpriteEffects.FlipHorizontally);

            int keepLeft = 0;
            int keepWidth = (int)(bladeDraw.origin.X - gripMargin);
            if (!flippedHorizontally)
            {
                keepLeft = (int)(bladeDraw.origin.X + gripMargin);
                keepWidth = bladeTexture.Width - keepLeft;
            }

            int keepHeight = (int)(bladeDraw.origin.Y - gripMargin);
            if (keepWidth <= 0 || keepHeight <= 0 || bladeDraw.effect.HasFlag(SpriteEffects.FlipVertically))
            {
                return;
            }

            bladeDraw.sourceRect = new Rectangle(keepLeft, 0, keepWidth, keepHeight);
            bladeDraw.origin = new Vector2(bladeDraw.origin.X - keepLeft, bladeDraw.origin.Y);
            bladeDraw.Draw(Main.spriteBatch);
        }

        void DrawBackBlood()
        {
            if (_backBlood.Count == 0)
            {
                return;
            }

            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Rectangle frame = new(0, 0, 1, 1);
            foreach (BackBloodDrop drop in _backBlood)
            {
                float fade = 1f - drop.Life / drop.MaxLife;
                Color color = new Color(120, 10, 24) * fade;
                Main.EntitySpriteDraw(pixel, drop.Position - Main.screenPosition, frame, color,
                    0f, frame.Size() * 0.5f, drop.Scale, SpriteEffects.None, 0);
            }
        }
    }
}
