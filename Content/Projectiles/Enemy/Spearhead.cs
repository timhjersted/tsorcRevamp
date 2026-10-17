using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    class Spearhead : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj)); //invis so doesnt matter

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.alpha = 255; //invis
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 35;
        }

        Vector2 difference;
        public override void AI()
        {
            NPC owner = Main.npc[(int)Projectile.ai[0]];

            if (Projectile.ai[1] < 1)
            {
                ++Projectile.ai[1];
                difference = Projectile.Center - owner.Center;
            }

            if (Projectile.ai[1] >= 1 && Projectile.ai[1] < 3)
            {
                //Create a new Vector2 with length offsetDistance, and then rotate it toward the correct direction
                //Add that to the npc's position
                if (owner.direction == 1)
                {
                    Projectile.Center = owner.Center + difference;
                }
                else
                {
                    Projectile.Center = owner.Center + difference;
                }
            }
        }

        // Which enemy's stab this is, passed as ai[2] (synced, so every client picks the same debuffs).
        public const int VariantDefault = 0;              // Lothric Black Knight, Ringed Knight
        public const int VariantHollowSpearman = 1;
        public const int VariantGhostOfTheDrowned = 2;
        public const int VariantLothricKnight = 3;
        public const int VariantLothricSpearKnight = 4;
        public const int VariantLothricSpearKnightEnraged = 5; // spear knight below 50% life, chosen at spawn

        // Normal-mode ticks. Broken Armor and Bleeding stretch in Expert/Master on their own; Crippled goes through
        // Crippled.ScaledTicks to match them.
        private const int HollowSpearmanCrippledTicks = 3 * 60;
        private const int LothricKnightDebuffTicks = 3 * 60;
        private const int LothricSpearKnightDebuffTicks = 3 * 60;
        private const int LothricSpearKnightEnragedDebuffTicks = 6 * 60;

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            int variant = (int)Projectile.ai[2];
            int crippledType = ModContent.BuffType<Crippled>();

            if (variant == VariantDefault)
            {
                target.AddBuff(crippledType, 600);
            }
            else if (variant == VariantHollowSpearman)
            {
                target.AddBuff(crippledType, Crippled.ScaledTicks(HollowSpearmanCrippledTicks));
            }
            else if (variant == VariantLothricKnight)
            {
                target.AddBuff(crippledType, Crippled.ScaledTicks(LothricKnightDebuffTicks));
                target.AddBuff(BuffID.BrokenArmor, LothricKnightDebuffTicks);
            }
            else if (variant == VariantLothricSpearKnight || variant == VariantLothricSpearKnightEnraged)
            {
                int debuffTicks = LothricSpearKnightDebuffTicks;
                if (variant == VariantLothricSpearKnightEnraged)
                {
                    debuffTicks = LothricSpearKnightEnragedDebuffTicks;
                }

                target.AddBuff(crippledType, Crippled.ScaledTicks(debuffTicks));
                target.AddBuff(BuffID.BrokenArmor, debuffTicks);
                target.AddBuff(BuffID.Bleeding, debuffTicks);
            }
            else if (variant == VariantGhostOfTheDrowned)
            {
                SoundEngine.PlaySound(SoundID.Drown, target.Center);
                target.AddBuff(BuffID.Darkness, 8 * 60);
                target.AddBuff(BuffID.BrokenArmor, 8 * 60);
                target.AddBuff(BuffID.Chilled, 8 * 60);
                target.AddBuff(ModContent.BuffType<Gilled>(), 16 * 60);
            }
        }
    }
}