using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Accessories;

namespace tsorcRevamp.Items.Armors;

[AutoloadEquip(EquipType.Body)]
public class DragoonArmor2 : ModItem
{
    public override string Texture => "tsorcRevamp/Items/Armors/DragoonArmor";
    public override void SetStaticDefaults()
    {
        DisplayName.SetDefault("Supreme Dragoon Armor");
        Tooltip.SetDefault("A reforged upgrade to the legendary Dragoon Armor." +
            "\nYou are a master of all forces, the protector of Earth, the Hero of the age." +
            "\nThe powers of the Dragoon Cloak are embedded within its blue-plated chest piece." +
            "\nDragoon Cloak effects kick in at 40% life.");
    }

    public override void SetDefaults()
    {
        Item.width = 18;
        Item.height = 18;
        Item.defense = 50;
        Item.rare = ItemRarityID.Expert;
        Item.value = PriceByRarity.fromItem(Item);
    }

    public override void UpdateEquip(Player player)
    {
        player.starCloakItem = new Item(ItemID.StarCloak);
        player.GetCritChance(DamageClass.Generic) += 3;
        player.GetDamage(DamageClass.Generic) += 0.05f;

        if (player.statLife <= (player.statLifeMax2 / 5 * 2))
        {
            player.lifeRegen += 6;
            player.statDefense += 10;
            player.manaRegenBonus += 5;
            player.GetCritChance(DamageClass.Generic) += 3;
            player.GetDamage(DamageClass.Generic) += 0.05f;
            int dust = Dust.NewDust(new Vector2(player.position.X, player.position.Y), player.width, player.height, 21, (player.velocity.X) + (player.direction * 1), player.velocity.Y, 150, Color.White, 0.5f);
            Main.dust[dust].noGravity = true;
        }
    }
    public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        RasterizerState OverflowHiddenRasterizerState = new RasterizerState
        {
            CullMode = CullMode.None,
            ScissorTestEnable = true
        };

        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.None, OverflowHiddenRasterizerState, null, Main.UIScaleMatrix);
        Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Item[Item.type];
        for (int i = 0; i < 4; i++)
        {
            Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
            spriteBatch.Draw(texture, position + offsetPositon, null, Color.White, 0, origin, scale, SpriteEffects.None, 0);
        }
        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, OverflowHiddenRasterizerState, null, Main.UIScaleMatrix);

        return true;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ModContent.ItemType<DragoonArmor>());
        recipe.AddIngredient(ModContent.ItemType<DragoonCloak>());
        //recipe.AddIngredient(ModContent.ItemType<DragonEssence>(), 1);
        recipe.AddIngredient(ModContent.ItemType<BequeathedSoul>(), 1);
        recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 90000);
        recipe.AddTile(TileID.DemonAltar);

        recipe.Register();
    }
}
