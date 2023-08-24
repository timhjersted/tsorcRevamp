using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Ammo;

namespace tsorcRevamp.Projectiles.Ranged.Ammo
{
    public class CruelArrowProjectile : ModProjectile
    {

        public override string Texture => "tsorcRevamp/Items/Ammo/CruelArrow";
        public override void SetDefaults()
        {
            Projectile.aiStyle = 1;
            Projectile.friendly = true;
            Projectile.height = 10;
            Projectile.damage = 10;
            Projectile.penetrate = 1 + CruelArrow.Pierce;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = true;
            Projectile.width = 5;
        }


        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (target.type == ModContent.NPCType<NPCs.Enemies.GhostoftheForgottenKnight>() ||
                target.type == ModContent.NPCType<NPCs.Enemies.GhostOfTheForgottenWarrior>() ||
                target.type == ModContent.NPCType<NPCs.Enemies.DemonSpirit>() ||
                target.type == ModContent.NPCType<NPCs.Enemies.CrazedDemonSpirit>() ||
                target.type == ModContent.NPCType<NPCs.Enemies.BarrowWight>() ||
                target.type == ModContent.NPCType<NPCs.Enemies.SuperHardMode.BarrowWightNemesis>() ||
                target.type == ModContent.NPCType<NPCs.Enemies.SuperHardMode.BarrowWightPhantom>() ||
                target.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.WyvernMageShadow>() ||
                target.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.GhostDragonHead>() ||
                target.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.GhostDragonBody>() ||
                target.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.GhostDragonBody2>() ||
                target.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.GhostDragonBody3>() ||
                target.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.GhostDragonLegs>() ||
                target.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.GhostDragonTail>() ||
                target.type == ModContent.NPCType<NPCs.Enemies.GhostOfTheDarkmoonKnight>())
                modifiers.SourceDamage *= CruelArrow.DmgMult;
        }

        public override void Kill(int timeLeft)
        {
            Collision.HitTiles(Projectile.position + Projectile.velocity, Projectile.velocity, Projectile.width, Projectile.height);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, Projectile.position);
        }

    }
}
