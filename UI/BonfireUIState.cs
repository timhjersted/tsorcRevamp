using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace tsorcRevamp.UI;

class BonfireUIState : UIState
{
    public tsorcDragableUIPanel BonfireUI;
    //public tsorcUIHoverTextButton ButtonSetSpawn;
    //public tsorcUIHoverTextButton ButtonPiggyBank;
    //public tsorcUIHoverTextButton ButtonSafe;
    //public tsorcUIHoverTextButton ButtonClose;

    public static bool Visible = false;

    public override void OnInitialize()
    {
        // Here we define our container UIElement. In DragableUIPanel.cs, you can see that DragableUIPanel is a UIPanel with a couple added features.
        BonfireUI = new tsorcDragableUIPanel();
        BonfireUI.SetPadding(0);
        // We need to place this UIElement in relation to its Parent. Later we will be calling `base.Append(BonfireUI);`. 
        // This means that this class, BonfireUIState, will be our Parent. Since BonfireUIState is a UIState, the Left and Top are relative to the top left of the screen.
        BonfireUI.Left.Set((Main.screenWidth - 160) / 2, 0f);
        BonfireUI.Top.Set((Main.screenHeight + 120) / 2, 0f);
        BonfireUI.Width.Set(160f, 0f);
        BonfireUI.Height.Set(64f, 0f);
        BonfireUI.BackgroundColor = new Color(35, 20, 20);

        // Next, we create another UIElement that we will place. Since we will be calling `BonfireUI.Append(ButtonSetSpawn);`, Left and Top are relative to the top left of the BonfireUI UIElement. 
        // By properly nesting UIElements, we can position things relatively to each other easily.
        Asset<Texture2D> buttonSetSpawnTexture = ModContent.Request<Texture2D>("tsorcRevamp/UI/ButtonSetSpawn");
        tsorcUIHoverTextButton ButtonSetSpawn = new tsorcUIHoverTextButton(buttonSetSpawnTexture, "Set Spawn");
        ButtonSetSpawn.Left.Set(10, 0f);
        ButtonSetSpawn.Top.Set(10, 0f);
        ButtonSetSpawn.Width.Set(44, 0f);
        ButtonSetSpawn.Height.Set(44, 0f);
        // UIHoverImageButton doesn't do anything when Clicked. Here we assign a method that we'd like to be called when the button is clicked.
        ButtonSetSpawn.OnClick += new MouseEvent(ButtonSetSpawnClicked);
        BonfireUI.Append(ButtonSetSpawn);

        Asset<Texture2D> buttonPiggyBankTexture = ModContent.Request<Texture2D>("tsorcRevamp/UI/ButtonPiggyBank");
        tsorcUIHoverTextButton ButtonPiggyBank = new tsorcUIHoverTextButton(buttonPiggyBankTexture, "Access Piggy Bank");
        ButtonPiggyBank.Left.Set(58, 0f);
        ButtonPiggyBank.Top.Set(10, 0f);
        ButtonPiggyBank.Width.Set(44, 0f);
        ButtonPiggyBank.Height.Set(44, 0f);
        ButtonPiggyBank.OnClick += new MouseEvent(ButtonPiggyBankClicked);
        BonfireUI.Append(ButtonPiggyBank);

        Asset<Texture2D> buttonSafeTexture = ModContent.Request<Texture2D>("tsorcRevamp/UI/ButtonSafe");
        tsorcUIHoverTextButton ButtonSafe = new tsorcUIHoverTextButton(buttonSafeTexture, "Access Safe");
        ButtonSafe.Left.Set(106, 0f);
        ButtonSafe.Top.Set(10, 0f);
        ButtonSafe.Width.Set(44, 0f);
        ButtonSafe.Height.Set(44, 0f);
        ButtonSafe.OnClick += new MouseEvent(ButtonSafeClicked);
        BonfireUI.Append(ButtonSafe);

        Append(BonfireUI);

        // As a recap, ExampleUI is a UIState, meaning it covers the whole screen. We attach BonfireUI to ExampleUI some distance from the top left corner.
        // We then place ButtonSetSpawn, closeButton, and moneyDiplay onto BonfireUI so we can easily place these UIElements relative to BonfireUI.
        // Since BonfireUI will move, this proper organization will move ButtonSetSpawn, closeButton, and moneyDiplay properly when BonfireUI moves.
    }

