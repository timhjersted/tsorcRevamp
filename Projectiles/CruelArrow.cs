using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    public class CruelArrow : ModProjectile {
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

        /* TODO add modified damage to special NPCs
        public void OnHitNPC(Player P, NPC npc, int damage, float KB, bool crit ){

            if (npc.name=="Ghost of the Forgotten Knight") damage *= 8;
            if (npc.name=="Ghost of the Forgotten Warrior") damage *= 8;
            if (npc.name=="Demon Spirit") damage *= 8;
            if (npc.name=="Crazed Demon Spirit") damage *= 8;
            if (npc.name=="Ghost Dragon Body") damage *= 8;
            if (npc.name=="Ghost Dragon Head") damage *= 8;
            if (npc.name=="Ghost Dragon Legs") damage *= 8;
            if (npc.name=="Wyvern Mage Shadow") damage *= 8;
            if (npc.name=="Barrow Wight") damage *= 8;
            if (npc.name=="Barrow Wight Nemesis") damage *= 8;
            if (npc.name=="Ghost of the Darkmoon Knight") damage *= 8;

        }

        */
        public override void Kill(int timeLeft) {
            Collision.HitTiles(projectile.position + projectile.velocity, projectile.velocity, projectile.width, projectile.height);
            Main.PlaySound(SoundID.Dig, projectile.position);
        }

    }
}
