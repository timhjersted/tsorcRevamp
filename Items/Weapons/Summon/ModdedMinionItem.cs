using Terraria.GameContent.Creative;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Weapons.Summon;
using tsorcRevamp.Projectiles.Melee.Spears;
using tsorcRevamp.Projectiles.Summon.NullSprite;
using Microsoft.Xna.Framework;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Summon.ForgottenImp;

namespace tsorcRevamp.Items.Weapons.Summon
{
    abstract class ModdedMinionItem : ModItem
    {
        public abstract int Width { get; }
        public abstract int Height { get; }
        public abstract int MinionProjectileType { get; }
        public abstract int MinionBuffType { get; }
        public abstract int BaseDmg { get; }
        public abstract int Crit {  get; }
        public abstract float SlotsRequired { get; }
        public abstract float SummonTagDmgMulti { get; }
        public abstract float Knockback {  get; }
        public abstract int UseTimeAnimation { get; }
        public abstract int Mana {  get; }
        public abstract int Rarity { get; }
        public override void SetStaticDefaults()
        {
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
            ItemID.Sets.GamepadWholeScreenUseRange[Item.type] = true; // This lets the player target anywhere on the whole screen while using a controller
            ItemID.Sets.LockOnIgnoresCollision[Item.type] = true;
            ItemID.Sets.StaffMinionSlotsRequired[Item.type] = SlotsRequired;
        }
        public override void SetDefaults()
        {
            Item.damage = BaseDmg;
            Item.crit = Crit;
            Item.knockBack = Knockback;
            Item.width = Width;
            Item.height = Height;
            Item.useTime = Item.useAnimation = UseTimeAnimation;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.rare = Rarity;
            Item.value = PriceByRarity.fromItem(Item);
            Item.mana = 10;
            Item.UseSound = SoundID.Item44;
            Item.DamageType = DamageClass.Summon;
            Item.shoot = MinionProjectileType;
            Item.buffType = MinionBuffType;
        }
        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            // Here you can change where the minion is spawned. Most vanilla minions spawn at the cursor position
            position = Main.MouseWorld;
        }
        public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 speed, int type, int damage, float knockBack)
        {
            player.AddBuff(Item.buffType, 2);
            if (Main.myPlayer == player.whoAmI)
            {
                Projectile minion = Projectile.NewProjectileDirect(source, position, speed, type, damage, knockBack);
                minion.originalDamage = Item.damage;
            }
            return false;
        }
    }
}