    private void ButtonSetSpawnClicked(UIMouseEvent evt, UIElement listeningElement)
    {
        Player player = Main.LocalPlayer;
        Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick, player.Center);
        int spawnX = (int)((player.position.X + player.width / 2.0) / 16.0);
        int spawnY = (int)((player.position.Y + player.height) / 16.0);

        //Main.NewText("spawnX is " + spawnX + ", spawnY is " + spawnY);
        //Main.NewText("Player.SpawnX is " + player.SpawnX + ", Player.SpawnY is " + player.SpawnY);

        if (player.SpawnX != spawnX && player.SpawnY != spawnY)
        {
            player.ChangeSpawn(spawnX, spawnY);
            player.FindSpawn();
            if (Main.netMode != NetmodeID.Server)
            {
                Main.NewText("Spawn point set!", 255, 240, 20);
            }
        }
        else
        {
            player.RemoveSpawn();
            if (Main.netMode != NetmodeID.Server)
            {
                Main.NewText("Spawn point removed!", 150, 140, 0);
            }
        }
    }

    private void ButtonPiggyBankClicked(UIMouseEvent evt, UIElement listeningElement)
    {
        Player player = Main.LocalPlayer;
        //Tile tile = Main.tile
        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item59);

        bool anyBanks = false;
        foreach (Projectile projectile in Main.projectile)
        {
            if (projectile.active && projectile.type == ModContent.ProjectileType<Projectiles.Pets.PiggyBankProjectile>() && projectile.owner == player.whoAmI)
            {
                //kill any active when the button is pressed again
                anyBanks = true;
                projectile.active = false;
                player.chest = -1;
                break;
            }
            /*if (projectile.active && projectile.type == ModContent.ProjectileType<Projectiles.Pets.SafeProjectile>() && projectile.owner == player.whoAmI) {
                //kill safes when spawning a piggy bank
                projectile.active = false;
                player.chest = -1;
            }*/

        }
        if (!anyBanks)
        { //only spawn a safe if there is no existing safe
            //Main.playerInventory = true; //force open inventory
            Projectile.NewProjectile(new EntitySource_Misc("Bonfire"), new Vector2(player.position.X - 48, player.position.Y), Vector2.Zero, ModContent.ProjectileType<Projectiles.Pets.PiggyBankProjectile>(), 0, 0, player.whoAmI);
            Recipe.FindRecipes();
        }
    }
    private void ButtonSafeClicked(UIMouseEvent evt, UIElement listeningElement)
    {
        bool anySafes = false;
        Player player = Main.player[Main.myPlayer];
        Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuOpen, player.Center);

        foreach (Projectile projectile in Main.projectile)
        {
            if (projectile.active && projectile.type == ModContent.ProjectileType<Projectiles.Pets.SafeProjectile>() && projectile.owner == player.whoAmI)
            {
                anySafes = true;
                projectile.active = false;
                player.chest = -1;
                break;
            }
            /*if (projectile.active && projectile.type == ModContent.ProjectileType<Projectiles.Pets.PiggyBankProjectile>() && projectile.owner == player.whoAmI) {
                projectile.active = false;
                player.chest = -1;
            }*/
        }
        if (!anySafes)
        {
            //Main.playerInventory = true;
            Projectile.NewProjectile(new EntitySource_Misc("Bonfire"), new Vector2(player.position.X + 64, player.position.Y), Vector2.Zero, ModContent.ProjectileType<Projectiles.Pets.SafeProjectile>(), 0, 0, player.whoAmI);
            Recipe.FindRecipes();
        }
    }
}