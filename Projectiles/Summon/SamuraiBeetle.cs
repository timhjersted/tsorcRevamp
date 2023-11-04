using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Weapons.Summon;

namespace tsorcRevamp.Projectiles.Summon
{
    public class SamuraiBeetle : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 36;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;

            Main.projPet[Projectile.type] = true;

            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.CultistIsResistantTo[Projectile.type] = true;
        }

        public sealed override void SetDefaults()
        {
            Projectile.width = 100;
            Projectile.height = 64;
            Projectile.penetrate = -1;
            Projectile.netImportant = true;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.timeLeft *= 5;
            Projectile.minion = true;
            Projectile.minionSlots = 1f;
            Projectile.friendly = true;
            Projectile.decidesManualFallThrough = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.tileCollide = true;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
        }
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            //Projectile.velocity.Y = 1;
            //Projectile.velocity.X = Projectile.Center.DirectionTo(player.Center).X;
            Projectile.spriteDirection = (int)(Projectile.Center.DirectionTo(player.Center).X > 0 ? 1 : -1);
            Collision.StepUp(ref Projectile.position, ref Projectile.velocity, Projectile.width, Projectile.height, ref Projectile.stepSpeed, ref Projectile.gfxOffY);

            if (!CheckActive(player))
            {
                return;
            }
        } // This is the "active check", makes sure the minion is alive while the player is alive, and despawns if not
        private bool CheckActive(Player owner)
        {
            if (owner.dead || !owner.active)
            {
                owner.ClearBuff(ModContent.BuffType<SamuraiBeetleBuff>());

                return false;
            }

            if (owner.HasBuff(ModContent.BuffType<SamuraiBeetleBuff>()))
            {
                Projectile.timeLeft = 2;
            }

            return true;
        }
        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
        {
            width = (int)(width * 0.25f);
            height = (int)(height * 0.9f);
            fallThrough = false;
            return true;
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            return false;
        }
        public override bool? CanCutTiles()
        {
            return false;
        }
        public override bool MinionContactDamage()
        {
            return true;
        }
    }
}