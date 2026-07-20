using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Accessories.Melee
{
    public abstract class MeleeShield : ModItem
    {
        //all the melee shields have a lot in common, so i use an abstract class from which they inherit values
        //i dont feel like writing the same thing 4 times. does it make the code less readable? yeah. i dont give a shit
        public virtual int Defense
        {
            get;
        }
        public virtual float DamageReduction
        {
            get;
        }
        public static int NonMeleeBadDmgMult = 100;
        public static float BadMoveSpeedMult = 20f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(DamageReduction, NonMeleeBadDmgMult, BadMoveSpeedMult);
        public override void SetStaticDefaults()
        {
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
            // Utility immunities stay passive in both modes (and in either slot).
            player.buffImmune[BuffID.Burning] = true;
            player.noKnockback = true;
            player.fireWalk = true;
            // Under Active Shields Revamp the % DR, the move-speed penalty, AND the melee-only damage penalties
            // are all dropped (active blocking replaces them, and dropping the penalty lets other classes use it).
            if (!tsorcRevampActiveShieldPlayer.ActiveFor(player))
            {
                player.moveSpeed *= 1f - BadMoveSpeedMult / 100f;
                player.endurance += DamageReduction / 100f;
                player.GetDamage(DamageClass.Magic) -= NonMeleeBadDmgMult / 100;
                player.GetDamage(DamageClass.Ranged) -= NonMeleeBadDmgMult / 100;
                player.GetDamage(DamageClass.Summon) -= NonMeleeBadDmgMult / 100;
                player.GetDamage(DamageClass.Throwing) -= NonMeleeBadDmgMult / 100;
            }
        }
        public override bool CanEquipAccessory(Player player, int slot, bool modded)
        {
            foreach (Item i in player.armor)
            {
                if (i.ModItem is MeleeShield)
                {
                    return false;
                }
            }

            return base.CanEquipAccessory(player, slot, modded);
        }
    }

    [AutoloadEquip(EquipType.Shield)]
    public class GazingShield : MeleeShield
    {
        public override float DamageReduction => 6f;
        public override int Defense => 6;
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.defense = Defense;
            Item.value = PriceByRarity.LightRed_4;
            Item.rare = ItemRarityID.LightRed;
        }


        public override void UpdateEquip(Player player)
        {
            base.UpdateEquip(player);
            // "Watchful" — the gazing eye reveals nearby enemies (Hunter effect). Passive whenever equipped,
            // in either mode and either slot — not tied to raising the shield.
            player.detectCreature = true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.CobaltBar, 10);
            //recipe.AddIngredient(ItemID.SoulofLight, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 15000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();

        }

    }

    [AutoloadEquip(EquipType.Shield)]
    public class BeholderShield : MeleeShield
    {
        public override float DamageReduction => 8f;
        public override int Defense => 15;
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.defense = Defense;
            Item.value = PriceByRarity.Pink_5;
            Item.rare = ItemRarityID.Pink;
        }

        public override void UpdateEquip(Player player)
        {
            base.UpdateEquip(player);
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<GazingShield>(), 1);
            recipe.AddIngredient(ItemID.SoulofMight, 10);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 30000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();

        }
    }

    [AutoloadEquip(EquipType.Shield)]
    public class BeholderShield2 : MeleeShield
    {
        public override float DamageReduction => 10f;
        public override int Defense => 24;
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Beholder Shield II");
            base.SetStaticDefaults();
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.defense = Defense;
            Item.value = PriceByRarity.Cyan_9;
            Item.rare = ItemRarityID.Purple;
        }

        public override void UpdateEquip(Player player)
        {
            base.UpdateEquip(player);
            player.buffImmune[BuffID.OnFire] = true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<BeholderShield>(), 1);
            recipe.AddIngredient(ModContent.ItemType<WhiteTitanite>(), 8);
            recipe.AddIngredient(ModContent.ItemType<SoulOfAttraidies>(), 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 65000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();

        }
    }

    [AutoloadEquip(EquipType.Shield)]
    public class EnchantedBeholderShield2 : MeleeShield
    {
        public override float DamageReduction => 12f;
        public override int Defense => 33;
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.defense = Defense;
            Item.value = PriceByRarity.Red_10;
            Item.rare = ModContent.RarityType<OrangeRed>();
        }

        public override void UpdateEquip(Player player)
        {
            base.UpdateEquip(player);
            player.buffImmune[BuffID.OnFire] = true;
            player.buffImmune[BuffID.CursedInferno] = true;
            player.buffImmune[BuffID.Ichor] = true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<BeholderShield2>(), 1);
            recipe.AddIngredient(ModContent.ItemType<GuardianSoul>(), 2);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 120000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();

        }
    }
}
