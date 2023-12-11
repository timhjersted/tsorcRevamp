using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.UI;

namespace tsorcRevamp.Projectiles.Melee.Swords
{
    public class WorldEnderSword : ModProjectile
    {
        public Vector2 Velocity;
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 3;
        }
        public override void SetDefaults()
        {
            Projectile.width = 250;
            Projectile.height = 333;
            Projectile.aiStyle = -1;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 60;
            Projectile.ownerHitCheck = true; // Prevents hits through tiles. Most melee weapons that use projectiles have this
            Projectile.extraUpdates = 1; // Update 1+extraUpdates times per tick
            Projectile.penetrate = -1;
        }
        public override void OnSpawn(IEntitySource source)
        {
            Player player = Main.player[Projectile.owner];
            Velocity = Projectile.velocity;
            Projectile.spriteDirection = 1;
            Projectile.frame = (int)Projectile.ai[0] - 1;
            Projectile.velocity = Vector2.Zero;
        }
        public Vector2 CritHitboxCenter1 = Vector2.Zero;
        public Vector2 CritHitboxCenter2 = Vector2.Zero;
        public Vector2 CritHitboxCentered = Vector2.Zero;
        public Vector2 CritHitboxCenter3 = Vector2.Zero;
        public Vector2 CritHitboxCenter4 = Vector2.Zero;
        public Vector2 CritHitboxSize = Vector2.Zero;
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            Projectile.rotation = Velocity.ToRotation();

            player.heldProj = Projectile.whoAmI;

            // Keep locked onto the player, but extend further based on the given velocity (Requires ShouldUpdatePosition returning false to work)
            Vector2 playerCenter = player.RotatedRelativePoint(player.MountedCenter, reverseRotation: false, addGfxOffY: false);
            Projectile.Center = playerCenter + Velocity;
            switch (Projectile.ai[0])
            {
                case 1:
                    {
                        CritHitboxCentered = playerCenter + (Velocity * 1.7f);
                        CritHitboxCenter1 = Vector2.Zero;
                        CritHitboxCenter2 = Vector2.Zero;
                        CritHitboxCenter3 = Vector2.Zero;
                        CritHitboxCenter4 = Vector2.Zero;
                        CritHitboxSize = new Vector2(84, 84);
                        break;
                    }
                case 2:
                    {
                        CritHitboxCenter1 = playerCenter + (Velocity * 1.65f).RotatedBy(-0.6);
                        CritHitboxCenter2 = playerCenter + (Velocity * 1.44f).RotatedBy(-0.33);
                        CritHitboxCentered = playerCenter + (Velocity * 1.35f);
                        CritHitboxCenter3 = playerCenter + (Velocity * 1.44f).RotatedBy(0.33);
                        CritHitboxCenter4 = playerCenter + (Velocity * 1.65f).RotatedBy(0.6);
                        CritHitboxSize = new Vector2(80, 80);
                        break;
                    }
                case 3:
                    {
                        CritHitboxCentered = playerCenter + (Velocity * 0.85f);
                        CritHitboxCenter1 = Vector2.Zero;
                        CritHitboxCenter2 = Vector2.Zero;
                        CritHitboxCenter3 = Vector2.Zero;
                        CritHitboxCenter4 = Vector2.Zero;
                        CritHitboxSize = new Vector2(84, 84);
                        break;
                    }
            }
            Dust.NewDustPerfect(CritHitboxCenter1, DustID.MartianSaucerSpark, null, 0, default, 0.5f);
            Dust.NewDustPerfect(CritHitboxCenter2, DustID.MartianSaucerSpark, null, 0, default, 0.5f);
            Dust.NewDustPerfect(CritHitboxCentered, DustID.MartianSaucerSpark, null, 0, default, 0.5f);
            Dust.NewDustPerfect(CritHitboxCenter3, DustID.MartianSaucerSpark, null, 0, default, 0.5f);
            Dust.NewDustPerfect(CritHitboxCenter4, DustID.MartianSaucerSpark, null, 0, default, 0.5f);
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Utils.CenteredRectangle(CritHitboxCenter1, CritHitboxSize).Intersects(target.Hitbox) || Utils.CenteredRectangle(CritHitboxCenter2, CritHitboxSize).Intersects(target.Hitbox) || Utils.CenteredRectangle(CritHitboxCentered, CritHitboxSize).Intersects(target.Hitbox) || Utils.CenteredRectangle(CritHitboxCenter3, CritHitboxSize).Intersects(target.Hitbox) || Utils.CenteredRectangle(CritHitboxCenter4, CritHitboxSize).Intersects(target.Hitbox))
            {
                modifiers.SourceDamage *= 2;
            }
        }
    }
}
