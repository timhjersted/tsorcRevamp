using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Prime
{
    class HomingMissileTrail : Projectiles.VFX.DynamicTrail
    {
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            // DisplayName.SetDefault("Ichor Trail");
        }
        public override void SetDefaults()
        {
            Projectile.damage = 0;
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 99999999;
            Projectile.penetrate = -1;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.hide = true;
            trailWidth = 110;
            trailPointLimit = 50;
            trailCollision = false;
            NPCSource = true;
            trailYOffset = 50;
            trailMaxLength = 200;
            deathSpeed = 1f / 20f;
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/MoltenWeld", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;

        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCs.Add(index);
        }
        public override void SetEffectParameters(Effect effect)
        {
            trailWidth = 35;
            effect.Parameters["effectColor"].SetValue(new Vector4(1.0f, 0.3f, 0.05f, 1) * fadeOut);
            effect.Parameters["start"].SetValue(0.7f);
            effect.Parameters["end"].SetValue(0.90f);
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
        }
    }
}