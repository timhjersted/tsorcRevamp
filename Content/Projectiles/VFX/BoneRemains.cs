using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Enemies;

namespace tsorcRevamp.Content.Projectiles.VFX
{
    /// <summary>
    /// The bones a crumbled bone-pile skeleton leaves behind (SkeletonPuppet.Crumble): no gameplay, lies 10s then fades.
    /// The NPC is already dead (loot dropped), so this harmless projectile carries the pile. It draws the skeleton
    /// type's own bones and props through its ContentSamples template.
    /// ai0 = skeleton NPC type, ai1 = facing (+1/-1), ai2 = starting age (0 plays the 12t fall from standing).
    /// </summary>
    public class BoneRemains : ModProjectile
    {
        // 600t (10s) lying, the last 90t of it fading out.
        const int LifetimeTicks = 600;
        const int FadeTicks = 90;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 16;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.damage = 0;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = LifetimeTicks;
            Projectile.aiStyle = -1;
        }

        public override bool? CanDamage()
        {
            return false;
        }

        public override void AI()
        {
            // localAI0 counts this machine's age from the server-given start, so a late joiner shows a rested pile.
            Projectile.velocity = Vector2.Zero;
            Projectile.localAI[0]++;

            // A last trickle of bone dust while it fades.
            bool fading = Projectile.timeLeft <= FadeTicks;
            if (!Main.dedServ && fading && Projectile.timeLeft % 10 == 0)
            {
                Vector2 spot = Projectile.Center + new Vector2(Main.rand.NextFloat(-14f, 14f), -6f);
                Dust.NewDustPerfect(spot, DustID.Bone, new Vector2(0f, -0.4f), 100, default, 0.7f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int skeletonType = (int)Projectile.ai[0];
            bool validType = skeletonType > 0 && skeletonType < NPCLoader.NPCCount;
            if (!validType)
            {
                return false;
            }

            if (ContentSamples.NpcsByNetId[skeletonType].ModNPC is not SkeletonPuppet template)
            {
                return false;
            }

            int facing = 1;
            if (Projectile.ai[1] < 0f)
            {
                facing = -1;
            }

            int age = (int)Projectile.ai[2] + (int)Projectile.localAI[0];
            float opacity = MathHelper.Clamp(Projectile.timeLeft / (float)FadeTicks, 0f, 1f);
            template.DrawRemains(Main.spriteBatch, Main.screenPosition, Projectile.Center, facing, age, opacity);
            return false;
        }
    }
}
