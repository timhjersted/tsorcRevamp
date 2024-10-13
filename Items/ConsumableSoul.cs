using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items
{

    public abstract class ConsumableSoul : ModItem // These can be placed on little breakable corpses found around the map like in DS, or just placed in chests or rare enemy drops.
    {
        //all consumable souls found here. It's odd how the item quantity is consumed at a random tick during item use. They behave quite oddly but they work.
        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemIconPulse[Item.type] = true; // Makes item pulsate in world.
            ItemID.Sets.ItemNoGravity[Item.type] = true; // Makes item float in world.
        }

        public override void SetDefaults()
        {
            Item refItem = new Item();
            refItem.SetDefaults(ItemID.SoulofSight);
            Item.width = 14;
            Item.height = 22;
            Item.maxStack = Item.CommonMaxStack;
            Item.autoReuse = true;
            Item.useTurn = true;
            Item.value = 1;
            Item.consumable = true;
            Item.useAnimation = 30; // Needs to be 1 tick more than use time for it to work properly. Not sure why.
            Item.useTime = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.rare = ItemRarityID.Green; // Mainly for colour consistency.
        }
        public override void PostUpdate()
        {
            Lighting.AddLight(Item.Center, 0.15f, 0.42f, 0.05f);
        }
        public override bool GrabStyle(Player player)
        { //make pulling souls through walls more consistent
            Vector2 vectorItemToPlayer = player.Center - Item.Center;
            Vector2 movement = vectorItemToPlayer.SafeNormalize(default) * 10f;
            Item.velocity = movement;
            return true;
        }
        public override void GrabRange(Player player, ref int grabRange)
        {
            grabRange *= (2 + Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().SoulReaper);
        }

        public override bool? UseItem(Player player) // Won't consume item without this
        {
            return true;
        }
    }

    public class FadingSoul : ConsumableSoul
    {
        public override string Texture => "tsorcRevamp/Items/ConsumableSoul_Fading";
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            // DisplayName.SetDefault("Fading soul");
            // Tooltip.SetDefault("Consume to gain a mere 50 souls");
        }
        public override void PostUpdate()
        {
            Lighting.AddLight(Item.Center, 0.05f, 0.18f, 0.02f);
        }
        public override void UseStyle(Player player, Rectangle rectangle)
        {
            // Each frame, make some dust
            if (Main.rand.NextFloat() < .3f)
            {
                Dust.NewDust(player.BottomLeft, player.width, player.height - 40, 89, 0f, -5f, 100, default, .75f);
            }

            if (player.itemTime == 1)
            {
                // This code runs once halfway through the useTime of the item.
                Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath52 with { Volume = 0.25f, PitchVariance = 0.5f }, player.Center); // Plays sound.

                if (Main.player[Main.myPlayer].whoAmI == player.whoAmI)
                {
                    player.QuickSpawnItem(player.GetSource_DropAsItem(), ModContent.ItemType<DarkSoul>(), 50); // Gives player souls.
                }

                for (int d = 0; d < 10; d++)
                {
                    Dust.NewDust(player.BottomLeft, player.width, player.height - 40, 89, 0f, -5f, 80, default, .75f); // player.Bottom if offset to the right for some reason, player.BottomLeft is centered
                }
            }
        }
    }
    public class LostUndeadSoul : ConsumableSoul
    {
        public override string Texture => "tsorcRevamp/Items/ConsumableSoul_LostUndead";
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            // DisplayName.SetDefault("Soul of a Lost Undead");
            // Tooltip.SetDefault("Consume to gain 200 souls");
        }
        public override void UseStyle(Player player, Rectangle rectangle)
        {
            // Each frame, make some dust
            if (Main.rand.NextBool())
            {
                Dust.NewDust(player.BottomLeft, player.width, player.height - 40, 89, 0f, -5f, 80, default, .8f);
            }

            if (player.itemTime == 1)
            {
                // This code runs once halfway through the useTime of the item. 
                Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath52 with { Volume = 0.35f, PitchVariance = 0.3f }, player.Center); // Plays sound.
                if (Main.player[Main.myPlayer].whoAmI == player.whoAmI)
                {
                    player.QuickSpawnItem(player.GetSource_DropAsItem(), ModContent.ItemType<DarkSoul>(), 200); // Gives player souls.
                }

                for (int d = 0; d < 30; d++)
                {
                    Dust.NewDust(player.BottomLeft, player.width, player.height - 40, 89, 0f, -5f, 50, default, .8f); // player.Bottom if offset to the right for some reason, player.BottomLeft is centered
                }
            }
        }
    }

    public class NamelessSoldierSoul : ConsumableSoul
    {
        public override string Texture => "tsorcRevamp/Items/ConsumableSoul_NamelessSoldier";
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            // DisplayName.SetDefault("Soul of a Nameless Soldier");
            // Tooltip.SetDefault("Consume to gain 800 souls");
        }
        public override void UseStyle(Player player, Rectangle rectangle)
        {
            // Each frame, make some dust
            if (Main.rand.NextBool())
            {
                Dust.NewDust(player.BottomLeft, player.width, player.height - 40, 89, 0f, -5f, 50, default, .8f);
            }

            if (Main.rand.NextFloat() < .3f)
            {
                Dust.NewDust(player.BottomLeft, player.width, player.height - 40, 89, 0f, -5f, 100, default, .75f);
            }

            if (player.itemTime == 1)
            {
                // This code runs once halfway through the useTime of the item. 
                Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath52 with { Volume = 0.5f, PitchVariance = 0.3f }, player.Center); // Plays sound.

                if (Main.player[Main.myPlayer].whoAmI == player.whoAmI)
                {
                    player.QuickSpawnItem(player.GetSource_DropAsItem(), ModContent.ItemType<DarkSoul>(), 800); // Gives player souls.
                }

                for (int d = 0; d < 60; d++)
                {
                    Dust.NewDust(player.BottomLeft, player.width, player.height - 40, 89, 0f, -5f, 50, default, .8f); // player.Bottom if offset to the right for some reason, player.BottomLeft is centered
                }

                for (int d = 0; d < 15; d++) // Left
                {
                    Dust.NewDust(player.BottomLeft, player.width, player.height - 55, 89, -6f, -4f, 50, default, .65f); // player.Bottom if offset to the right for some reason, player.BottomLeft is centered
                }

                for (int d = 0; d < 15; d++) // Right
                {
                    Dust.NewDust(player.BottomLeft, player.width, player.height - 55, 89, 6f, -4f, 50, default, .65f); // player.Bottom if offset to the right for some reason, player.BottomLeft is centered
                }
            }
        }
    }

    public class ProudKnightSoul : ConsumableSoul
    {
        public override string Texture => "tsorcRevamp/Items/ConsumableSoul_ProudKnight";
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            // DisplayName.SetDefault("Soul of a Proud Knight");
            // Tooltip.SetDefault("Consume to gain 2000 souls");
        }
        public override void PostUpdate()
        {
            Lighting.AddLight(Item.Center, 0.25f, 0.60f, 0.15f);
        }
        public override void UseStyle(Player player, Rectangle rectangle)
        {
            // Each frame, make some dust
            if (Main.rand.NextBool())
            {
                Dust.NewDust(player.BottomLeft, player.width, player.height - 40, 89, 0f, -5f, 50, default, .8f);
            }

            if (Main.rand.NextFloat() < .5f)
            {
                Dust.NewDust(player.BottomLeft, player.width, player.height - 40, 89, 0f, -5f, 100, default, .75f);
            }

            if (player.itemTime == 1)
            {
                // This code runs once halfway through the useTime of the item. 
                Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath52 with { Volume = 0.75f, PitchVariance = 0.3f }, player.Center); // Plays sound.
                if (Main.myPlayer == player.whoAmI)
                {
                    player.QuickSpawnItem(player.GetSource_DropAsItem(), ModContent.ItemType<DarkSoul>(), 2000); // Gives player souls.
                    Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.Top, player.velocity, ModContent.ProjectileType<Projectiles.Soulsplosion>(), 250, 15, 0);
                }

                for (int d = 0; d < 90; d++) // Upwards
                {
                    Dust.NewDust(player.BottomLeft, player.width, player.height - 40, 89, 0f, -5f, 30, default, .8f); // player.Bottom if offset to the right for some reason, player.BottomLeft is centered
                }

                for (int d = 0; d < 30; d++) // Left
                {
                    Dust.NewDust(player.BottomLeft, player.width, player.height - 55, 89, -6f, -4f, 30, default, .7f); // player.Bottom if offset to the right for some reason, player.BottomLeft is centered
                }

                for (int d = 0; d < 30; d++) // Right
                {
                    Dust.NewDust(player.BottomLeft, player.width, player.height - 55, 89, 6f, -4f, 30, default, .7f); // player.Bottom if offset to the right for some reason, player.BottomLeft is centered
                }
            }
        }

    }

    public class BraveWarriorSoul : ModItem
    {
        public override string Texture => "tsorcRevamp/Items/ConsumableSoul_BraveWarrior";
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            // DisplayName.SetDefault("Soul of a Brave Warrior");
            // Tooltip.SetDefault("Consume to gain 5000 souls");
            ItemID.Sets.ItemIconPulse[Item.type] = true; // Makes item pulsate in world.
            ItemID.Sets.ItemNoGravity[Item.type] = true; // Makes item float in world.
        }

        public override void SetDefaults()
        {
            Item refItem = new Item();
            refItem.SetDefaults(ItemID.SoulofSight);
            Item.width = 14;
            Item.height = 22;
            Item.maxStack = Item.CommonMaxStack;
            Item.autoReuse = true;
            Item.useTurn = true;
            Item.value = 1;
            Item.consumable = true;
            Item.useAnimation = 60;
            Item.useTime = 60;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.rare = ItemRarityID.Green; // Mainly for colour consistency.
        }
        public override void PostUpdate()
        {
            Lighting.AddLight(Item.Center, 0.2f, 0.52f, 0.08f);
        }

        public override bool? UseItem(Player player) // Won't consume item without this
        {

            return true;
        }

        public override void UseStyle(Player player, Rectangle rectangle)
        {
            // Each frame, make some dust
            if (Main.rand.NextBool())
            {
                Dust.NewDust(player.BottomLeft, player.width, player.height - 40, 89, 0f, -5f, 50, default, .8f);
            }

            if (Main.rand.NextFloat() < .75f)
            {
                Dust.NewDust(player.BottomLeft, player.width, player.height - 40, 89, 0f, -5f, 100, default, .75f);
            }

            if (player.itemTime == 1)
            {
                // This code runs at the end of the usetime of the item
                Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath52 with { Volume = 0.9f, PitchVariance = 0.3f }, player.Center); // Plays sound.

                if (Main.myPlayer == player.whoAmI)
                {
                    player.QuickSpawnItem(player.GetSource_DropAsItem(), ModContent.ItemType<DarkSoul>(), 5000); // Gives player souls.
                    Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.Top, player.velocity, ModContent.ProjectileType<Projectiles.Soulsplosion>(), 600, 15, 0);
                }

                for (int d = 0; d < 100; d++) // Upwards
                {
                    Dust.NewDust(player.BottomLeft, player.width, player.height - 40, 89, 0f, -5f, 30, default, .8f); // player.Bottom if offset to the right for some reason, player.BottomLeft is centered
                }

                for (int d = 0; d < 40; d++) // Left
                {
                    Dust.NewDust(player.BottomLeft, player.width, player.height - 55, 89, -6f, -3f, 30, default, .7f); // player.Bottom if offset to the right for some reason, player.BottomLeft is centered
                }

                for (int d = 0; d < 40; d++) // Right
                {
                    Dust.NewDust(player.BottomLeft, player.width, player.height - 55, 89, 6f, -3f, 30, default, .7f); // player.Bottom if offset to the right for some reason, player.BottomLeft is centered
                }

                for (int d = 0; d < 40; d++) // Left
                {
                    Dust.NewDust(player.BottomLeft, player.width, player.height - 55, 89, -6f, -6f, 30, default, .7f); // player.Bottom if offset to the right for some reason, player.BottomLeft is centered
                }

                for (int d = 0; d < 40; d++) // Right
                {
                    Dust.NewDust(player.BottomLeft, player.width, player.height - 55, 89, 6f, -6f, 30, default, .7f); // player.Bottom if offset to the right for some reason, player.BottomLeft is centered
                }
            }
        }

    }

    public class HeroSoul : ModItem
    {
        public override string Texture => "tsorcRevamp/Items/ConsumableSoul_Hero";
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            // DisplayName.SetDefault("Soul of a Hero");
            // Tooltip.SetDefault("Consume to gain 10000 souls");
            ItemID.Sets.ItemIconPulse[Item.type] = true; // Makes item pulsate in world.
            ItemID.Sets.ItemNoGravity[Item.type] = true; // Makes item float in world.
        }

        public override void SetDefaults()
        {
            Item refItem = new Item();
            refItem.SetDefaults(ItemID.SoulofSight);
            Item.width = 14;
            Item.height = 22;
            Item.maxStack = Item.CommonMaxStack;
            Item.autoReuse = true;
            Item.useTurn = true;
            Item.value = 1;
            Item.consumable = true;
            Item.useAnimation = 120; // Needs to be 1 tick more than use time for it to work properly. Not sure why.
            Item.useTime = 120;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.rare = ItemRarityID.Green; // Mainly for colour consistency.
        }
        public override void PostUpdate()
        {
            Lighting.AddLight(Item.Center, 0.25f, 0.6f, 0.10f);
        }

        public override bool? UseItem(Player player) // Won't consume item without this
        {
            return true;
        }

        public override void UseStyle(Player player, Rectangle rectangle)
        {
            // Each frame, make some dust
            if (Main.rand.NextBool())
            {
                Dust.NewDust(player.BottomLeft, player.width, player.height - 40, 89, 0f, -5f, 50, default, .8f);
            }

            if (Main.rand.NextFloat() < 1f)
            {
                Dust.NewDust(player.BottomLeft, player.width, player.height - 40, 89, 0f, -5f, 50, default, .75f);
            }

            if (player.itemTime == 1)
            {
                // This code runs once halfway through the useTime of the item.
                Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath52 with { Volume = 1f, PitchVariance = 0.3f }, player.Center); // Plays sound.
                if (Main.myPlayer == player.whoAmI)
                {
                    player.QuickSpawnItem(player.GetSource_DropAsItem(), ModContent.ItemType<DarkSoul>(), 10000); // Gives player souls.
                    Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.Top, player.velocity, ModContent.ProjectileType<Projectiles.SoulsplosionLarge>(), 2000, 15, 0);
                }

                for (int d = 0; d < 100; d++) // Upwards
                {
                    Dust.NewDust(player.BottomLeft, player.width, player.height - 40, 89, 0f, -5f, 30, default, .8f); // player.Bottom if offset to the right for some reason, player.BottomLeft is centered
                }

                for (int d = 0; d < 70; d++) // Left
                {
                    Dust.NewDust(player.BottomLeft, player.width, player.height - 55, 89, -6f, -3f, 30, default, .7f); // player.Bottom if offset to the right for some reason, player.BottomLeft is centered
                }

                for (int d = 0; d < 70; d++) // Right
                {
                    Dust.NewDust(player.BottomLeft, player.width, player.height - 55, 89, 6f, -3f, 30, default, .7f); // player.Bottom if offset to the right for some reason, player.BottomLeft is centered
                }

                for (int d = 0; d < 1000; d++) // Left
                {
                    Dust.NewDust(player.BottomLeft, player.width, player.height - 55, 89, Main.rand.NextFloat(-3.5f, 3.5f), Main.rand.NextFloat(-3f, -10f), 30, default, Main.rand.NextFloat(.5f, .8f));
                }
            }
        }

    }
}