using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    public class CruelArrow : ModProjectile {

        public override string Texture => "tsorcRevamp/Items/Ammo/CruelArrow";
        public override void SetDefaults() {
            Projectile.aiStyle = 1;
            Projectile.friendly = true;
            Projectile.height = 10;
            Projectile.damage = 10;
            Projectile.penetrate = 2;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.scale = (float)1;
            Projectile.tileCollide = true;
            Projectile.width = 5;
        }


        public void OnHitNPC(Player P, NPC npc, int damage, float KB, bool crit )
        {
            if (npc.type == ModContent.NPCType<NPCs.Enemies.GhostoftheForgottenKnight>()) damage *= 8;
            if (npc.type == ModContent.NPCType<NPCs.Enemies.GhostOfTheForgottenWarrior>()) damage *= 8;
            if (npc.type == ModContent.NPCType<NPCs.Enemies.DemonSpirit>()) damage *= 8;
            if (npc.type == ModContent.NPCType<NPCs.Enemies.CrazedDemonSpirit>()) damage *= 8;
            if (npc.type == ModContent.NPCType<NPCs.Enemies.BarrowWight>()) damage *= 8;
            if (npc.type == ModContent.NPCType<NPCs.Enemies.SuperHardMode.BarrowWightNemesis>()) damage *= 8;
            if (npc.type == ModContent.NPCType<NPCs.Enemies.SuperHardMode.BarrowWightPhantom>()) damage *= 8;
            if (npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.WyvernMageShadow>()) damage *= 8;
            if (npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.GhostDragonHead>()) damage *= 8;
            if (npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.GhostDragonBody>()) damage *= 8;
            if (npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.GhostDragonBody2>()) damage *= 8;
            if (npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.GhostDragonBody3>()) damage *= 8;
            if (npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.GhostDragonLegs>()) damage *= 8;
            if (npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.GhostDragonTail>()) damage *= 8;
            if (npc.type == ModContent.NPCType<NPCs.Enemies.GhostOfTheDarkmoonKnight>()) damage *= 8;
        }

        public override void Kill(int timeLeft) {
            Collision.HitTiles(Projectile.position + Projectile.velocity, Projectile.velocity, Projectile.width, Projectile.height);
            Main.PlaySound(SoundID.Dig, Projectile.position);
        }

    }
}
