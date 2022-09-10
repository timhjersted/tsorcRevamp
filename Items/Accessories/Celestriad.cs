using Terraria;
using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories
{
    public class Celestriad : ModItem
    {

        public static int manaCost = 95;
        public static int regenDelay = 150;
        public static float damageResistance = 0.50f;


        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("[c/ffbf00:Focuses the user's mana into a protective shield when below 200 HP]" +
                                $"\nMana Shield reduces incoming damage by {damageResistance * 100}%, but drains {manaCost} mana per hit" +
                                "\nInhibits both natural and artificial mana regen for a period of time" +
                                "\n[c/00ffd4:Doubles max mana and decreases stamina usage by 15%]");
                                
        }

        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 26;
            Item.accessory = true;
            Item.value = PriceByRarity.Purple_11;
            Item.rare = ItemRarityID.Purple;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<Melee.ManaShield>(), 1);
            recipe.AddIngredient(ModContent.ItemType<Magic.EssenceOfMana>(), 1);
            recipe.AddIngredient(ModContent.ItemType<CursedSoul>(), 20);
            recipe.AddIngredient(ModContent.ItemType<SoulOfArtorias>(), 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 200000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult += 0.15f;
            player.statManaMax2 = (int)(player.statManaMax2 * 1.5f);

            if (player.statLife <= 200)
            {
                

                if (player.statLife <= 100)
                {
                    
                    int dust = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height, DustID.AncientLight, player.velocity.X, player.velocity.Y, 150, Color.White, 0.5f);
                    Main.dust[dust].noGravity = true;
                }
            
                //Iterate through the five main accessory slots
                for (int i = 3; i < (8 + player.extraAccessorySlots); i++)
                {
                    //If they're wearing the accesories that totally break this concept, it won't function for them.
                    if (player.armor[i].type == ItemID.MagicCuffs || player.armor[i].type == ItemID.CelestialCuffs || player.armor[i].type == ItemID.ManaRegenerationBand)
                    {
                        player.GetModPlayer<tsorcRevampPlayer>().manaShield = 0;
                        return;
                    }
                }

                base.UpdateEquip(player);
                player.GetModPlayer<tsorcRevampPlayer>().manaShield = 2;
                //player.GetDamage(DamageClass.Ranged) *= 0.01f;
                //player.GetDamage(DamageClass.Magic) *= 0.01f;
                //player.GetDamage(DamageClass.Summon) *= 0.01f;
                if (player.statMana >= manaCost)
                {
                    player.endurance += damageResistance;
                }
            }
        }
    }
}
