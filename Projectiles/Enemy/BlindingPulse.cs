using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Projectiles.Enemy
{
    class BlindingPulse : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Items/Weapons/Melee/ThrowingAxe"; //invis so doesnt matter

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.alpha = 255; //invis
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 90;
        }

        public override void AI()
        {

        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info) //Inflicts Darkness and Chilled, and upgrades those to their stronger counterparts if hit again with previous debuffs active
        {
            if (target.HasBuff(BuffID.Darkness))
            {
                target.AddBuff(BuffID.Blackout, 10 * 60);
                target.ClearBuff(BuffID.Darkness);
            }
            else
            {
                target.AddBuff(BuffID.Darkness, 15 * 60);
            }
            if (target.HasBuff(BuffID.Chilled))
            {
                target.AddBuff(BuffID.Slow, 10 * 60);
                target.ClearBuff(BuffID.Chilled);
            }
            else
            {
                target.AddBuff(BuffID.Chilled, 15 * 60);
            }
        }
    }
}