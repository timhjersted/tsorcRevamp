using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    class MediumWeaponSlash : ModProjectile //Slightly bigger than Small Weapon Slash, but also cripples the player
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj)); //invis so doesnt matter

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.alpha = 255; //invis
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 12;
        }

        int difference;
        public override void AI()
        {
            NPC owner = Main.npc[(int)Projectile.ai[0]];

            if (Projectile.ai[1] < 1)
            {
                ++Projectile.ai[1];
                difference = (int)Projectile.Center.X - (int)owner.Center.X;
            }

            if (Projectile.ai[1] >= 1)
            {
                //Create a new Vector2 with length offsetDistance, and then rotate it toward the correct direction
                //Add that to the npc's position
                if (owner.direction == 1)
                {
                    Projectile.position.X = owner.Center.X + difference - 10;
                }
                else
                {
                    Projectile.position.X = owner.Center.X - difference - 42;
                }
            }
        }

        // Which enemy's slash this is, passed as ai[2] (synced). Default: Hollow Soldier, Lothric Black Knight, Ringed Knight.
        public const int VariantDefault = 0;
        public const int VariantLothricKnight = 1;

        // Lothric Knight: 3s each in Normal; Broken Armor stretches in Expert/Master itself, Crippled via ScaledTicks.
        private const int LothricKnightDebuffTicks = 3 * 60;

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            int variant = (int)Projectile.ai[2];

            if (variant == VariantLothricKnight)
            {
                target.AddBuff(ModContent.BuffType<Crippled>(), Crippled.ScaledTicks(LothricKnightDebuffTicks));
                target.AddBuff(Terraria.ID.BuffID.BrokenArmor, LothricKnightDebuffTicks);
                return;
            }

            target.AddBuff(ModContent.BuffType<Crippled>(), 600);
        }
    }
}