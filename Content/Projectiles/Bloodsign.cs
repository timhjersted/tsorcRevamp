using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles
{
    class Bloodsign : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Bloodsign");
            Main.projFrames[Projectile.type] = 25;
        }
        public override void SetDefaults()
        {
            Projectile.friendly = true;
            Projectile.width = 64;
            Projectile.height = 98;
            Projectile.penetrate = -1;
            Projectile.scale = 1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 216000; // 60 minutes at 60 ticks/s; when it runs out OnKill clears the owner's Hollowed
            Projectile.alpha = 254; //start nearly invis
        }
        public float AI_Projectile_Lifetime
        {
            get => Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }

        public bool playerReturned = false;
        public override void AI()
        {
            AI_Projectile_Lifetime += 1f;

            var player = Main.player[Projectile.owner];

            if ((player.Distance(Projectile.Center) < 360f) && !player.dead) //kill when player returns.
            {
                playerReturned = true;
                if (player.HasBuff(ModContent.BuffType<Buffs.Debuffs.Hollowed>()))
                {
                    player.ClearBuff(ModContent.BuffType<Buffs.Debuffs.Hollowed>());
                }
            }

            if (playerReturned)
            {
                Projectile.alpha += 1;
                if (Projectile.alpha > 254)
                {
                    Projectile.Kill();
                }
            }
            if (AI_Projectile_Lifetime < 100)
            {
                Projectile.alpha -= 4; //increase visibility
            }

            //movement
            if (AI_Projectile_Lifetime <= 60)
            {
                Projectile.velocity.Y = -.9f; //float up for 1 second
            }
            else
            {
                Projectile.velocity.Y = 0f; //stop upwards velocity
            }

            //animation

            if (++Projectile.frameCounter >= 5)
            {
                Projectile.frameCounter = 0;
                if (++Projectile.frame >= 25)
                {
                    Projectile.frame = 0;
                }
            }

        }
        public override void OnSpawn(IEntitySource source)
        {
            // One stain per player: a new death replaces the old one. Runs where the stain is created (singleplayer, the
            // server's world-load restore, or the owner's client); other clients just receive the old stain's removal.
            bool createdHere = Main.netMode != NetmodeID.MultiplayerClient || Projectile.owner == Main.myPlayer;

            if (!createdHere)
            {
                return;
            }

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                bool oldStain = other.active && i != Projectile.whoAmI && other.type == Projectile.type && other.owner == Projectile.owner;

                if (oldStain)
                {
                    other.Kill();
                }
            }
        }

        public override void OnKill(int timeLeft)
        {
            // timeLeft > 0 means the player returned (AI fades it out and kills it) or something else killed it; only a natural
            // expiry (0) lifts the curse. Runs on every client, so only the owner's client clears their own buff.
            bool expired = timeLeft <= 0;
            bool isOwnerClient = Projectile.owner == Main.myPlayer;

            if (!expired || !isOwnerClient)
            {
                return;
            }

            Player owner = Main.player[Projectile.owner];

            if (owner.HasBuff(ModContent.BuffType<Buffs.Debuffs.Hollowed>()))
            {
                owner.ClearBuff(ModContent.BuffType<Buffs.Debuffs.Hollowed>());
            }
        }

        public override bool? CanDamage()
        {
            return false;
        }


    }
}