using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Melee
{
    public abstract class MeleeShield : ModItem
    {
        //all the melee shields have a lot in common, so i use an abstract class from which they inherit values
        //i dont feel like writing the same thing 4 times. does it make the code less readable? yeah. i dont give a shit
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("For melee warriors only" +
                                "\nGrants immunity to knockback and fire blocks" +
                                "\nReduces movement speed by 20%");
        }
        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.accessory = true;
            Item.rare = ItemRarityID.Orange;
        }
        public override void UpdateEquip(Player player)
        {
            player.buffImmune[BuffID.Burning] = true;
            player.moveSpeed *= 0.8f;
            player.noKnockback = true;
            player.fireWalk = true;
            player.manaCost += 0.7f;
        }
        public override bool CanEquipAccessory(Player player, int slot, bool modded)
        {
            foreach (Item i in player.armor)
            {
                if (i.ModItem is MeleeShield) {
                    return false;
                }
            }

            return base.CanEquipAccessory(player, slot, modded);
        }
    }

    public class GazingShield : MeleeShield
    {

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            //only insert the tooltip if the last valid line is not the name, the "Equipped in social slot" line, or the "No stats will be gained" line (aka do not insert if in a vanity slot)
            int ttindex = tooltips.FindLastIndex(t => t.Name != "ItemName" && t.Name != "Social" && t.Name != "SocialDesc" && !t.Name.Contains("Prefix"));
            if (ttindex != -1)
            {// if we find one
             //insert the extra tooltip line
                tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "",
                "Plus 6% damage reduction" +
                "\nReduces Ranged, Magic and Summoner Damage by 85%. +70% mana cost"));
            }
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.defense = 6;
            Item.value = PriceByRarity.LightRed_4;
            Item.rare = ItemRarityID.LightRed;
        }


        public override void UpdateEquip(Player player)
        {
            base.UpdateEquip(player);
            player.endurance += 0.06f;
            player.GetDamage(DamageClass.Melee) += 0f;
            player.GetDamage(DamageClass.Magic) -= 0.85f;
            player.GetDamage(DamageClass.Ranged) -= 0.85f;
            player.GetDamage(DamageClass.Summon) -= 0.85f;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.MythrilBar, 15);
            recipe.AddIngredient(ItemID.SoulofLight, 5);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 15000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();

        }

    }

    public class BeholderShield : MeleeShield
    {
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            //only insert the tooltip if the last valid line is not the name, the "Equipped in social slot" line, or the "No stats will be gained" line (aka do not insert if in a vanity slot)
            int ttindex = tooltips.FindLastIndex(t => t.Name != "ItemName" && t.Name != "Social" && t.Name != "SocialDesc" && !t.Name.Contains("Prefix"));
            if (ttindex != -1)
            {// if we find one
             //insert the extra tooltip line
                tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "",
                "Plus 8% damage reduction" +
                "\nReduces Ranged, Magic and Summoner Damage by 150%. +70% mana cost"));
            }
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.defense = 15;
            Item.value = PriceByRarity.Pink_5;
            Item.rare = ItemRarityID.Pink;
        }

        public override void UpdateEquip(Player player)
        {
            base.UpdateEquip(player);
            player.endurance += 0.08f;
            player.GetDamage(DamageClass.Melee) += 0f;
            player.GetDamage(DamageClass.Magic) -= 1.5f;
            player.GetDamage(DamageClass.Ranged) -= 1.5f;
            player.GetDamage(DamageClass.Summon) -= 1.5f;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(Mod.Find<ModItem>("GazingShield").Type, 1);
            recipe.AddIngredient(ItemID.SoulofMight, 10);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 30000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();

        }
    }

    public class BeholderShield2 : MeleeShield
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Beholder Shield II");
            base.SetStaticDefaults();
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            //only insert the tooltip if the last valid line is not the name, the "Equipped in social slot" line, or the "No stats will be gained" line (aka do not insert if in a vanity slot)
            int ttindex = tooltips.FindLastIndex(t => t.Name != "ItemName" && t.Name != "Social" && t.Name != "SocialDesc" && !t.Name.Contains("Prefix"));
            if (ttindex != -1)
            {// if we find one
             //insert the extra tooltip line
                tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "",
                "Plus immunity to On Fire and 10% damage reduction" +
                "\nReduces Ranged, Magic and Summoner Damage by 150%. +70% mana cost"));
            }
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.defense = 24;
            Item.value = PriceByRarity.Cyan_9;
            Item.rare = ItemRarityID.Cyan;
        }

        public override void UpdateEquip(Player player)
        {
            base.UpdateEquip(player);
            player.endurance += 0.1f;
            player.GetDamage(DamageClass.Melee) += 0f;
            player.GetDamage(DamageClass.Magic) -= 1.5f;
            player.GetDamage(DamageClass.Ranged) -= 1.5f;
            player.GetDamage(DamageClass.Summon) -= 1.5f;
            player.buffImmune[BuffID.OnFire] = true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(Mod.Find<ModItem>("BeholderShield").Type, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("GuardianSoul").Type, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("SoulOfAttraidies").Type, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 60000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();

        }
    }

    public class EnchantedBeholderShield2 : MeleeShield
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Enchanted Beholder Shield II");
            Tooltip.SetDefault("A legendary shield for melee warriors only" +
                "\nGrants immunity to knockback and nearly all debuffs, plus 12% damage reduction" +
                "\nReduces Ranged, Magic and Summoner Damage by 300%. +70% mana cost" +
                "\nReduces movement speed by 20%");
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.defense = 33;
            Item.value = PriceByRarity.Red_10;
            Item.rare = ItemRarityID.Red;
        }

        public override void UpdateEquip(Player player)
        {
            base.UpdateEquip(player);
            player.endurance += 0.12f;
            player.GetDamage(DamageClass.Melee) += 0f;
            player.GetDamage(DamageClass.Magic) -= 3f;
            player.GetDamage(DamageClass.Ranged) -= 3f;
            player.GetDamage(DamageClass.Summon) -= 3f;
            player.buffImmune[BuffID.Poisoned] = true;
            player.buffImmune[BuffID.Darkness] = true;
            player.buffImmune[BuffID.Cursed] = true;
            player.buffImmune[BuffID.OnFire] = true;
            player.buffImmune[BuffID.Bleeding] = true;
            player.buffImmune[BuffID.Confused] = true;
            player.buffImmune[BuffID.Slow] = true;
            player.buffImmune[BuffID.Weak] = true;
            player.buffImmune[BuffID.Silenced] = true;
            player.buffImmune[BuffID.BrokenArmor] = true;
            player.buffImmune[BuffID.CursedInferno] = true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(Mod.Find<ModItem>("BeholderShield2").Type, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("GuardianSoul").Type, 2);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 120000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();

        }
    }
}
