# Git History Changelog

Source: pushed history from the local remote-tracking ref `origin/1.4.4-tracked`, from `2025-11-01` through `2026-06-20`.

`HEAD` and `origin/1.4.4-tracked` both point to `61b5339eabc8929e849bda4b1fe224a9e4ca1e82` in this checkout.

## 2026-06

### New Class Selection Menu added during New Character Creation ++
- Date: 2026-06-20
- Author: timhjersted
- Commit: `61b5339ea`

Description:

- New character class menu has you select between 4 classes. Choice determines a primary and secondary weapon on spawn, plus light stat adjustments, plus all classes get "Norman's Ring" which offers light stat boosts based on class chosen. Class and New World menus got 50% smaller so they don't clip the screen bottom on small resolutions.

-Mana Crystals now give +10 max mana in SoulsMode instead of +20 to extend progression curve and offer more opportunities to reward exploration

- New Controls config with "Recommended Controls" toggle. Is set to off if any of the custom controls have already been set by user or don't match all custom bindings. Is auto-set to on if all keybinds that would be changed currently use vanilla keys on first load. Toggling on gives quick way to switch all keys at once.

-Controls tutorial message changes dynamically based on what your keybinds actually are

-Finished SF4 and stagger compatibility port for Lothric Knights, Ringed Knight, Hollow Spearman, & Hollow Soldier. These enemies got a couple evasive abilities unique to their combat profile and their shield ability got changed from a basic timer to pre-emptive and reactive (on hit and on incoming hit). Ringed Knight got fire teleport style and fire teleport attack. All SF4 enemies can now TP out of lava.

-Flamelurker got 2 new attacks, Flaming Soulmass and Flaming Meteor. Removed LostSoul projectile attack. Now shoots new FlameOrb and spawns 1 LostSoul projectile on death.

