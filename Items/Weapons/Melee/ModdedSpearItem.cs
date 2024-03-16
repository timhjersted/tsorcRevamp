using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee
{
    public abstract class ModdedSpearItem : ModItem
    {
        public abstract int ProjectileID { get; }
        public abstract int Width { get; }
        public abstract int Height { get; }
        public abstract int BaseDmg { get; }
        public abstract int BaseCritChance { get; }
        public abstract float BaseKnockback { get; }
        public abstract int UseAnimationTime { get; }
        public abstract int UseTime { get; }
        public abstract int Rarity { get; }
        public abstract int Value { get; }
        public abstract SoundStyle UseSoundID { get; }
        public override void SetStaticDefaults()
        {
            ItemID.Sets.SkipsInitialUseSound[Item.type] = true; // This skips use animation-tied sound playback, so that we're able to make it be tied to use time instead in the UseItem() hook.
            ItemID.Sets.Spears[Item.type] = true; // This allows the game to recognize our new item as a spear.
        }

        public override void SetDefaults()
        {
            Item.width = Width;
            Item.height = Height;

            Item.damage = BaseDmg;
            Item.crit = BaseCritChance;
            Item.knockBack = BaseKnockback;

            Item.useAnimation = UseAnimationTime; // The length of the item's use animation in ticks (60 ticks == 1 second.) //12
            Item.useTime = UseTime; // The length of the item's use time in ticks (60 ticks == 1 second.) //18

            Item.rare = Rarity; // Assign this item a rarity level of Pink
            Item.value = Value; // The number and type of coins item can be sold for to an NPC

            // Use Properties
            Item.useStyle = ItemUseStyleID.Shoot; // How you use the item (swinging, holding out, etc.)
            Item.UseSound = UseSoundID; // The sound that this item plays when used. //SoundID.Item71
            Item.autoReuse = true; // Allows the player to hold click to automatically use the item again. Most spears don't autoReuse, but it's possible when used in conjunction with CanUseItem()

            // Weapon Properties
            Item.noUseGraphic = true; // When true, the item's sprite will not be visible while the item is in use. This is true because the spear projectile is what's shown so we do not want to show the spear sprite as well.
            Item.DamageType = DamageClass.Melee;
            Item.noMelee = true; // Allows the item's animation to do damage. This is important because the spear is actually a projectile instead of an item. This prevents the melee hitbox of this item.

            // Projectile Properties
            Item.shootSpeed = 3.7f; // The speed of the projectile measured in pixels per frame.
            Item.shoot = ProjectileID; // The projectile that is fired from this weapon
        }

        public override bool CanUseItem(Player player)
        {
            // Ensures no more than one spear can be thrown out, use this when using autoReuse
            return player.ownedProjectileCounts[Item.shoot] < 1;
        }

        public override bool? UseItem(Player player)
        {
            // Because we're skipping sound playback on use animation start, we have to play it ourselves whenever the item is actually used.
            if (!Main.dedServ && Item.UseSound.HasValue)
            {
                SoundEngine.PlaySound(Item.UseSound.Value, player.Center);
            }

            return null;
        }
    }
}
