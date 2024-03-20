using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Runeterra.Summon;
using tsorcRevamp.Items.Weapons.Summon.Runeterra;
using tsorcRevamp.Projectiles.VFX;

namespace tsorcRevamp.Projectiles.Melee.Axes
{
    class AncientFireAxeFireballBurst : ModProjectile
    {
        public int ProjectileLifetime = 40;
        public int Frames = 12;
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = Frames;
        }
        public override void SetDefaults()
        {
            Projectile.width = 60;
            Projectile.height = 60;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = ProjectileLifetime;
            Projectile.penetrate = -1;
            Projectile.hide = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 30;
        }
        public override void OnSpawn(IEntitySource source)
        {
            Projectile.CritChance = (int)Projectile.ai[0];
            Projectile.ai[0] = 0;
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindProjectiles.Add(index);
        }
        public override void AI()
        {
            int frameSpeed = (int)Math.Round((double)ProjectileLifetime / ((double)Frames));
            Projectile.frameCounter++;

            if (Projectile.frameCounter >= frameSpeed)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.rand.NextBool(2))
            {
                target.AddBuff(BuffID.OnFire, 5 * 60);
            }
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
        }
    }
}