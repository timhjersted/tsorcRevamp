using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
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
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            // Deliberately not added to overPlayers - renders behind the impaled player's sprite.
            behindNPCs.Add(index);
        }
    }
}
