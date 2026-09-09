using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Bosses.SuperHardMode;

namespace tsorcRevamp.Projectiles.Enemy
{
    // Purely visual: the actual stab/bonus damage and heal are applied directly by Artorias
    // (OnPierceContact) the instant the dash connects. This projectile just tracks the sword tip
    // position Artorias computes (GetSwordTipWorldPosition) and anchors the impaled player to it,
    // drawn behind the player sprite so it reads as skewering through them.
    public class ArtoriasImpalingSword : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Items/Weapons/Melee/Broadswords/ArtoriasGreatsword";

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
            // Deliberately not added to overPlayers - renders behind the impaled player's sprite.
            behindNPCs.Add(index);
        }

        // The held greatsword already follows this exact raise-and-flick pose. This helper only
        // anchors the player; drawing its texture as well produced a duplicate sword.
        public override bool PreDraw(ref Color lightColor)
        {
            if (OwnerWhoAmI >= 0 && OwnerWhoAmI < Main.maxNPCs && Main.npc[OwnerWhoAmI].active
                && Main.npc[OwnerWhoAmI].ModNPC is Artorias artorias)
            {
                Vector2 start = Projectile.Center;
                if (TargetWhoAmI >= 0 && TargetWhoAmI < Main.maxPlayers && Main.player[TargetWhoAmI].active)
                {
                    start = Main.player[TargetWhoAmI].Center;
                }
                ArtoriasVFX.DrawTendril(start, artorias.NPC.Center,
                    artorias.GetImpaleRaiseProgress01(), 0.48f, hostileTip: false);
            }

            DrawBackBlood();
            return false;
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