-Basilisk enemies got some of their attacks tuned and expanded (Disrupter has new variants and Shifter's breath attack got a cone radius limitation and chooses only one direction to spray from. All attacks have 'saved position' awareness, so projectiles target player at the time of the TelegraphFlash, 25 ticks before attack. This allows last second dodge rolling behind the enemy to evade. They also got kiting ranges. Polished Tongue Sucker attack.

-Red Knight, Abyssal Ninja invader and Pinwheel now drop a Stamina Vessel. Max possible stamina raised from 185 to 200.

-Animated and improved several enemy projectile sprites, including ShadowShot, WaterTrail, IceSpirit, HypnoticDisruptor & DemonSpirit, plus created FireLurkerFlamingOrb for its new attack

### New behavior system: Kiting, Ported more enemies to SF4 and stagger system ++
- Date: 2026-06-17
- Author: timhjersted
- Commit: `f0b018cc5`

Description:

-Newly created worlds (adventure and classic) now have their date-created stat modified to be the current date, so we can finally keep track of multiple playthroughs easier

-New kiting behavior gives a min and max range for ranged enemies to prefer to stay within rather than blindly getting into melee range constantly. KiteLooseness stat is a chance per re-roll window to NOT back off, so melee can close the gap,  also so kiting doesn't create a rubberband effect with the player.

-Added scary new attack to Basilisk enemies

-Improved SF4 pit escape logic

-Ported GhostOfAHollowWarrior, GhostOfTheDrowned, HollowSoldier and HollowSpearman to SF4 and stagger system, plus added colored telegraph flashes for their core attacks (red, white and blue for jump slash, spear throw and bubble spew). They also got a buff to their shield mechanic, absorbing more damage and stagger damage.

-GhostOfTheForgottenKnight, GhostOfTheForgottenWarrior and GhostOfTheDarkmoonKnight got improved hyperarmor during attacks and also got new Kiting behavior

-TibianAmazon and TibianValkyrie got a couple new evasive moves and new kiting

-Improved and organized Basilisk family enemies (Evasive and CanJumpBeforeAttack moves were named and moved into their related helpers, which can now be individually turned on when an enemy has CanJumpBeforeAttack), plus they got kiting stats

-Moved melee's random 1/8 chance to trigger confuse to the enemy stagger window (now 1/10)

-Slightly increased transparency on melee swing VFX

### New Evasive profiles and abilities for enemies ++
- Date: 2026-06-17
- Author: timhjersted
- Commit: `32638e898`

Description:

-Adjusted control default for Dodge Roll to Left Shift and Smart Select to Left Alt, and edited tutorial tooltip (only changes on first load)
-2nd Slot item no longer fires on right click when the cursor is hovering over vanilla objects that use right click like speaking with friendly NPCs and opening Void Bag

-Revamped RedKnight aka FighterOnHit to EvasiveOnHit with a collection of independent bools that can be batched into profiles for enemy classes or declared individually inline per NPC. The current RedKnight evasive behaviors were named and organized and I also added several new behaviors, which are currently applied to the Dworc and Basilisk families. One new one is the Quick Step, a dash with iframes, used to dodge attacks

-Gave Dworc and Basilisk families compatibility with the new 'stagger to interrupt attack' logic

-After an enemy is staggered during its immunity window or while the enemy has hyperarmor (0 knockback and can't be interrupted), an enemy's stagger bar now turns white. The enemy's immunity window was also changed to 0 knockback instead of reduced flinch.

-Increased NavSearch radius for Basilsisk family (70, 80, 90)

-Nerfed enemy self-heal ability (heals less, less often)

-Added new Evasive CanGoInvisible ability style, applied to most enemies that had the random style. Now enemies go invisible when hit as an evasive behavior and their invisibility stats determine how long the invisibility lasts and the chance a hit will knock them out of invisibility. Longer durations have an equally long cooldown.

-Improved SF4 edge cases (with getting stuck in small pits with lips on ledge and low clearance)

-Improved Basilisk behavior and fixed Basilisk Shifter's glitched flame thrower projectile kill sounds

### Enemy Debug Tome bug fixes
- Date: 2026-06-16
- Author: timhjersted
- Commit: `efc3196a4`

Description:

-Fixed cursor / sprite placement not aligning
-Removed test events

### Reorganization of some enemy files, improved Soapstone interactions and added new attacks for DemonSpirit
- Date: 2026-06-16
- Author: timhjersted
- Commit: `48660f706`

Description:

-Improved mouse and character movement behavior for closing soapstone messages (via walking away or mouse movement)
-Moved some enemy classes into folders so Enemies list isn't so large
-Removed NPC ID from saved events JSON since those IDs change for modded enemies on every reload and were only reliable for vanilla enemies. Now the JSON uses exact names for vanilla and modded enemies. This will keep the github diff from changing arbitrarily all the time.
-Added the ability to edit individual NPCs via the Enemy Debug Tome (WIP)
-Added Ghost Afterimages effect to Demon Spirit, Water Spirit and Crazed Demon Spirit
-Added extra attacks to Demon Spirit and Crazed Demon Spirit and tweaked SFX and projectile dusts

### Enemy Debug Tome improvements
- Date: 2026-06-16
- Author: timhjersted
- Commit: `31f53f5d0`

Description:

- Made various improvements. Will explain all the features when it's done :p Still have a ways to go.

### Stagger and Poise System extended to 46 enemies + More
- Date: 2026-06-15
- Author: timhjersted
- Commit: `2713c6149`

Description:

-Extended Stagger and Poise system to additional enemies and reduced stagger buildup by 50%. KB resist for these enemies is now controlled centrally via GlobalNPC, so it's easy to balance at a glance and remember which enemies are opted in to the system. These enemies get the stun and resist mechanics. Attack interruption on stagger will need to be added case by case by adding attack states to each enemy's AI (like Red Knight class has).

-Added unique stagger mechanic for walking ghost enemies that have 0 knockback for lore purposes. Now, using magic damage or melee weapons infused with magic damage will cause grounded ghost enemies to take flinch and stagger buildup when hit

-Added DirectPounce attack style, as an alternative to default CanPounce bool, now named "HighArcPounce"

-Added "Plague" teleport style, with black and purple dusts, which comes with an additional attack, a plume of plague smoke that causes Curse buildup, used currently by Black Knight

-Added flaming explosion attack to Fire style teleport

-Added globalNPC.CanPassThroughWalls, giving walking ghosts the ability to teleport easily through walls with ghost smoke effect

-Added globalNPC.HasGhostAfterimages to make walking ghosts look like badass scary ghosts

-Added hand sprites for GhostOfTheForgottenKnight, GhostOfTheForgottenWarrior, and GhostOfTheDarkmoonKnight, used for improved telegraph visuals.

-Replaced GhostOfTheForgottenWarrior's spear attack with a new ephemeral axe attack, which triggers 10s of ichor on hit

-Curse and powerful curse now guarantees one of the 1-3 negative stat penalties rolled is max HP loss, and no benefit can roll max HP gain, to keep the debuff accurate to the source inspiration, but all other unique mechanics remain. I also added the negative and positive modifiers the player has to the Curse and Powerful Curse debuff tooltips, so looking at the Humanity item is no longer the only way to find out what happened to you. Also added a sound effect cue when cursed.

-Gave Red and Black Knight aggressive style teleport abilities (TP quickly after losing LOS).

-Increased weapon stamina use from .5 to .75 in Unkindled mode

-Summoner staffs and sentry staffs that used 0 mana now use 10 mana to cast. All vanilla staffs use 10 mana already so this change brings the modded items in line. Spirit Bells use more for lore purposes.

-Slightly simplified crafting recipe for Purging Stone

-Slight nerf to Dunlending Axe

-Additional human enemies can no longer spawn in water

-Ported Hollow Warrior to SF4

-Fixed Thorium Mod events compatibility

### Abyssal Inferno expansion, debuffs stuffs, other stuffs
- Date: 2026-06-14
- Author: Xelvaa
- Commit: `c8cac6bb1`

Description:

- Expanded Abyssal Inferno/Flame of the Abyss stuff, some enemies can now inflict Abyssal Inferno to the player (10 dps), Slighty changed the sprite and the projectile of Defiled Hornet, Changed the projetiles of Serpent of the Abyss and added Abyssal Strider, a SHM abyss variant of Fire Lurker. These enemies are now basically "abyss-themed"
- Added Abyssal Fabric, which replaces Soul of Abyssal Invader and can be use to craft Abyssal Ninja Armor
- Buffed Dark Inferno, Abyssal Inferno and Morgul Poisoning
- Made the display of certain debuffs more dynamic
- Plaguesmith : New rare SHM enemy that spawns in the underground jungle (mostly here for the Great Foundry in the remix map). Basic movement but has an aura around him that debuff the player and buff the enemies inside (they are tankier and deal more damage + inflict venom)
Juggernaut (or Massacre) - not finished but planned as a rare and strong mob in SHM during the night or in the underworld
- Slighty changed the sprite of Oolacile Demon
- Tweaks to Hunter : Increased HP, Flamethrower attack changed, Young hunter nerfed
- Added some debuff immunities to Hellkite Dragon, Ghost Wyvern and Seath
- Fixed Ichor Fragment's hitbox being still alive after the projectile disappears
- Nerfed Flasks

### Added prototype Enemy Poise and Stagger System, Added No Enemy Healthbar config + more
- Date: 2026-06-13
- Author: timhjersted
- Commit: `eb8509f3a`

Description:

-New feature: Poise and Stagger system. Enemies take a modified amount of knockback based on weapon knockback stat and enemy knockback resist, modified by a global tuning value. This allows most enemies to have some amount of knockback (with the exception of ghost enemies for instance), ensuring every enemy hit feels like it registers. Hits build the stagger meter. Fill it to stagger enemies for 2 seconds, slowing them and canceling any wind up attacks. Right before and during attacks, enemies have hyperarmor. In this state they take no knockback and their attacks cannot be interrupted. After a stagger, enemies gain extra poise, making each stagger harder to achieve. They also gain stagger immunity and take reduced flinch for 4 seconds from the stagger trigger.

In addition, enemies now have 'stunlock protection'. They can detect when pinned against a wall and can either jump, dodge roll, or teleport depending on whether they have the ability. Currently active for Red Knight enemies, to be rolled out to further mod enemies in the future.

-Stagger and Health meter added to Enemy Stamina bar.
- Added "Enemy Health Bars" config in the mod's Visual options for cleaner UI. Defaults to off. Players can still see enemy health when hovering over an enemy, and also can see it via the new mod health bar which displays for enemies that are opted in to the stagger system).
- Improved SF4 navigation logic
- Recolored Basilisk Shifter and Dworc Shaman class enemies
- Tweaked fire and smoke teleports to be shorter and freeze the enemy properly during TP
-Reduced frequency of evasive movement for Red Knight enemies by 50% (Now called FighterEvasiveOnHit)

### Enemy improvements + Dropped souls no longer lost when saving and reloading!
- Date: 2026-06-12
- Author: timhjersted
- Commit: `a73cfae01`

Description:

-Red Knight, Great Red Knight, and Black Knight all get pretty polished looking hand sprites so the 'holding weapon' telegraphs look good. I used a hand only sprite overlay similar to Tibian Valkyrie's spear sheet but without the weapon so all 3 knight weapons get the improvement.

- Turned CanGoInvisible into a global bool with per-NPC parameters. Gave ability to Assassin and Red Cloud Hunter.

-Added new 'predator' style for CanGoInvisible (random is the old style). Predator goes invisible while moving and repositioning and goes visible during telegraphs and attacks.

- Gave invisibility effect a shimmering / fading trails effect and also made the Hunter potion effect (show enemies) ability force alpha to 50 so invisible enemies can be seen.

- Turned CanSelfHeal into a global bool with per-NPC parameters and tweaked heal amounts for a few enemies

-Added CanHealAllies global bool with per-NPC parameters and gave to a couple enemies (Necromancer and HydrisNecromancer). Sprite-based heal effect plus heal number used to telegraph

- Migrated FirebombHollow to FighterAI with SF4, combat behavior retained, plus it can now do direct throws rather than only arc throws at close range

- Improved Red Knight nav radius and max jump power.
- Hydris Elemental: toned down leap speed and top speed
- Hydris Necromancer: removed body dust effects that were constant for clarity. Now pink dust spawns when enemy spawns npc.
-Polished smoke and fire teleport styles. Enemies now teleport 90 frames into 2 second dust effect on both ends instead of right at the beginning
-Gave some slight randomness to the exact time that enemies go into patrol / bored state to avoid enemies stacking
-Made improvements with SF4 nav
-Made 2nd Slot easier to place and retrieve items without dead center accuracy
- Dropped souls no longer lost when saving and reloading!
- Tweaked Abyssal Ninja invader stats for SHM and gave the Abysall Star as a drop plus Abyssal Ninja armor now needs 3 Souls Of Abyssal Invader and reduced soul cost

### Improved Enemy Debug Tome and Red Knight hand placement +
- Date: 2026-06-11
- Author: timhjersted
- Commit: `3b5ff2bcf`

Description:

- Added proper braking to SF4 to avoid 'landing on ice' effect for NPCs that were using the pounce ability then transitioning into SF4 navigation
- Deleted Faraam Test Invader armor
-Red Knights get magic ball in hand with new dust for telegraph, removed full body dust telegraph (cleaner). Extended spear/bomb telegraph by 30 ticks
-Enemy Debug Tome properly saves triggered non-permanent events so they won't retrigger until all players have died or until save/reload world, json stores named NPCs for clarity and save fallback for mod npcs, plus other improvements

### FighterAI enemies now have SF4 pathfinding intelligence + progress on Enemy Debug Tome
- Date: 2026-06-11
- Author: timhjersted
- Commit: `1e328de3f`

Description:

-All FighterAI enemies got upgraded to the new intelligent pathfinding system using navradius to determine intelligence (10-20 for simple enemies, 30-40 for moderate intelligence, 50-80 for high intelligence enemies). Lots of enemies also got the ability to climb ropes, as appropriate (human and intelligent walking enemies mostly).

-Progress on Enemy Debug Tome. New "Quick Add" feature for placing lots of simple 1 npc events with basic defaults but with the ability to customize. Still not ready to use. More features and polish to add. But this a good save point.

- Progress on adding a hand sprite to Red Knight and Great Red Knight via code (placing the hand sprite on top of the projectile sprites, so it looks like the enemy is holding the spear or bomb instead of it just floating on top of the enemy). WIP.

- Polish on the Archer Spirit's behavior

-Custom resource bars on by default, with added tooltip

-Made progress on movement behavior of large beasts like Gigas and Massacre, but still needs lots of work

-Added Faraam Sprite test experiment that will either get fixed or trashed (the goal was to convert a sprite sheet to an armor sheet, then use it for an invader)

-Improved Forgotten Pearl Spear and switched spears for Red Knight and GRK

### New Feature! Enemy Debug Tome (WIP)
- Date: 2026-06-09
- Author: timhjersted
- Commit: `3767990e1`

Description:

-Enemy Debug Tome allows easy in-game placement of enemy events! Old events can be seen and edited. New events can be created with right-click while the tome is selected. Left click an event book to see and edit event details. While the tome is selected, event rings can be seen and will not trigger. To test, unselecting the tome will allow event rings to trigger as they would normally (they won't trigger if the in-world conditions aren't met like HM or EoW downed, remix world and you're in classic world etc. When in classic world, events will default to classic map only (Tim's map) and when you're in the remix world, events will default to remix only. I also plan to add a 'quick add' feature so you can pick one enemy and place individual spawns for that enemy with default ring size and an invisible trigger ring. Complex events with multiple enemies per event ring are possible now.

NOTE: The foundation for this feature is almost complete but is not ready to use yet. Still needs further testing and refinement. I may need to reset the json file while testing, which resets the events back to only the manually coded ones. All our manually placed events are preserved and will stay untouched as a fallback. But when it's done, the DynamicEvents.json will house all our event data.

-MinSurfaceWidth logic added for large enemies (large sprites can only walk and jump on minimum 2 width to 4 width wide flat surfaces, preventing odd-looking climbs up slopes or stairs where their sprite is mostly floating in air). Full large beast movement logic coming soon.
-Missing gore sprites added.
-Laid the foundations for FighterAI integration with SF4 (super smart enemy navigation logic and more intelligent local movement abilities (gap crossing, rope climbing when turned on etc).
-Fixed Red Cloud Hunter's arrow projectile not having a kill effect when hitting surfaces and also fixed it not doing damage to the player

### Adjusted Soul Counter, Estus & Cerulean Tear default locations (now also disappear when inventory open)
- Date: 2026-06-05
- Author: timhjersted
- Commit: `4d6bf0057`

Description:

-Polished custom Resource Bar visuals
-Stamina bar above player is now green

### AI / Navigation overhaul (FighterAI), Added New Resource Bar UI, Added Archer Spirit Bell ++
- Date: 2026-06-05
- Author: timhjersted
- Commit: `3cf3c0aab`

Description:

-Added Archer Spirit Bell (summons the ash spirit of Red Cloud Hunter). 0.5s use time. costs 100 mana to cast.

-New Resource Bar UI for Health, Mana and Stamina. Lost HP/mana etc turns yellow briefly before decaying. Yellow decay effect added to small stamina bar above player, with option to disable the entire above-player display in config. Gonna do a little more visual polish in a future pass.

-Reorganized config menu into 3 submenus: Gameplay, Visuals, Sound (and reordered some options by importance).

-Added Wiki script for updating the wiki with 2 .lua files. The files are generated by typing /exportwikidata in the game chat and hitting enter to generate the Lua database files and the XML page importer file. The files still need to be improved to show enemy drops and item crafting recipes or enemy drop sources.

-AI / Navigation overhaul (FighterAI): Replaced the old boredom system with a shared Pursue/Search/Patrol state machine — enemies now intelligently give up, investigate your last-known position, patrol, or blink back to re-engage instead of pacing or freezing. Unified all teleporting into one configurable system (Relaxed/Normal/Aggressive styles with charges, cooldowns, and high-ground bias for ranged enemies), and gave each enemy archetype-tuned behavior (archers/casters seek elevation, ghosts and beasts roam where they lost you, soldiers pace their post, bosses blink aggressively, Tonberry stalks relentlessly). Tier-0 enemies now climb walls scaled to their own jump power and use a smooth step-up instead of bouncing, with a robust anti-stuck that disengages when truly blocked. Under the hood, removed the dead BFS/NavTier pathfinding system, the legacy WeakTeleport, and the old BoredTimer — a large cleanup mostly so far preparing the way for SF4 integration. The main benefits currently are FighterAI enemies inheriting a much more intelligent state machine once they lose track of the player. No more bouncing against walls for ages when they get stuck!

-Reduced Celestriad max mana gain to 50% (was 100%)

- Reduced size of Estus Flask and Cerulean Tear UI by 30%

-Added several enemies and 3 friendly NPCs from Omnir's mod, currently just minimally imported into the game to see how they look. No spawn conditions, loot, attacks or anything. Will see where they could fit into the game potentially in the future. Going to implement at least a couple with new attacks and AI behavior as time permits. Need to ensure I can get the bigger sprites looking right when moving around uneven terrain.

### Improved SmartFighter4 logic, Added Patrol System, cleaned up Smart Nav experiments
- Date: 2026-06-04
- Author: timhjersted
- Commit: `a2da7b1e8`

Description:

-Rope climbing logic is polished. Pursuit system added with pursue, search and patrol states. Patrol states include: idle, pace, wander and returntospawn. 'Search for better angle' logic for SF4 improved when the enemy can't physically reach the player but can find LOS for a projectile attack. Other edge case navigation 'bugs' squashed. Also added 'auto step-up' for SF4 which models how the player steps up 1 tile high blocks.
-Removed failed smart nav experiments. SF4 is the winner. S-tier navigation abilities, tunable via search radius.
-Fixed soapstone bug

### SF4 rope climb and cliff detection fixes
- Date: 2026-06-03
- Author: timhjersted
- Commit: `d5d1bde51`

Description:

_No additional description in commit body._

### Broke up massive GlobalNPC class into 3 partial classes (AIs, DespawnHandler, Navigation)
- Date: 2026-06-03
- Author: timhjersted
- Commit: `b7a4f5827`

Description:

Moved for clarity and maintainability. No behavioral changes - just a refactor to isolate a few distinct functions and improve readability.

- Also improved SF4 rope handling, but still has issues

### Fixed dwarven contract in mutliplayer (thanks to DreamSea) and other things
- Date: 2026-06-03
- Author: Xelvaa
- Commit: `ccacd869f`

Description:

- Added the music menu to the mod
- Fixed potions using the eat food swing animation iinstead of drinking
- Tweaked the Celestriad's sprite
- Rebalanced the movement speed nerf for supersonic wings

### Adjusted Runeterra Gauntlets(Scorching Point and its upgrades)
- Date: 2026-06-02
- Author: Marf
- Commit: `892493e76`

Description:

- Base dmg reduced on Scorching Point, but buffed on upgrades
- DoT effects of their debuffs now respect whether the enemy is immune to DoT debuffs usually
- Now costs a full minion slot to summon a ball
- Balls now scale better with summon tag dmg but dragon scales much worse with tag dmg
- Dragon is now free to summon but will only stay if there is at least 1 ball of the right type summoned
- Dragon dmg scaling has been adjusted: no longer based on dragon tier so higher tiers of dragons will always deal more damage than lower tier dragons(needs to summon 10 balls for the dragon to deal the items full damage)
- Dragon applies marks less often and marks disappear more quickly if not refreshed, proccing full marks deals more dmg but lasts shorter

### 3 New Features: Active Shields, Parries & 2nd Slot
- Date: 2026-06-02
- Author: timhjersted
- Commit: `4a30cc1aa`

Description:

Active Shields is a new mode accessible to Unkindled and BotC players that transforms shields from passive DR stacking accessories into an active gameplay mechanic. Right click to raise an equipped shield to block all frontal damage. Blocking a hit costs stamina (base amount plus an additional % relative to damage negated, so higher damage hits use more stamina). While using the shield, stamina doesn't regenerate and the player is slowed. Better shields use less stamina and slow the player less. If the player runs out of stamina while blocking, the player gets a new debuff "Shield Guard Break" which inflicts ichor and slow for 2 seconds, plus reduced stamina regen. The player can only equip one shield at a time and can't use weapons while actively blocking. Passive DR has been entirely removed, and passive defense has been rebalanced across all the shields to be a minor benefit. Because of these limitations, weapon damage penalties have also been removed, allowing all classes to use shields. Passive debuff immunity benefits have been retained.

In addition, several shields have gained additional unique mechanics or offensive abilities, most of which are tied to the 2nd new feature: perfect parries. For instance, blocking right before an attack with the Dragon Crest Shield emits a short dragon fire blast, and parrying with the Beholder shield gives a chance to confuse enemies. Perfect parries also use 50% less stamina.

The 2 shield accessories that use mana for tanking hits now also use stamina in addition to mana, must also be actively held to block damage, but gain the ability to block damage from all sides, in fitting with their force field fantasy. They also have new abilities tied to holding and releasing the shield.

Note: The prior 'shield mode' and all previous stats and balancing has been preserved for the "Classic" difficulty. SoulsMode players can toggle the mode off in settings.

The final new feature is 2nd Slot - a dedicated 'right mouse click' slot for weapons, utility items and shields, available only to Unkindled and BotC players. If a player does not have a weapon with a alt right click feature currently selected, the item in the 2nd slot will be used when right clicking. Aside from shields, accessories are currently not allowed in this slot, and only one weapon (left click or right click) can still be used at a time, but the new slot gives right click an active, player-chosen role when not in use by a special weapon, greatly expanding the dynamic feel of combat and build variety.

- Unkindled mode: weapons now use stamina, half the amount used in BotC mode, so the mechanic isn't quite as strict as BotC, but enough to create an interesting tension with dodgerolls and shield use.

- Resprited Celestriad (WIP)
- Cleaned up Red Knight Test attack animations
-There are currently 4 shield sprites that are currently just recolored DragonCrestShield sprites, which could use more unique edits
-Fixed walking animations with TibianVS4
-Dark Souls can no longer be moved out of the Soul slot
-Removed broken EoC minion sprite

### Making EoW and Jungle Wyvern immune to regular debuffs
- Date: 2026-06-02
- Author: Marf
- Commit: `178274625`

Description:

_No additional description in commit body._

## 2026-05

### Triad Desperation Tuning (scaled based on difficulty mode) +
- Date: 2026-05-30
- Author: timhjersted
- Commit: `eb474230d`

Description:

-Triad Desperation Tuning: Scaled the final bullet-hell desperation phase to 40 seconds in Bearer of the Curse (what it was before), 30 seconds in Unkindled, and 20 seconds in Classic mode (can change easily based on feedback)

-Unkindled mana regen now has a 30 second delay when using mana (so cerulean tear is necessary for fights, but this allows one CT flask to be refilled after the cooldown)
-Reduced hurt sounds another 20%

-Revamped and cleaned up RedKnightTest

-Improved Abyssal Ninja hitbox, crossbow aim, shoots crossbow more often

### New "player hurt" and "player killed" sounds added with dynamic playback
- Date: 2026-05-30
- Author: timhjersted
- Commit: `86cbbeb02`

Description:

- The new play hurt sounds (1-5) play based on how much damage the player took as well as how much total health the player has. As player health gets lower, a new sound plays at different health thresholds to telegraph low health or huge hits. The original player hurt/killed sounds can be turned back on via config toggle.
- Added new sound to dark souls retrieval and 'new location' found (all from Dark Souls)
-MainMenu background is no longer black and uses default background
-Fixes for Red Knight attack behavior
-Added 'fade to black' effect on player death and changed death text to "YOU DIED"

### Enemy test tweaks
- Date: 2026-05-30
- Author: timhjersted
- Commit: `8627610dd`

Description:

_No additional description in commit body._

### Changes on worm, meltdown, and arcane lightrifle
- Date: 2026-05-30
- Author: Xelvaa
- Commit: `285e8feda`

Description:

- Meltdown and freezedown heavily nerfed against every worms, no longer shred Destroyer
- Buffed Destroyer HP and Defense
- Pierce resistance is also applied on the SHM worms bosses
- Slighty buffed The Machine hp
- Added a pierce limit to Arcane Lightrifle, and fixed the projectile not being magic (cannot proc things like celestial cloak mana rain)

Also made the logo less AI looking

### Resprited Ancient Warhammer and Barrow Blade with a couple of tweaks
- Date: 2026-05-30
- Author: Xelvaa
- Commit: `b8814ab80`

Description:

- Changed the rarity of Covenant of Artorias and Dwarven Contract
- Added the Human NPCs list
- Slighty nerfed Gigant Axe
- Slighty buffed Barrow blade, Gaia Blade and Ancient Warhammer (now Dwarf Warhammer)

### Added Crafting Recipe Guide slot
- Date: 2026-05-30
- Author: timhjersted
- Commit: `349731168`

Description:

-Added Crafting Recipe Guide slot to player inventory, to the right of Souls slot. Functionality mirrors what talking to the guide offers, making Recipe Browser no longer totally necessary
-Abyss mode's doubled spawn rates and total spawn capacity has been returned, without the blood moon's red tint effect
-RedKnightTest continues to be very janky but the work continues
-Improvements to SmartFighter4 (rope climbing abilities improved, as well as platform detection, downward descend intelligence, and complicated house routing). Still not applied to any spawning enemies in-game.

### SoulsMode Mobility Limit, Abyss VFX revamp, Great Magic Mirror revamp + more
- Date: 2026-05-30
- Author: timhjersted
- Commit: `783888c02`

Description:

-Revamped VFX for Abyss mode (permanent bloodmoon is no longer active to remove red tint from world so I'll need to double enemy spawn rates manually in future patch)
-SoulsMode Mobility Limit: Reduced the max top speed, acceleration and flight time of the player globally, with specific tuning of several late game items (mainly Supersonic accessories). These changes are exclusive to Souls Mode (Unkindled or BotC) and can be turned off via the mod options. Design goals: 1) Bring back the slower paced Dark Souls feel that is more present at the beginning and middle of the game. 2) Prevent players from blasting through dungeons at mock 10 speed, which breaks immersion and intended experience (captured the best in early game before mobility accessories get out of control). 3) Encourage use of the dodgeroll for evasion
-Removed no fall damage perk from Dragoon Gear and Supersonic Wings II (design goal: retain danger of fall damage in late game or must use accessory slot)
-Revamped Great Magic Mirror to be a short range teleport tool, which can only be used within 100 tiles of the saved waypoint. Design goal: Transition the item to be used as intended: to make specific jumping or traversal challenges less frustrating, allowing for a quick 'try and reset' option for challenging map sections. Previously, the item could effectively break all traversal tension by allowing the player to constantly set new save points, eliminating the tension of reaching the next bonfire or dying and losing traversal progress. If enough people request it, the original function can be moved to 'classic' difficulty.
-Invaders and player-drawn enemies can use wings and aerial attacks (WIP)
-Invaders now have expanded weapon equip options with 5 attack combos per weapon archetype (WIP)
-Standing near a bonfire now allows the player to craft items with dark souls. Demon Altar functionality is retained as a 'in the field' crafting alternative.
-Soapstone messages disappear when you move your mouse a smaller distance
-Improvements to SmartFighter3 movement AI (currently being worked on via SmartFighter4)
-Title screen logo and black background added (WIP)

### Improved SmartFighter3 NPC pathing: ropes, cliffs, halts
- Date: 2026-05-29
- Author: timhjersted
- Commit: `0a78615d9`

Description:

Enhanced SF3 navigation and movement behavior: add halt-at-attack-range so ranged NPCs brake and wait while attacking; avoid blindly walking off cliffs by braking when a large drop is detected ahead. Introduced BadEdgeTargets memory (with pruning) and BadEdgePenalty so recently-failed step targets are deprioritized during replanning. Add tight-landing penalty for narrow landings and adjust drop penalties. Make platform-drops per-column (require an actual platform tile) instead of span-only. Added rope support: detect rope columns, add RopeClimb edges to the graph, a HasRopeColumn helper, and ExecRopeClimb to execute climbs. Improved jump handling: tighter alignment and velocity settling for pure-vertical jumps and zero horizontal velocity + clear air-commit on vertical jump-fire. Ensure npc.noGravity is restored on OnHit, plan completion, and step timeouts. Misc: wired up new StepKind/EdgeKind entries and integrated bad-edge bookkeeping when a step times out.

### Progress with SmartFighter3AI and new experimental sprite replacement for Red Knight (WIP)
- Date: 2026-05-27
- Author: timhjersted
- Commit: `f46faeae9`

Description:

I'm attempting to use a player's armor sprite for the Red Knight enemy. Just a test at the moment. Nothing in game.

### Added WIP SmartFighter AI and Tibian Valkyrie test NPC
- Date: 2026-05-27
- Author: timhjersted
- Commit: `ccceb2cd7`

Description:

Introduced the foundation for a new SmartFighterAI implementing advanced navigation and combat behaviors (route planning, gap/rope/platform handling, jump/gap heuristics and projectile use). Nothing yet is live in game. Just some testing NPCs.

### PROJECT INVADER (Phase 1, WIP, not yet in-game) + various fixes and changes
- Date: 2026-05-25
- Author: timhjersted
- Commit: `56918c962`

Description:

Invader System
----------------------------------
-New Invader enemy class that uses player armor sprites and player weapons for visuals instead of traditional enemy sprite sheets. This allows weapon swing, hold and shoot animations for swords, throwables/magic and bows/crossbows respectively. Upon spawning, an "INVADED BY [NAME OF INVADER]" text appears on screen, same as location announcements. Currently have 1 test invader, the Abyssal Ninja, with an assortment of ranged and melee attacks. No spawn or loot set yet. The global invader cs is building a number of abilities, which individual invaders can inherit or not based on type.

Enemy AI / Navigation
----------------------------------
-Added an experimental smarter navigation framework for future enemies, including waypoint state, ledge/run-up handling, jump tuning, and nav logging. All enemies currently use the old nav system (nav tier 0).
-Added WIP opt-in CanStopToFire so enemies “plant and fire” when they are at a ledge but have LOS and can hit the player rather than walk off a ledge or steep drop off where they would lose LOS.
-Added per-attack line-of-sight support so normal projectiles require LOS by default, while special attacks or attacks that can pass through walls like poison storm can fire without LOS.
- Added navigation debug info in debug mode and log output for diagnosing stuck enemies.
-Added WeakTeleport, a limited fallback teleport that can only be used twice total by each NPC and places the enemy farther from the player with a normal teleport telegraph.

Enemy Balance / Behavior
----------------------------------
-Adjusted piercing immunity for The Destroyer so piercing weapons can still hit multiple segments, but cannot rapidly hit the same segment repeatedly.
-Added or adjusted jump tuning values on several enemies for future navigation work.
-Updated Red Knight, Black Knight, and Great Red Knight projectile behavior so more attacks respect line of sight.
-Improved some Red Knight / Great Red Knight attack targeting by using the player’s center instead of top-left position and added clearer telegraphs for some falling/air attacks. Also fixed the attack logic so that the projectile direction is set at the time the telegraph flashes so a player can dodge roll through an enemy and its attack successfully
-Added Tibian Valkyrie scaling for Hardmode and Super Hardmode, including stronger spear damage.
-Added WeakTeleport to Tibian Valkyrie/Basilisk-style enemies as a limited fallback movement tool.
-Changed Dworc Shaman poison storm and demon attacks to not require line of sight, since those attacks are meant to work through walls. Now that enemies don't attack without LOS by default, I still need to add no LOS requirements for other enemy attacks that can go through walls.

Soapstones / Map Notes
----------------------------------
-Reduced the distance where the soapstone “Show” button appears.
-Location text triggers from a slightly farther distance from player
-Updated several soapstone entries with new location names and clearer map-note wording.

Visual / Rendering Fixes
----------------------------------
-Improved poison storm attack visuals so the damaging ring has a clearer, more solid edge while expanding.
Applied the same clearer expanding-ring effect to Abysswalker’s abyss storm attack.
-Fixed Hellkite Dragon body dust to use the correct dust ID instead of a projectile ID.
-Eye of Cthulhu now uses Chinese Terraria edition sprites
-Reduced the Red Knight / Great Red Knight bomb telegraph sprite size by 20%.
-Melee animation: player now faces direction of swinging weapon, even when walking backwards; jumping feet animation frame no longer present when walking or standing animation is more appropriate while swinging.
-Made VFX for melee trail/swing animations reactive to lighting so VFX aren't fully lit in the dark.
-Fixed weird sound that played for Dworc Shaman enemies

### Nerfed Gem Box
- Date: 2026-05-22
- Author: Xelvaa
- Commit: `f134b52b6`

Description:

Gem Box now increases magic attack speed by 50% instead of 100%
Bad damage multiplier decreased from 30% to 20%
All SHM bosses drop 1 Holy War Elixir now, with other adjustements
Slighty nerfed Meltdown
Slighty increased Heaven's Tear's ball aura

### Merge pull request #84 from DreamSea/misc-fixes
- Date: 2026-05-22
- Author: Xelvaa
- Commit: `7ec16c898`

Description:

Misc fixes

### add armor pen tooltip to KrakenCarcass
- Date: 2026-05-21
- Author: 張
- Commit: `0e336de62`

Description:

_No additional description in commit body._

### fix Lionheart Gunblade shoot animation
- Date: 2026-05-21
- Author: 張
- Commit: `e96163572`

Description:

previously first shoot animation after swing was bugged with lionheart aim not matching projectile.

logging suggests order of calls looks like
- AltFunctionUse() // if user right clicks
- CanUseItem()
- Shoot()
- UseStyle() // repeated during weapon animation

---

- set AltFunctionUse() to always true, i think tells terraria to conditionally set altFunctionUse value depending on click
- do useStyle (and noMelee) checks in CanUseItem(), i think what was happening was setting Item.useStyle in calls further down was too late, and the initial frame/rotation has already been chosen by the time Shoot() is called.

### Bunch of soapstone tweaks for remix map
- Date: 2026-05-21
- Author: Xelvaa
- Commit: `1fd4e29f4`

Description:

_No additional description in commit body._

### Boss Hunting Tome Upgrades / Location Names added to Map!
- Date: 2026-05-19
- Author: timhjersted
- Commit: `245987141`

Description:

The tome's menu now communicates progression at a glance instead of being a flat wall of question marks.

- Yellow "next critical path" indicator: the ? for the single next undefeated critical-path boss across all three eras renders in yellow instead of black, signposting where to head next. Highlight updates automatically as each critical path boss falls.
- Critical-path clue gating: replaced the era-only gate with a progression-aware gate. Hover hints now surface only for undefeated bosses with rarity ≤ the next undefeated critical-path boss. After each critical-path defeat, the cluster of optional bosses up to the next critical path unlocks their clues (designed with a purple question mark). Era gate still applies as a safety net so HM/SHM clues can't leak in pre-hardmode.
- Critical-path data: new `CriticalPathRarities` HashSet on `BossSelectVisuals`. Edit there if progression changes.
- Open menu without a kill: removed the "No rematchable bosses defeated!" block at `BossRematchTome.UseItem`. Fresh characters can open the tome the moment they receive it from the Emerald Herald.

## Life Crystal HP scales with party size
Pre-existing Souls-mode Life Crystal nerf (10 HP instead of 20) now scales with active-player count to keep multiplayer parties closer to vanilla progression:

1 (solo)  +10 HP
2–3  +15 HP
4+  +20 HP

Classic-tier players are unaffected. Implemented in `tsorcGlobalItem.OnConsumeItem` — vanilla applies +20, the hook subtracts the partial nerf back. Player count is sampled at consumption time, so the nerf is locked in when the crystal is used (party-size changes after the fact don't retroactively rebalance). However, if the player uses the Darksign to revert to classic mode, their health is reverted to what it would have been.

## Chloranthy ring tuning
- `BrokenArmor` removed from the Chloranthy + Estus drink branch. The remaining trade is just `Ichor` (−15 defense + yellow glow). Roughly a 25–30% damage-taken increase during the drink window vs the old ~100% with BrokenArmor stacked.
- The trade-off mechanic was switched to Estus use as Cerulean Tear use has no movement penalty
- Both Chloranthy ring tooltips updated to reference the Estus Flask and the lighter Ichor-only cost.

## Location banner fixes and improvements:
-HUGE: Added location names to map after discovery!!!
They disappear naturally as you zoom out to avoid text overlap.
- Location banner is now centered even when UI isn't set to 100%.
- Off-by-half-tile proximity trigger: `SoapstoneTile` was using `(i + 1, j + 1) * 16` as the proximity anchor, which is the bottom-right corner of the 1×1 sign tile rather than its center. Fixed to `(i * 16 + 8, j * 16 + 8)` so the 40-px trigger circle sits centered on the actual sign. Players walking past from the left no longer have to overshoot before the banner fires; mouse-hover detection benefits from the same fix.
- `LOCATION_BANNER_HOLD` raised from 240 to 360 frames (+2 seconds, total visible time now 7.5s).
- Missing Path of Ambition, Corrupted Tunnel and Arazium's Mountain Caverns locations. Also added a few new location names. (Changes not yet on remix map file)
-Updated the soapstone system so that soapstone text edits will appear when loading existing world files (previously edits were baked in on first load only).

MISC:
Charm of Myths and a couple other items: Botc benefit now extends to Unkindled mode as well
Boss clues: Fixed some typos and improved clue clarity.
Fixed the Life Crystal 10HP change (for Unkindled and BotC only). Wasn't actually working yet.

### Removed Ragnarok, Added Gore for Great Abyss Demon,
- Date: 2026-05-19
- Author: Xelvaa
- Commit: `bef0ef6b5`

Description:

Also :
- Resprited Abyssal Deluge with a buff
Nerfed Heaven's Tear and Sundering Light damages
Some tweaks on Serris (visual and audio)
Great Demon of the Abyss now drops flame of the abyss and cursed soul instead of ragnarok (because well removed)
Fixed Heaven's Tear raining not working
Balanced Dimaond Crusher

### Merge pull request #83 from DreamSea/modded-spear-projectiles
- Date: 2026-05-19
- Author: Xelvaa
- Commit: `47bb6b8f2`

Description:

have Gaebolg poke/thrown subclass ModdedSpearPojectile*

### have Gaebolg poke/thrown subclass ModdedSpearPojectile*
- Date: 2026-05-18
- Author: 張
- Commit: `bfbbe37ac`

Description:

trying to have texture vs projectile center / hitbox logic 'just work' for subclasses, though left PostDraw alone for now since i couldnt think up a satisfying way to deal with alpha / offset, and iteration bounds.

options for future?
- children implement `PostDrawAfterimage(lightColor, texture, origin, drawPosition)` and call Main.EntitySpriteDraw
- children provide PostDrawAfterimageCount(), PostDrawAfterimageOffset(i), and PostDrawAfterimageAlpha(i)

### Added DiamondCrusherShockwave & miscellanous changes
- Date: 2026-05-18
- Author: Xelvaa
- Commit: `1f9aac2f6`

Description:

- Buffed Crimson Potion
- Buffed Bloom shards
- Resprited Crimson and Shockwave Potion
- Changed the recipe of supreme dragoon weapons and other
- Made more enemies dropping Flame of the Abyss
- Some hitboxes changes/fixes on
projectiles
- Removed Covenant of Artorias effects other than enter the abyss
- Some progress on bestiary beecause why not lol

### Introducing Unkindled Mode (PT1), Enhanced Soapstone System (PT2) + more
- Date: 2026-05-18
- Author: timhjersted
- Commit: `ef2231d5d`

Description:

PART 1: UNKINDLED MODE
------------------------------------------------

Adds a middle tier between Classic and Bearer of the Curse. Several of the mod's unique mechanics (Estus Flask, Cerulean Tear, related upgrade items) were previously gated behind BotC, which the Darksign item told new players to skip. That meant most first-time players never experienced a large part of the mod's identity. Unkindled is now the default experience, BotC stays as an opt-in hard mode, and "Classic" remains for players that prefer Terraria's vanilla healing and mana systems.

**Player tier flag**
- Added `tsorcRevampPlayer.Unkindled` + computed `SoulsMode` property (`Unkindled || BearerOfTheCurse`).
- Added `ApplyHealing(int baseAmount)` helper returning `full / half / zero` by tier.
- Save migration in `LoadData`: existing non-BotC characters auto-promote to Unkindled on first load. Existing BotC stays BotC. New characters spawn Unkindled with a Darksign granted via `OnEnterWorld` as before.

**Darksign — tri-state cycle**
- Left-click cycles `Unkindled → BotC → Classic → Unkindled`.
- Tooltip rewritten to describe all three modes and the cycle direction.
- "Not recommended on first playthrough" warning removed.

**Souls-mode gating** — widened from BotC-only to `SoulsMode`:
- Estus + Cerulean flask UIs and bonfire refill loops
- Lifegem / RadiantLifegem / StarlightShard heal-tick blocks (moved out of the BotC-gated `PostUpdateEquips` section)
- Soul-mode boss drops: `FirstBagCursedRule`, scripted-event Estus Shards, first-kill drops in `VanillaChanges.cs` (Sublime Bone Dust, Estus Flask Shard, doubled Humanity)
- Quick Heal / Quick Mana auto-drink (`MethodSwaps.CustomQuickHeal` / `CustomQuickMana`)
- Star / SugarPlum / SoulCake → Cerulean conversion (`MethodSwaps.On_Player_PickupItem`)
- Soul Vessel pickup + Sublime Bone Dust bonfire consumption
- `BearerOfTheCurseEnabled` recipe condition is now an alias forwarding to `SoulsModeEnabled` — recipe files don't need to change, but `tsorcRevampWorld.cs` exposes both names.
- Emerald Herald now has unique Unkindled gift bundle and flavor text.

**Preserved as BotC-only**:
- Class mechanics (Lethal Tempo / Accuracy / Cerulean magic amp / Conqueror)
- Stamina-draining weapons + healing-item block
- Hard mana-regen pin and Hollowed (20% max HP on death)
- Extra accessory slot
- +20% Dark Soul drop amplifier
- Great Magic Mirror / Village Mirror restriction (re-added — was previously commented out — with BotC warning tooltips)

**Tier-aware healing**
- All mod food/potion items (5 mushroom skewers + BloodredMossClump) refactored to call `ApplyHealing`.
- Tome of Health uses `ApplyHealing` and shows a tooltip line in Unkindled.
- Vanilla healing items routed through new `GlobalItem.GetHealLife` override in `tsorcGlobalItem`. BotC `CanUseItem` block (already blocks `healLife > 0`) is unchanged.

**Mana regen split** (`Player/tsorcRevampPlayerUpdateLoops.cs` `PostUpdateEquips`)
- BotC: hard pin `manaRegenDelay = 100` each frame while in combat or away from bonfire (unchanged).
- Unkindled: `manaRegenBonus -= statManaMax2 × {2/5 standing, 3/10 moving}` to deliver roughly −60% standing / −90% moving across all pool sizes. Numbers need playtesting - currently set as a midpoint between Classic and BotC.

Unkindled life regen - `lifeRegen = lifeRegen × 3/4` (−25%) while in combat or away from bonfire, half of BotC's `/= 2`.

Description.txt rewritten to explain the three tiers with Unkindled as default.

## Boss Hunting Tome (renamed from Boss Rematch Tome)

- Display name changed in localization; internal class name `BossRematchTome` preserved for save compatibility.
- Tooltip rewritten to communicate dual purpose: hover for hints + click to re-summon.
- Hover behavior:
  - **Defeated boss**: unchanged "Next Boss:" hint (rarity + 1).
  - **Undefeated (???)**: new "Where to find:" hint for that boss, era-gated. PHM hints visible always; HM hints require `Main.hardMode`; SHM hints require `tsorcRevampWorld.SuperHardMode`. Prevents end-game spoilers.
- Added parallel `PreHardmodeRarities` / `HardmodeRarities` / `SHMRarities` lists in `BossSelectVisuals.cs` so undefeated bosses (rendered as bunnies) can still look up their intended clue index. Falls back to the parallel list for defeated bosses too — some entries (Golem head, Moon Lord eye) override their NPC type at draw time and zero out the runtime rarity.
- Chaos rarity fix: `NPCs/Bosses/SuperHardMode/Chaos.cs` was missing `NPC.rarity` (defaulted to 0, surfaced clue 1 / Leonhard's). Set to 42.
- Comment block added in `en-US.hjson` mapping each clue index 1–45 to its boss, file path, and era. Vanilla boss rarities live in `NPCs/VanillaChanges.cs` per-case in the `npc.rarity = N` lines.
- Acquisition change: Emerald Herald now offers the tome as a separate gift step between greeting and the tip sequence (new `chatState = 20`, gated on new `ReceivedHuntingTome` save flag). Shaman Elder shop entry + Demon Altar recipe unchanged as backup routes.
- Clue text quality still needs a full pass — accuracy/consistency review.

## Combat / balance

Chloranthy Ring I & II (`Player/tsorcRevampPlayerDodgeRoll.cs`)
- Fixed double-applied speed multiplier — `speedMultiplier` was being applied to `dodgeSpeed` on one line and then re-applied when assigning to `Player.velocity.X`, producing ~1.96× intended velocity on the ground. Removed the duplicate multiply.
- Ring 1 dodge-speed boost reduced from `+3f` to `+1f`. Ring 2 from `+6f` to `+2f`. The compound effect of ring + double-multiply + momentum carry was producing an unintended "fling across the room" effect; now the dodge feels tight on stop and retains momentum cleanly from a run.
- *Known issue*: BotC currently has no Cerulean-drink stamina restoration (removed for class balance). TODO: move the BotC bonus to the Estus drink path instead.

*Life Crystal HP nerf - Unkindled and BotC players gain +10 HP per Life Crystal (was +20). Classic unchanged. Implemented in `tsorcGlobalItem.OnConsumeItem`: vanilla applies +20, hook subtracts 10 back. Stretches the HP curve further into PHM so players don't cap on hearts by Earth Temple.

## Polish

- Removed continuous per-frame dust at the player's feet during Estus / Cerulean drinks - these were Crippled debuff visuals leaking into the channel duration, not intended flask feedback. Held-flask sprite + gulp sound carry the moment.
- Synced flask draw thresholds (`* 0.4f → * 0.05f`) in both `tsorcRevampPlayerEstus.cs` / `Cerulean.cs` body-frame and the parallel `tsorcRevampPlayerVisuals.cs` sprite-draw checks — eliminates the perceived input delay between key press and when flask appears.
- `RadiantLifegem.Item.scale = 0.85f` - was reading as oversized next to the player when held up. Hitbox unchanged.
- Removed Copper Shortsword and Copper Axe from starting inventory.

PART 2: SOAPSTONE OVERHAUL
------------------------------------------------

Schema
Added two optional fields to every soapstone JSON entry:
"category" — comma-separated tags. Valid values: story, lore, hint, tutorial, location. Missing/empty → treated as hint.
"locationName" — display name for the on-screen banner. Rendered in white uppercase. Independent of category — present or not.

New runtime behaviors
----------------
Closed-by-default soapstones. Walking near a sign no longer auto-opens its bubble. A Show {Tags} prompt appears instead (e.g. Show Story, Show Location & Hint). Click to open.

Location banner. Walking near a sign with a locationName fires a large white all-caps banner at top-center of the screen, fading in/holding/fading out over ~3.5 seconds. The banner fires only when the encountered location's ID is different from the last one shown — so re-entering the same area is silent, but moving between locations re-fires.

Category filtering. Players can disable entire categories via config. A multi-tag sign survives if any of its tags remains enabled. hint and location are never disable-able (they're load-bearing for gameplay).

Config options (Mod Configuration → tsorcRevamp Config)
----------------
Option					                Default	Effect
AutoOpenSoapstones		    false 	    If true, restores old auto-open-on-proximity behavior.
DisableStorySoapstones	    false	  	    Suppresses story-only signs.
DisableLoreSoapstones		false		    Suppresses lore-only signs.
DisableTutorialSoapstones	false		    Suppresses tutorial-only signs.
DisableLocationBanner		false		Disables the white-caps banner.

JSON re-sync on every world load
----------------
BuildSoapstones now treats the JSON as the source of truth on every world load, not just the first:

Existing soapstones: text, textWidth, category, locationName are re-patched from JSON. Per-player state (read, hidden) is preserved.
New JSON entries (not in the save yet) are placed only on empty tiles. If a player block, chest, vanilla sign, or any other tile occupies the coord, the placement is skipped and a warning is logged to client.log.

Tagging applied
----------------
tsorcSoapstones_en-US.json — 180 entries tagged
tsorcRemixSoapstones_en-US.json — 231 entries tagged
Other localizations (ru-RU, zh-Hans, Remix variants) are untagged. They still work — every untagged entry defaults to hint.

Authoring guide
----------------
Adding a new sign
Append a new object to the relevant JSON file:

{
  "text": "Your text. Use --NEWLINE  for paragraph breaks.",
  "tileX": 1234,
  "tileY": 567,
  "textWidth": 320,
  "style": 1,
  "category": "hint,location",
  "locationName": "SOME PLACE NAME"
}
category and locationName are optional. Omit them and the entry behaves as a hint.
For pure location signs (just announces a place): "category": "location" + "locationName": "...". Body text usually short.

For hybrid location+hint signs (announce + give gameplay info): "category": "hint,location" + "locationName": "...". Banner fires; bubble shows the hint when clicked.

For story/lore/tutorial: single category, no locationName.

Getting coordinates
----------------
In-game: enable DebugMode in config (or right-click the DebugTome item), then press P (Print Position keybind). Your tile coords print to chat in X = …, Y = … format — paste straight into JSON.

Editing existing signs
----------------
Edit the JSON, reload your world. Changes to text, textWidth, category, locationName propagate automatically. No need to start a fresh world.

Removing a sign
----------------
Two parts:

Delete the JSON entry.
The soapstone tile remains in existing saves. The sign body will still show whatever text was last saved in the world file. The tile will be removed for fresh worlds.

Things to be aware of
----------------
Hint is silently the default
Any entry without category is treated as hint. This is intentional (keeps untagged translations working) but means it's easy to forget to tag a new story sign. If a sign you expect to be filterable isn't responding to DisableStorySoapstones, check that it actually has "category": "story".

Single-line category field
Categories are comma-separated within one string: "category": "hint,location", not an array. Whitespace around commas is tolerated. Case-insensitive. Unknown tags are treated as "allowed" (won't get filtered) — so a typo like "strory" will silently make the sign behave like a hint that can't be disabled.

Banner trigger uses entity ID, not cords
----------------
The "don't re-fire on same location" check uses the runtime TileEntity.ID, which changes between sessions. So returning to a location in a new session will fire the banner again — that's intended (helps with re-orientation after a break).

Banner is client-side only
----------------
In multiplayer, each player sees their own banner based on their own position. Banner state is not synced. No additional MP work needed.

New signs only auto-place on empty tiles
----------------
If you add a JSON entry at coords occupied by something else in an existing save, the placement is skipped and logged. To force placement, manually clear the occupying tile in-game, then reload. For brand-new world authoring, this isn't an issue — all coords are empty.

Duplicate coords in JSON
----------------
A couple of entries in the Remix file have duplicate (tileX, tileY). Only the first one placed wins; the second is effectively dead data. If you're authoring, give each sign a unique coord.

Localization
----------------
ShowPrefix, TagStory, TagLore, TagHint, TagTutorial, TagLocation were added to en-US.hjson, ru-RU.hjson, zh-Hans.hjson. The ru/zh translations are machine-translated stubs — review them before shipping.

The legacy UI.ClickToShow key is no longer used by the soapstone UI but kept in the hjson files for safety.

Hybrid signs need both fields
A hint+location sign needs both "category": "hint,location" and "locationName": "...". The location tag alone (without locationName) won't show a banner — and locationName without location in category still shows a banner (the banner fires off locationName presence alone, intentionally).

### Finished Covenant of Everlasting Love, couple of fixes
- Date: 2026-05-17
- Author: Xelvaa
- Commit: `c528a2301`

Description:

- Finished Covenant of Everlasting (need a better sprite still)
- Fixed Mark of the Hunt's vanity effect not being centered into the right eye
- Resprited Soul of the Ghost Wyvern to match the sprite of the boss
- Replace Essence of Oolacile by Soul of the Occultist
- Couple of description changes for the souls especially
- Added the Water Npcs region with a couple of new npcs here
- Miscellanous changes about flails

### Merge pull request #82 from DreamSea/modded-spear-projectiles
- Date: 2026-05-17
- Author: Xelvaa
- Commit: `5fcb8abe9`

Description:

Modded spear projectiles

### ModdedSpearProjectileThrown parent class
- Date: 2026-05-16
- Author: 張
- Commit: `9bcd320fd`

Description:

create a ModdedSpearProjectileThrown parent class so that child classes dont need to worry about sprite offset stuff

ModdedSpearProjectileThrown is mostly branched from LonginusThrown at the moment, will want to move Longinus specific stuff back to LonginusThrown once more things are using ModdedSpearProjectileThrown

### LonginusThrown sprite offset
- Date: 2026-05-16
- Author: 張
- Commit: `27ec1c550`

Description:

offset LonginusThrown sprite so collision / projection center is at spear-tip

... at low projectile speeds this makes the spear look very spear-tip heavy

### move LonginusPoke to ModdedSpearProjectile
- Date: 2026-05-13
- Author: 張
- Commit: `92860c258`

Description:

referenced previous LonginusProj.cs, but adjusted HoldoutRangeMin, scale, and dust to try and match current look.

### Last commit for 1.4.4, a lot of progress but a lot of wip stuffs too!
- Date: 2026-05-14
- Author: Xelvaa
- Commit: `d6c5394a5`

Description:

Honestly, too much to cover lol the changelog covers that
I will try to make smaller commits and less miscellanous stuff here and here to be more organise and to list more easily all the changes
Also, I saw that a 1.4.5 alpha for devs on tmod has been launch, so I will try to get into it !

## 2026-04

### Oops
- Date: 2026-04-14
- Author: Xelvaa
- Commit: `b9af228fe`

Description:

_No additional description in commit body._

### A lot of changes and content!!
- Date: 2026-04-14
- Author: Xelvaa
- Commit: `bd5f2da23`

Description:

Too much in this commit to cover but basically
- Reworked some spears especially longinus and gae bolg
- Experimental stuffs on NPCs because why not (I have some plans for them)
- Added Eye Of The Hunt accessory (name and sprite VERY WIP)
- Added Shadowspark bullet
- Bunch of random tweaks, balance, folder management
- Some changes on Enemies, like spawn, Scaling or anything
- Remix Map Stuff, as always
- Started to write some achievements ideas
- More miscellanous stuffs here and here

## 2026-03

### Progress on Chaos
- Date: 2026-03-03
- Author: Xelvaa
- Commit: `c50c46640`

Description:

_No additional description in commit body._

## 2026-02

### Big patch, Reworked Chaos for real this time (thanks to Boatsoon, still wip though)
- Date: 2026-02-28
- Author: Xelvaa
- Commit: `6e3324379`

Description:

See the changelog, but basically a lot of summoner tweaks, Added Sanguinis, Dark cloud changes, some fixes here and here

### update zh-Hans
- Date: 2026-02-07
- Author: urgiv
- Commit: `dfc7c0e46`

Description:

_No additional description in commit body._

### Resprited Trinity, Fixed rocket convertion for Blast and Evolution Beasts are GONE
- Date: 2026-02-02
- Author: Xelvaa
- Commit: `043f8dfee`

Description:

_No additional description in commit body._

### Big commit, added/removed some weapons
- Date: 2026-02-01
- Author: Xelvaa
- Commit: `3220fbc41`

Description:

Go to the changelog to see all the changes and additions.

## 2025-12

### More soapstones
- Date: 2025-12-21
- Author: Xelvaa
- Commit: `2b7182640`

Description:

_No additional description in commit body._

### Tweaks on AOS, Seath, Chaos, True DEath and others
- Date: 2025-12-12
- Author: Xelvaa
- Commit: `c91321bc5`

Description:

Make enemies in SHM immune to lava
Tweaks on Lonely fairy size and sprite
Buffed some damage values on some
SHM bosses
Renamed AOS
Repsrite VOEG
Tweaks on some SHM enemies

Remix map stuffs

## 2025-11

### Reworked Dwarven Contract, Progression on Thorium compatibility, Added Vault Of Endless Greed
- Date: 2025-11-30
- Author: Xelvaa
- Commit: `e51bd7f27`

Description:

Also:
Tweaks on Sundered Moon and Ultima Tome
Removed Leonhard Remix
Renamed some enemies
Remix map stuffs
Absolute Death fightable
Tweaks on Corrupted elemental and Ice skeleton sprites
and huhh stuffs I guess

### Merge pull request #80 from DreamSea/misc-fixes
- Date: 2025-11-27
- Author: Xelvaa
- Commit: `53bcd30a2`

Description:

    fix HP not scaling with NPC.damage = 0
    fix LightOfDawn/SunderedMoon projectiles sometimes passing through enemies without doing damage

### fix LightOfDawn/SunderedMoon tomes collision checking
- Date: 2025-11-26
- Author: 張
- Commit: `35cf6545f`

Description:

LightOfDawn/SunderedMoon projectiles were sometimes passing through enemies without doing any damage

switched to calculating padding using trailPositions.Count, which is what most of other DynamicTrail subclasses use https://github.com/search?q=repo%3Atimhjersted%2FtsorcRevamp+%22collisionEndPadding+%3D+trailPositions.Count%22&type=code

---

projectile collision checking for DynamicTrail is done with `for (int i = collisionEndPadding; i < trailPositions.Count - collisionFrequency - 1 - collisionPadding; i += collisionFrequency)`

the projectiles reach trailMaxLength quickly, so collisionEndPadding = (int)250 / 30 = 8, and it wasn't uncommon for trailPositions.Count to be 16 or less (with collisionFrequency 5 and collisionPadding 2), resulting in the collision checking loop being skipped altogether as `for (int i = 8; i < 8; i += 5)`

switching to `collisionEndPadding = trailPositions.Count / 30` sets the value to 0 for nearly all cases i saw (light of dawn averaged a count of ~20 for me, and sundered moon rarely broke 30), but leaving it as is since the projectile collisions didnt seem particularly unusual when testing with the changes

---

included TripleThreat summon for consistency, but it probably doesnt need it (trailMaxLength=500 seems long enough to always allow for collision checking due to creating enough trailPositions). skipped changing Triad's HomingStar projectile since it is a boss (a nice surprise for player if they are lucky enough to avoid damage?) but it probably also doesn't need it anyways given a trailMaxLength of 700

### fix HP not scaling in multiplayer for bosses with `NPC.damage = 0` in `SetDefault()`
- Date: 2025-11-04
- Author: 張
- Commit: `2943f677f`

Description:

terraria skips scaling altogether for NPCs with `damage == 0`, but this can be overridden by setting NeedsExpertScaling

terraria scaling code w/ tmodloader patch looks roughly like
```
    public void ScaleStats(
      int? activePlayersCount,
      GameModeData gameModeData,
      float? strengthOverride)
    {
      if ((!NPCID.Sets.NeedsExpertScaling.IndexInRange<bool>(this.type) || !NPCID.Sets.NeedsExpertScaling[this.type]) && (this.lifeMax <= 5 || this.damage == 0 || (this.friendly || this.townNPC)))
        return;
      ...
      <vanilla scaling logic>
      ...
      ApplyDifficultyAndPlayerScaling() hook
```

### Progression on Thorium compatibility
- Date: 2025-11-23
- Author: Xelvaa
- Commit: `24748f29f`

Description:

_No additional description in commit body._

### Started Thorium compatibility
- Date: 2025-11-22
- Author: Xelvaa
- Commit: `c9b3163a7`

Description:

Also some stuffs for remix map

### Semi-reworked Chaos and Earth Fiend Lich, also other things
- Date: 2025-11-21
- Author: Xelvaa
- Commit: `49090d466`

Description:

- Re-tiered Crescent Moon Sword
- Buffed the HP scaling in SHM (start at 1x instead of 0.7x)
- Removed the obtenable test items
- Removed Cobalt Halberd
- Resprited Boss Rematch Tome
- Finished Ultima Weapon
- Finished Blast
- Removed the pirate invasion in the remix map
- Buffed Mage Shadow
Earth Fiend semi rework is finished I think, but I still need to do more works on Chaos and I will see for tweaks on Death and Seath

### Try this, config option for Soapstones to only take effect when creating new map
- Date: 2025-11-08
- Author: urgiv
- Commit: `dd60253fa`

Description:

_No additional description in commit body._

### Fixed Twins bag and other changes
- Date: 2025-11-06
- Author: Xelvaa
- Commit: `bb1af5172`

Description:

resprited Essence of Terraria

### NonRemixWorldDropCondition
- Date: 2025-11-06
- Author: Xelvaa
- Commit: `1500ef67b`

Description:

_No additional description in commit body._

### Buffed the HP values of most SHM enemies
- Date: 2025-11-06
- Author: Xelvaa
- Commit: `b29e078f6`

Description:

_No additional description in commit body._

### RemixWorldDropCondition
- Date: 2025-11-06
- Author: urgiv
- Commit: `ce7a992bf`

Description:

_No additional description in commit body._

### update zh-Hans
- Date: 2025-11-06
- Author: urgiv
- Commit: `645b31eb3`

Description:

_No additional description in commit body._

### Merge pull request #79 from DreamSea/misc-fixes
- Date: 2025-11-02
- Author: Xelvaa
- Commit: `0a853528f`

Description:

Misc fixes (AncesteralSpiritBag / WyvernMageBag / PermanentLuckPotion / Celestial Lance / Longinus / Ionic Fury)

### BIG COMMIT FOR 0.16.4
- Date: 2025-11-01
- Author: Xelvaa
- Commit: `842282575`

Description:

Almost are in #temporary-changelog !
- notable one are the addition of Ultima Tome, Sundered Moon and Volatile Bazooka (all are wip), rework of forgotten bows, remix map stuff and other !

### enable some projectile spawned projectiles in multiplayer
- Date: 2025-11-01
- Author: 張
- Commit: `f895425f2`

Description:

guessing `Main.netMode != NetmodeID.MultiplayerClient` might be mostly for singleplayer/server owned NPCs (like bosses) spawning projectiles

not sure if the owner check is strictly required in the current case (maybe it is more important for server owned projectiles rather than equipment based?) since it looks like the code only gets run by the owner of the projectile to begin with, but keeping it just in case 🙈

