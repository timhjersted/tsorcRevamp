using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    public class CruelArrow : ModProjectile {

        public override string Texture => "tsorcRevamp/Items/Ammo/CruelArrow";
        public override void SetDefaults() {
            projectile.aiStyle = 1;
            projectile.friendly = true;
            projectile.height = 10;
            projectile.damage = 10;
            projectile.penetrate = 2;
            projectile.ranged = true;
            projectile.scale = (float)1;
            projectile.tileCollide = true;
            projectile.width = 5;
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
            Collision.HitTiles(projectile.position + projectile.velocity, projectile.velocity, projectile.width, projectile.height);
            Main.PlaySound(SoundID.Dig, projectile.position);
        }

    }
}
