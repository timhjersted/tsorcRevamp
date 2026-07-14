using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Magic;

namespace tsorcRevamp.Items.Weapons.Magic
{
    ///<summary>
    ///The Ice Gigas's spell tome: four of its moves in one book.
    ///  • Cursor high tap:   Icicle Crown — three icicles form overhead and lance at the cursor
    ///  • Cursor high hold:  Frost Breath — a channeled cone of frost
    ///  • Cursor low tap:    Rimefang — ice spikes erupt from the ground at the cursor
    ///  • Cursor low hold:   Absolute Zero — charge, then release an expanding freeze ring
    ///All four are routed through HeartOfWinterController, which watches whether the button is
    ///tapped (released within 18 ticks) or held. Placeholder art: vanilla Water Bolt book —
    ///swap the Texture override for a bespoke sprite when one exists.
    ///</summary>
    class HeartOfWinter : ModItem
    {
        public override string Texture => "Terraria/Images/Item_" + ItemID.WaterBolt;

        public override void SetDefaults()
        {
            Item.damage = 55;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 12;
            Item.width = 28;
            Item.height = 30;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.channel = true; //the controller watches player.channel to split tap vs hold
            Item.knockBack = 4f;
            Item.value = Item.sellPrice(gold: 8);
            Item.rare = ItemRarityID.Pink;
            Item.UseSound = SoundID.Item28;
            Item.autoReuse = false;
            Item.shoot = ModContent.ProjectileType<HeartOfWinterController>();
            Item.shootSpeed = 1f;
        }

        public override bool CanUseItem(Player player)
        {
            //One controller at a time
            return player.ownedProjectileCounts[ModContent.ProjectileType<HeartOfWinterController>()] == 0;
        }

        public override bool AltFunctionUse(Player player) => FourAttackWeaponControls.UsesAltFunction;

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int mode = FourAttackWeaponControls.GetAttackMode(player);
            Projectile.NewProjectile(source, player.Center, Vector2.Zero, type, damage, knockback,
                player.whoAmI, mode, player.altFunctionUse == 2 ? 1f : 0f);
            return false;
        }
    }
}
