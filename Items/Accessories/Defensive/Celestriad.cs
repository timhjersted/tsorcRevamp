using Terraria;
using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Accessories.Magic;

namespace tsorcRevamp.Items.Accessories.Defensive
{
    public class Celestriad : ModItem
    {

        public static int manaCost = 90;
        public static int regenDelay = 800;
        public static float damageResistance = 0.35f;


        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("[c/ffbf00:Focuses the user's mana into a protective shield]" +
                                $"\nMana Shield reduces incoming damage by {damageResistance * 100}%, but drains {manaCost} mana per hit" +
                                "\nInhibits both natural and artificial mana regen for a period of time" +
                                "\n[c/00ffd4:Doubles max mana and decreases stamina usage by 15%]" +
                                "\nReduces ranged, magic and summon damage by 25% multiplicatively");
                                
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
            recipe.AddIngredient(ModContent.ItemType<ManaShield>(), 1);
            recipe.AddIngredient(ModContent.ItemType<EssenceOfMana>(), 1);
            recipe.AddIngredient(ModContent.ItemType<CursedSoul>(), 30);
            recipe.AddIngredient(ModContent.ItemType<SoulOfArtorias>(), 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 200000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult += 0.15f;
            player.statManaMax2 = (int)(player.statManaMax2 * 2f);
            player.GetDamage(DamageClass.Ranged) *= 0.75f;
            player.GetDamage(DamageClass.Magic) *= 0.75f;
            player.GetDamage(DamageClass.Summon) *= 0.75f;

            //Iterate through the five main accessory slots
            for (int i = 3; i < (8 + player.extraAccessorySlots); i++)
            {
                //If they're wearing the accesories that totally break this concept, it won't function for them.
                if (player.armor[i].type == ItemID.MagicCuffs || player.armor[i].type == ItemID.CelestialCuffs || player.armor[i].type == ItemID.ManaRegenerationBand || player.armor[i].type == ModContent.ItemType<CelestialCloak>())
                {
                    player.GetModPlayer<tsorcRevampPlayer>().manaShield = 0;
                    return;
                }
            }
            player.GetModPlayer<tsorcRevampPlayer>().manaShield = 2;
            if (player.statMana >= manaCost)
            {
                player.endurance += damageResistance; 
                int dust = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height, DustID.AncientLight, player.velocity.X, player.velocity.Y, 150, Color.White, 0.5f);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}
