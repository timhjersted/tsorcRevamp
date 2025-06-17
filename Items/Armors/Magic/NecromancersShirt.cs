using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Utilities;
using tsorcRevamp.Projectiles.Summon.NecromanticSerpent;
using Microsoft.Xna.Framework;

namespace tsorcRevamp.Items.Armors.Magic
{
    [AutoloadEquip(EquipType.Body)]
    public class NecromancersShirt : ModItem
    {
        public const float ManaCost = 20f;
        public const int MaxMana = 220;
        public const int SoulCost = 70000;
        public const int SkullBaseDmg = 250;
        public const float SkullBaseKnockback = 5f;
        public int serpentPos = -1;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MaxMana, ManaCost);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 25;
            Item.rare = ModContent.RarityType<DarkBlue>();
            Item.value = PriceByRarity.Purple_11;
        }
        public override void UpdateEquip(Player player)
        {
            player.statManaMax2 += MaxMana;
            player.manaCost -= ManaCost / 100f;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<NecromancersSkullmask>() && legs.type == ModContent.ItemType<NecromancersPants>();
        }
        public override void UpdateArmorSet(Player player)
        {
            UsefulFunctions.ModPlayer(player).NecromanticSerpent = true;
        
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            if (player.ownedProjectileCounts[ModContent.ProjectileType<NecromanticSerpentHead>()] != 0) return;

            serpentPos = Projectile.NewProjectile(player.GetSource_FromThis(), player.position, Vector2.Zero, ModContent.ProjectileType<NecromanticSerpentHead>(),
                (int)NecromanticSerpentHead.OriginalDamage, NecromanticSerpentHead.OriginalKnockback, player.whoAmI);

            player.AddBuff(NecromanticSerpentHead.BuffType, 2);
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SpectreRobe);
            recipe.AddIngredient(ModContent.ItemType<LichBone>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), SoulCost);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
