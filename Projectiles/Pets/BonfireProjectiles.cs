using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Pets
{
    public abstract class BonfireProjectiles : ModProjectile
    {

        public abstract int ChestType { get; }
        public abstract int ItemType { get; }
        public virtual void SetWhoAmI(tsorcRevampPlayer player, int value) => player.chestBank = value;

        public abstract SoundStyle UseSound { get; }

        public override void AI()
        {
            if (Projectile.ai[0] == 0f)
            {
                if (Projectile.velocity.Length() < 0.1)
                {
                    Projectile.velocity.X = 0f;
                    Projectile.velocity.Y = 0f;
                    Projectile.ai[0] = 1f;
                    Projectile.ai[1] = 45f;
                    return;
                }

                Projectile.velocity *= 0.94f;
                if (Projectile.velocity.X < 0f)
                {
                    Projectile.direction = -1;
                }
                else
                {
                    Projectile.direction = 1;
                }

                Projectile.spriteDirection = Projectile.direction;
                return;
            }

            if (Main.player[Projectile.owner].Center.X < Projectile.Center.X)
            {
                Projectile.direction = -1;
            }
            else
                Projectile.direction = 1;

            Projectile.spriteDirection = Projectile.direction;
            Projectile.ai[1] += 1f;
            float acceleration = 0.005f;
            if (Projectile.ai[1] > 0f)
            {
                Projectile.velocity.Y -= acceleration;
            }
            else
            {
                Projectile.velocity.Y += acceleration;
            }

            if (Projectile.ai[1] >= 90f)
            {
                Projectile.ai[1] *= -1f;
            }

            StorageProjectileAI(Projectile);
        }

        internal void StorageProjectileAI(Projectile proj)
        {
            Player p = Main.LocalPlayer;
            if (Main.gamePaused && !Main.gameMenu)
            {
                return;
            }
            Vector2 projPosRelative = proj.position - Main.screenPosition;
            if (!(Main.mouseX > projPosRelative.X) || !(Main.mouseX < projPosRelative.X + proj.width) || !(Main.mouseY > projPosRelative.Y) || !(Main.mouseY < projPosRelative.Y + proj.height))
            {
                return;
            }

            int pTilePosX = (int)(p.Center.X / 16.0);
            int pTilePosY = (int)(p.Center.Y / 16.0);
            int projTilePosX = (int)proj.Center.X / 16;
            int projTilePosY = (int)proj.Center.Y / 16;
            int lastTileRangeX = p.lastTileRangeX;
            int lastTileRangeY = p.lastTileRangeY;
            if (pTilePosX < projTilePosX - lastTileRangeX || pTilePosX > projTilePosX + lastTileRangeX + 1 || pTilePosY < projTilePosY - lastTileRangeY || pTilePosY > projTilePosY + lastTileRangeY + 1)
            {
                return;
            }


            p.noThrow = 2;
            p.cursorItemIconEnabled = true;
            p.cursorItemIconID = ItemType;
            if (PlayerInput.UsingGamepad)
            {
                p.GamepadEnableGrappleCooldown();
            }
            if (!Main.mouseRight || !Main.mouseRightRelease || Player.StopMoneyTroughFromWorking != 0)
            {
                return;
            }
            Main.mouseRightRelease = false;
            p.tileInteractAttempted = true;
            p.tileInteractionHappened = true;
            p.releaseUseTile = false;
            if (p.chest == ChestType)
            {
                Terraria.Audio.SoundEngine.PlaySound(UseSound);
                p.chest = -1;
                SetWhoAmI(p.GetModPlayer<tsorcRevampPlayer>(), -1);
                Recipe.FindRecipes();
                return;
            }

            SetWhoAmI(p.GetModPlayer<tsorcRevampPlayer>(), proj.whoAmI);

            p.chest = ChestType;
            p.chestX = projTilePosX;
            p.chestY = projTilePosY;
            p.talkNPC = -1;
            Main.npcShop = 0;
            Main.playerInventory = true;
            Terraria.Audio.SoundEngine.PlaySound(UseSound);
            Recipe.FindRecipes();


        }
    }
    public class SafeProjectile : BonfireProjectiles
    {
        public override int ChestType => -3;
        public override int ItemType => ItemID.Safe;
        public override SoundStyle UseSound => SoundID.Item37;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Soul Safe");
            Main.projFrames[Projectile.type] = 10;
        }
        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 38;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 10800;
            Projectile.alpha = 120;
        }
        public override void AI()
        {
            if (Projectile.alpha <= 200)
            {
                Lighting.AddLight(Projectile.Center, 0.15f, 0.6f, 0.32f);
            }
            if (Projectile.alpha > 200)
            {
                Lighting.AddLight(Projectile.Center, 0.1f, 0.45f, 0.21f);
            }

            if (++Projectile.frameCounter >= 8)
            {
                Projectile.frameCounter = 0;
                if (++Projectile.frame >= 10)
                {
                    Projectile.frame = 0;
                }
            }

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (Main.player[i] != null && Main.player[i].active)
                {
                    if (Main.player[i].Distance(Projectile.Center) >= 350f && Main.rand.Next(2) == 0)
                    {
                        Projectile.alpha += 1;
                    }

                    if ((Main.player[i].Distance(Projectile.Center) <= 300f) && Projectile.alpha >= 120)
                    {
                        Projectile.alpha -= 2;
                    }
                }
            }

            if (Projectile.alpha == 255)
            {
                Projectile.timeLeft = 0;
            }


            base.AI();
        }
    }
    public class PiggyBankProjectile : BonfireProjectiles
    {
        public override int ChestType => -2;
        public override int ItemType => ItemID.PiggyBank;
        public override SoundStyle UseSound => SoundID.Item59;

        public override void SetWhoAmI(tsorcRevampPlayer player, int value)
        {
            player.chestPiggy = value;
            player.chestBank = -1;
            player.chestBankOpen = false;
        }
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Soul Piglett");
            Main.projFrames[Projectile.type] = 10;
        }
        public override void SetDefaults()
        {
            Projectile.height = 24;
            Projectile.width = 24;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 10800;
            Projectile.alpha = 120;
        }

        public override void AI()
        {
            if (Projectile.alpha <= 200)
            {
                Lighting.AddLight(Projectile.Center, 0.15f, 0.6f, 0.32f);
            }
            if (Projectile.alpha > 200)
            {
                Lighting.AddLight(Projectile.Center, 0.1f, 0.45f, 0.21f);
            }

            if (++Projectile.frameCounter >= 10)
            {
                Projectile.frameCounter = 0;
                if (++Projectile.frame >= 10)
                {
                    Projectile.frame = 0;
                }
            }

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (Main.player[i] != null && Main.player[i].active)
                {
                    if (Main.player[i].Distance(Projectile.Center) >= 350f && Main.rand.Next(2) == 0)
                    {

                        Projectile.alpha += 1;

                    }

                    if ((Main.player[i].Distance(Projectile.Center) <= 300f) && Projectile.alpha >= 120)
                    {
                        Projectile.alpha -= 2;
                    }
                }
            }

            if (Projectile.alpha == 255)
            {
                Projectile.timeLeft = 0;
            }


            base.AI();
        }
    }
}
