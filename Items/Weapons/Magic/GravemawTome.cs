using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Magic
{
    ///<summary>
    ///The Vessel of Souls' spell tome: four of its moves in one book, all on LEFT CLICK (cursor
    ///high/low × tap/hold), routed through GravemawController — the same scheme as Heart of Winter.
    ///  • Cursor high tap:  Soulspit    — a fan of homing soul-skulls
    ///  • Cursor high hold: Reliquary Nova — charge, then release an expanding soul ring
    ///  • Cursor low tap:   Gravemaw Orb — lob an orb that opens a brief black hole, then bursts
    ///  • Cursor low hold:  Hungering Maw — channel a short INHALE cone that pulls foes in, damages
    ///                                       them rapidly, and trickles a little life back (soul eating)
    ///Placeholder art: vanilla Water Bolt book. Crafted from 1 Soul of the Vessel + 4000 Dark Souls.
    ///</summary>
    class GravemawTome : ModItem
    {
        public override string Texture => "Terraria/Images/Item_" + ItemID.WaterBolt;

        public override void SetDefaults()
        {
            Item.damage = 18;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 8;
            Item.width = 28;
            Item.height = 30;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.channel = true; // the controller watches player.channel to split tap vs hold
            Item.knockBack = 3f;
            Item.value = Item.sellPrice(gold: 2);
            Item.rare = ItemRarityID.Green;
            Item.UseSound = SoundID.Item20;
            Item.autoReuse = false;
            Item.shoot = ModContent.ProjectileType<Projectiles.Magic.Gravemaw.GravemawController>();
            Item.shootSpeed = 1f;
        }

        public override bool CanUseItem(Player player) =>
            player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Magic.Gravemaw.GravemawController>()] == 0;

        public override bool AltFunctionUse(Player player) => FourAttackWeaponControls.UsesAltFunction;

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int mode = FourAttackWeaponControls.GetAttackMode(player);
            Projectile.NewProjectile(source, player.Center, Vector2.Zero, type, damage, knockback,
                player.whoAmI, mode, player.altFunctionUse == 2 ? 1f : 0f);
            return false;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<SoulOfTheVessel>(), 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 4000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
