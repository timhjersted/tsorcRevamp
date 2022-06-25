using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class EphemeralThrowingAxe2 : ModProjectile
    {

        public override string Texture => "tsorcRevamp/Items/Weapons/Melee/EphemeralThrowingAxe";

        public override void SetDefaults()
        {
            Projectile.aiStyle = 2;
            Projectile.friendly = true;
            Projectile.height = 22;
            Projectile.penetrate = 1;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.width = 22;
            Projectile.timeLeft = 50;
        }

        public override void ModifyHitNPC(NPC target, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            //todo add mod NPCs to this list
            if (target.type == NPCID.Tim
                || target.type == NPCID.DarkCaster
                || target.type == NPCID.GoblinSorcerer
                //|| target.type == ModContent.NPCType<UndeadCaster>()
                //|| target.type == ModContent.NPCType<MindflayerServant>()
                //|| target.type == ModContent.NPCType<DungeonMage>()
                //|| target.type == ModContent.NPCType<DemonSpirit>()
                //|| target.type == ModContent.NPCType<CrazedDemonSpirit>()
                //|| target.type == ModContent.NPCType<ShadowMage>()
                //|| target.type == ModContent.NPCType<AttraidiesIllusion>()
                //|| target.type == ModContent.NPCType<AttraidiesManifestation>()
                //|| target.type == ModContent.NPCType<MindflayerKing>()
                //|| target.type == ModContent.NPCType<DarkShogunMask>()
                //|| target.type == ModContent.NPCType<DarkDragonMask>()
                //|| target.type == ModContent.NPCType<BrokenOkiku>()
                //|| target.type == ModContent.NPCType<Okiku>()
                //|| target.type == ModContent.NPCType<WyvernMage>()
                //|| target.type == ModContent.NPCType<LichKingDisciple>()
                //|| target.type == ModContent.NPCType<Attraidies>()
                //|| target.type == ModContent.NPCType<GhostOfTheForgottenKnight>()
                //|| target.type == ModContent.NPCType<GhostOfTheForgottenWarrior>()
                //|| target.type == ModContent.NPCType<BarrowWight>()
                )
            {
                damage *= 2;
            }
        }
        public override void AI()
        {
            Color color = new Color();
            int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 57, 0f, 0f, 80, color, 1f);
            Main.dust[dust].noGravity = true;
        }
        public override void Kill(int timeLeft)
        {

            if (!Projectile.active)
            {
                return;
            }
            Projectile.timeLeft = 0;
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
                for (int i = 0; i < 10; i++)
                {
                    Vector2 arg_92_0 = new Vector2(Projectile.position.X, Projectile.position.Y);
                    int arg_92_1 = Projectile.width;
                    int arg_92_2 = Projectile.height;
                    int arg_92_3 = 7;
                    float arg_92_4 = 0f;
                    float arg_92_5 = 0f;
                    int arg_92_6 = 0;
                    Color newColor = default(Color);
                    Dust.NewDust(arg_92_0, arg_92_1, arg_92_2, arg_92_3, arg_92_4, arg_92_5, arg_92_6, newColor, 1f);
                }
            }
            Projectile.active = false;

        }
    }
}
