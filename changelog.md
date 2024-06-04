# Changelog

## 1.13.1 (2024-06-02)

#### Features and Polish:
- Added 3 new synergies!

#### Balance Changes:
- Prevented Alien Nailgun from spawning Jammed enemies
- Frisbee can now be instantly ridden if used during a dodge roll
- Added MM: Reloading and Blank Checks to list of items made free by Library Cardtridge
- Killing a Jammed enemy with the Holy Water Gun now reduces curse by 0.5 instead of 1
- Alligator now builds charge slightly faster while moving over carpeted floors

#### Bugfixes and Misc:
- Fixed null deref caused by throwing Suncaster while prisms are deployed
- Fixed certain projectiles from Alien Nailgun replicants not dealing damage
- Fixed weird Frisbee-riding offsets on some characters
- Fixed Insurance Policy issue where insuring an item would visually mark all items of the same type as insured
- Fixed several small typos in Ammonomicon descriptions and lore
- Tweaked Camping Supplies' campfire sprite to be a little less washed-out
- Lowered volume for Scotsman's shooting and detonation sounds
- Tweaked reticle rendering code for Gunbrella, Scotsman, and Maestro to make things look a little nicer

## 1.13.0 (2024-05-27)

#### Features and Polish:
- Added Frisbee, Maestro, and Starmageddon
	- Maestro sprite thanks to Dallan!

#### Balance Changes:
- Changed Pincushion's class from BEAM to FULLAUTO
- Made Suncaster's prisms bounce off walls 

#### Bugfixes and Misc:
- Made logic for cleaning up temporary startup patches execute even if startup horrendously fails for some reason, preventing other mods from breaking
- Added failsafe to prevent Suncaster prisms from getting stuck inside walls or other objects
- Prevented Camping Supplies from recalculating stats every single frame while moving
- Fixed Holy Water gun registering duplicate enemy events every time it was dropped and picked up
- Fixed Macchi Auto Ammonomicon icon to match in-game sprite
- Fixed Camping Supplies causing lag by trying to invoke a non-existent method
- Fixed accidental removal of stunning behavior from Platinum Star
- Fixed sprites for 50-casing pickups being replaced by Scavenging Arms' small ammo pickup sprites
- Fixed mastery ritual not working in certain rooms that it should've worked in
- Fixed item tips breakage for items with colons and apostrophes in their names
- Slightly sped up Alligator's reload animation so it actually finishes playing
- Removed occlusion from Gunbrella projectiles to increase their visibility and make them look nicer
- Tweaked Chamber Jammer's description for clarity

## 1.12.0 (2024-05-05)

#### Features and Polish:
- Added Macchi Auto, Nycterian, Scavenging Arms, Armor Piercing Rounds, and MM: Reloading
	- Armor Piercing Rounds and MM: Reloading sprites thanks to Dallan!
- Added gun mastery system
- Added masteries for Grandmaster, Chekhov's Gun, Pincushion, Platinum Star, and Natascha
- Added 18 new hats
- Bulletbot Implant, Bionic Finger, and Gorgun's Eye can now spawn in Handy's shop from [Knife to a Gunfight](https://thunderstore.io/c/enter-the-gungeon/p/Skilotar/Knife_to_a_Gunfight/)!

#### Bugfixes and Misc:
- Fixed issue with knight pieces fired from Grandmaster getting stuck in infinite loops when no enemies are around
- Fixed Aimu Hakurei being able to build graze off of co-op partner's bullets
- Fixed null deref in Safety Gloves due to trying to play VFX above despawned enemies
- Fixed visual bug with misaligned muzzle flash for Holy Water Gun 
- Fixed bug (for the second time) where loading the secret floor after returning to the Breach would cause an infinite loading screen
- Lowered volume on Natascha spinup sound

## 1.11.5 (2024-04-20)

#### Bugfixes:
- Fixed null deref due to Plot Armor checking whether a non-existent room is a boss foyer
- Fixed null deref in Ki Blast when trying to apply knockback to enemies without KnockbackDoers
- Fixed null deref in Ki Blast when used against the Dragun
- Fixed not being able to play the Glockarina when using a controller
- Prevented Quarter Pounder from creating duplicate gold sprites for enemy types that have already been transmuted to gold

#### Balance Changes:
- Buffed Glocakrina's base damage from 4 to 7.5
- Buffed Magunet's debris damage from 1 to 2
- Buffed Magunet's corpse damage from 10 to 30
- Buffed Jugglernaut's fire rate from 1.0 seconds to 0.4 seconds
- Increased Jugglernaut's ammo from 150 to 240
- Pistol Whip now only passively increases curse by 2 instead of 3
- Enemies transmuted with Quarter Pounder now drop an extra casing upon death
- Aimu Hakurei can now build graze even when it's not the active gun
- Doubled Sub Machine Gun's charm chance, making enemies at 50% health or less 100% susceptible to charm
- Changed Scotsman's quality from A to B
- Changed Ammo Conservation Manual's quality from B to C
- Changed Dead Ringer's quality from A to B
- Changed Gyroscope's quality from A to B
- Changed Amazon Primer's quality from A to B
- Changed Chest Scanner's quality from B to C
- Removed Insurance Policy from normal item pool (so it can only be purchased at Insurance Shop)

## 1.11.4 (2024-04-12)

- Fixed null deref due to Gorgun Eye scanning for enemies without an active room
- Fixed null deref due to Seltzer Pelter projectiles trying to emit seltzer streams from expired projectiles
- Fixed null deref due to Insurance Policy trying to reuse previously-destroyed VFX on insured items
- Fixed null deref due to Aimu Hakurei (and potentially Alyx) trying to continue running a coroutine after being destroyed
- Fixed ghost sprites of items remaining on screen after activating Kaliber's Justice 

## 1.11.3 (2024-04-11)

#### Bugfixes:
- Fixed invalid operation exception when trying to apply Soul Kaliber's soul link status effect to enemies
- Fixed Magunet retaining it's charge when switched or dropped while charging
- Fixed Magunet charge particles getting stuck in the air when switched or dropped while charging
- Fixed softlock caused by getting hit after dropping Warrior's Gi and starting a new floor
- Fixed Ticonderogun being able to hit invulnerable enemies
- Fixed insurance chests spawning past the first floor when continuing a saved run
- Fixed null deref in Borrowed Time due to checking for whether non-existing rooms are combat rooms
- Fixed random invalid operation exceptions when trying to use Borrowed Time

#### Balance Changes:
- Added Ballot to list of items purchasable for free by Library Cardtridge
- Chicks spawned by Hatchling Gun no longer deal obscene collision damage to enemies
- Seltzer water from Seltzer Pelter's projectiles can now be electrified like normal water 
- Switched Bouncer from C to D quality since it's ridiculously hard to use effectively

#### Misc.
- Renamed Curator's Badge to Custodian's Badge, since I apparently forgot the difference between the two
- Updated description of Custodian's Badge to mention removal from inventory after letting too many breakables break
- Added directional sprites for a few hats
- Updated Thunderstore Icon, huge thanks to Lynceus for the new one! :D <3

## 1.11.2 (2024-04-07)

- Added 11 hats! (sprites thanks to Dallan)
- Migrated all cAPI code to Alexandria and updated required Alexandria version to 0.4.0
- Fixed null deref in Comfy Slippers due to trying to get position of nonexistent owner when updating on the ground
- Fixed null deref in Emergency Siren due to trying to check the user's current room without an active user
- Fixed Vladimir impale point extending too far beyond the end of the weapon
- Fixed wonky collision detection on shopkeepers
- Made Pistol Whip quieter when used with Scattershot
- Made Lightwing projectiles no longer restore ammo if fired for free (e.g., with Scattershot)

## 1.11.1 (2024-04-04)

- Fixed hang when trying to access the hat room after starting a run and returning to the Breach 

## 1.11.0 (2024-04-04)

- Added 56 cosmetic hats! :D
	- Hats are accessible via a new hat room near Winchester in the Breach
	- Hat sprites thanks to Dallan!
	- Hat room pedestal, entrance, and exit sprites thanks to Lynceus!
- Fixed Gorgun Eye constantly making noise every frame

## 1.10.4 (2024-03-27)

- Fixed Subtractor Beam being completely broken and causing numerous null reference exceptions when killing enemies with health values visible
- Fixed Hand Cannon being able to stun enemies that haven't spawned in yet
- Fixed Hand Cannon dealing 0 damage to enemies it can't stun
- Fixed Hand Cannon dealing 0 damage to enemies it can't knock back
- Fixed Quarter Pounder's midas effect applying to bosses and minibosses, potentially softlocking the game (e.g., against the Dragun)
- Fixed Omnidirectional Laser reticle and renderer completely breaking when loading a new floor
- Fixed Omnidirectional Laser renderer breaking when dropped and picked back up

## 1.10.3 (2024-03-24)

#### Bugfixes:
- Fixed several guns being able to target enemies internally marked as "not worth shooting at"
- Fixed idle animation speed for Ki Blast, Deadline, Schrodinger's Gat, Racket Launcher, Subtractor Beam, Wavefront, and Alien Nailgun not being set up correctly after a recent update
- Fixed Grandmaster, Scotsman, Bouncer, B. B. Gun, Seltzer Pelter, Carpet Bomber, Lightwing, and Blackjack projectiles freaking out when used with Helix Bullets
- Limited length of Aimu Hakurei, Subtractor Beam, and Omnidirectional Laser projectile trails after bouncing
- Fixed Carpet Bomber, Lightwing, and Blackjack projectiles freaking out when used with Orbital Bullets
- Fixed Pistol Whip, Jugglernaut, and Deadline projectiles not respecting aim deviation from items like Backup Gun or Scattershot
- Fixed King's Law and Iron Maid not launching bullets when player has instant reloads
- Blacklisted Racket Launcher and Seltzer Pelter projectiles from being affected by Orbital Bullets
- Fixed Scotsman and Ki Blast projectiles ignoring reticle when used with Remote Bullets
- Fixed Gunbrella projectiles being non-functional when fired using Duct Tape or Ring of Triggers
- Fixed Gunbrella firing Backup Gun projectiles forwards
- Silenced Gunbrella debug log spam when duct taped to another gun
- Fixed Scotsman projectiles all being sent in the same direction with Backup Gun
- Fixed Scotsman projectiles despawning immediately when not spawned with Scotsman (e.g., with Duct Tape or Chance Bullets)
- Fixed Ki Blast firing Backup Gun and Ring of Triggers projectiles forwards
- Fixed Ki Blast projectiles ignoring effects of Helix Bullets
- Fixed deployed Chekhov's Gun sightlines following the cursor when the player has Remote Bullets
- Fixed deployed Chekhov's Gun sprites appearing far away from the player's gun sprites with certain rotations
- Fixed null dereference caused by spawning Iron Maid projectiles from other guns (e.g., with Duct Tape or Chance Bullets)
- Fixed null dereference when dropping Iron Maid with active projectiles
- Fixed Natascha firing every single frame with certain projectile-duplicating items such as Scattershot, Backup Gun, and Helix Bullets
- Fixed extremely high fire rate for guns duct-taped to Natascha
- Fixed B. B. Gun being able to launch glitchy uncharged projectiles
- Fixed freebie projectiles from B. B. Gun being able to restore ammo
- Fixed Deadline projectiles not working properly when not spawned from Deadline (e.g., with Duct Tape or Chance Bullets)
- Fixed null dereference when K.A.L.I. projectiles are spawned by Ring of Triggers
- Fixed Grandmaster projectiles continuously bouncing against walls with Bouncy Bullets
- Fixed idle animation for Crapshooter playing way too fast
- Fixed Omnidirectional Laser secondary projectiles (e.g., from Backup Gun and Scattershot) always firing towards the reticle and overlapping the original projectile
- Fixed Pincushion firing Backup Gun projectiles forwards
- Fixed null dereference in Schrodinger's Gat due to incorrectly referencing projectiles belonging to despawned enemies
- Fixed chicks spawned from the Hatchling Gun not being marked as harmless enemies and being targeted by things they shouldn't be
- Fixed Subtractor Beam not displaying health for neutral enemies like Chance Kin or Key Bullet Kin
- Fixed Blamethrower being able to scapegoat enemies outside of the current room
- Fixed Wavefront projectiles noisily colliding with player companions
- Fixed Holy Water Gun having an invisible beam when used with Orbital Bullets

#### Balance Changes:
- Reduced Holy Water Gun ammo from 500 to 100
- Reduced Holy Water Gun knockback from 50 to 15
- Made Holy Water Gun respect bullet damage modifiers (previously dealt fixed damage)
- Tweaked Natascha spin up mechanics, spin up rate, and max spin up speed
- Made Gunbrella projectiles target a random enemy when no Gunbrella reticle is present
- Gave Bouncer projectiles stacking bounces with Bouncy Bullets
- Gave Deadline hitscan projectiles
- Gave Pistol Whip infinite ammo for its pistol projectile (whip itself already had infinite ammo)
- Made Pistol Whip take spread into account instead of having perfect accuracy

## 1.10.2 (2024-03-22)

#### Bugfixes:
- Fixed Insurance Shop sometimes spawning on the first floor even without an S or A tier item in the player's inventory
- Fixed Holy Water Gun's beam hitbox not extending all the way to its point of collision
- Fixed Holy Water Gun spamming the debug log due to improperly-set colliders
- Fixed Blank Checks inconsistently activating before / after triggering blanks (sometimes using one, sometimes not)
	- Blank Checks now consistently activates prior to triggering blanks, so using a blank with none in your inventory will always give 3 blanks and use 1 immediately
- Fixed Blank Checks spamming the debug log whenever attempting to use a blank
- Fixed Adrenaline Shot still being able to activate after it's been dropped
- Fixed King's Law breaking if it somehow manages to spawn hundreds of bullets in stasis

#### Polish:
- Bulletbot Implant, Carpet Bomber, King's Law, Ammo Conservation Manual, and Stunt Helmet can now appear in various vanilla / modded sub-shops
- Cuppajoe cooldown bar now shows caffeine time remaining while ticking down and crash time remaining while ticking up
- Added sound effect for tanking damage while Adrenaline Shot is active
- Gave Paintball Cannon a slightly larger projectile
- Tweaked Insurance Shopkeeper's animations

## 1.10.1 (2024-03-20)

- Fixed startup crash due to Omnidirectional Laser trying to read non-existent attach points

## 1.10.0 (2024-03-20)

#### Features and Polish:
- Added Omnidirectional Laser, R.C. Launcher, Breegull, Sub Machine Gun, Reflex Ammolet, Chest Scanner, and Bulletbot Implant
	- Omnidirectional Laser, Reflex Ammolet, Chest Scanner, and Bulletbot Implant sprites thanks to Dallan!
	- Omnidirectional Laser concept thanks to Duudle!

#### Bugfixes and Misc:
- Fixed issue with faulty sprite references for Alligator, Aimu Hakurei, Blamethrower, and [REDACTED] projectile impact VFX, causing a large white flame to appear instead
	- Added debug code to warn about faulty VFX sprite reuse to prevent similar bugs from occurring in the future
- Fixed misplaced projectile when firing Hand Cannon
- Fixed bad hook causing failures when trying to give certain enemies guns outside of Hecked Mode
- Cleaned up sound handling code for guns / projectiles to make adjusting sounds slightly easier in the future
- Substantially lowered Pincushion's firing sound

## 1.9.1 (2024-03-10)

- Fixed numerous items and guns causing issues when removed directly from the inventory (e.g., when using Gun Munchers)
- Fixed Blamethrower scapegoat effect persisting after dropping the gun

## 1.9.0 (2024-03-10)

#### Features and Polish:
- Added K.A.L.I., Alien Nailgun, Reserve Ammolet, and Gun Synthesizer
	- Alien Nailgun sprites thanks to Lynceus!
	- Reserve Ammolet and Gun Synthesizer sprites thanks to Dallan!
- Added Insurance Shop
	- Spawns if an A or S tier item is in your inventory at the beginning of a floor
	- Sells Insurance Policy items that let you carry over an item to your next run
	- Room and NPC design thanks to Lynceus!

#### Bugfixes:
- Fixed a null dereference when Alligator's projectiles hit enemies without HealthHavers
- Fixed certain guns in Hecked mode causing null reference exceptions when wielded by enemies
- Fixed some graphical glitches with Insurance Policy sprites
- Fixed some graphical glitches with various projectile trails

#### Misc:
- Dramatically improved startup time due to better sprite loading techniques
- Reduced RAM and VRAM usage by packing all graphics into a single texture
- Silenced Seltzer Pelter's projectiles after a few bounces so they don't cause a racket if they get stuck
- Made Deadline's fire and reload sounds quieter

## 1.8.2 (2024-02-21)
- Fixed accidentally making all bosses jammed whoops D:

## 1.8.1 (2024-02-20)
- Fixed null dereference causing Gyroscope and Drifter's Headgear to break after boss fights
- Fixed some pickup bounding box weirdness on Vladimir, Blamethrower, and Suncaster
- Added ammo clip sprites for Suncaster and Blamethrower thanks to Lynceus!
- Added some missing sprite credits

## 1.8.0 (2024-02-11)

#### Features and Polish:
- Added Vladimir, Blamethrower, and Suncaster
	- Vladimir sprite thanks to Dallan!

#### Bugfixes:
- Fixed another rare edge case where Astral Projector could get the player stuck in walls
- Fixed Gyroscope and Drifter's Headgear preventing the player from dodge rolling in the Aimless Void
- Fixed Gyroscope and Drifter's Headgear polluting the debug log with error messages when used
- Fixed Gorgun's Eye and Hand Cannon attempting to stun unstunnable enemies
- Fixed several projectiles ignoring the frame rate during physics calculations
- Fixed several visual effects ignoring the frame rate during physics calculations
- Fixed a few typos in item descriptions

#### Misc:
- Added instructions for enabling Hecked Mode to readme 
- Made companion shopkeeper no longer face towards player since the shading looked weird

## 1.7.0 (2024-01-20)

#### Features and Polish:
- Added Hecked Mode O_O
	- Enable it in the Mod Config pause menu (Options -> Mod Config -> GungeonCraft)
- Added Scotsman, Chekhov's Gun, Ring of Defenestration, Ammo Conservation Manual, and Stop Sign
	- Ring of Defenestration, Ammo Conservation Manual, and Stop Sign sprites thanks to Dallan!
- Added several new sprites thanks to Lynceus!
	- Brand new item sprites for John's Wick, Emergency Siren, and Amazon Primer
	- Polished item sprites for Cozy Camper and Gyroscope
	- Polished projectile sprites for Carpet Bomber
	- Custom ammo clip sprites for Wavefront
	
#### Balance Changes:
- Library Cardtridge now makes "books and paper-based items" free at shops, which now includes Origuni

#### Bugfixes and Misc:
- [Gunfig](https://enter-the-gungeon.thunderstore.io/package/CaptainPretzel/Gunfig/) is now a required dependency of GungeonCraft 
- Toned down volume of Pistol Whip sound effects
- Clarified in Emergency Siren's description that it opens sealed combat doors rather than actual locked doors
- Fixed potential null dereference when Magunet and Vacuum Cleaner try to grab inactive debris
- Fixed backwards sprite for Pincushion in Ammonomicon
- Fixed a few typos in various item descriptions in Ammonomicon
- Added some missing sprite credits in earlier Changelog entries

## 1.6.1 (2024-01-07)

#### Features and Polish:
- Overhauled sprites for companion shopkeeper NPC, thanks to Lynceus!
- Added critters to the companion shop and changed the room to be more outdoors-y

#### Balance Changes:
- Companion shop discount reduced from 50% to 30% (i.e., companions now cost 70% of full price)
- Reworked Natascha
	- Reloading now toggles whether the gun remains spun up when not firing, maintaining its increased fire rate and decreased movement speed
	- Projectiles now apply a slow effect on hit
	- Decreased max ammo from 2500 to 1500
	- Gun class changed from BEAM (why???) to FULLAUTO 
	- Added spin up and spin down sounds
	- Added casing sprites
- Reworked Bouncer
	- Projectiles now bounce 3 times instead of once, exploding only after bouncing 3 times
	- Projectile damage decreased by 40% to compensate for increased longevity
	- Projectile damage now scales with player's damage stat
	- Reload time increased from 0.8 seconds to 1.3 seconds
	- Clarified in Ammonomicon that projectiles' damage scales with speed (which was always true, but never mentioned)
	- Fixed bug where projectile acceleration and bounce time were tied to the frame rate (yikes)
	- Fixed projectiles sometimes getting stuck in walls
	- Added brand new reload animation and sounds
	- Tweaked firing animation and firing sound volume
- Reworked Tranquilizer
	- Hitting an enemy with multiple projectiles now speeds up the tranquilization process
	- Projectiles no longer deal any damage, only knockback
	- Added new projectile impact effects
	- Added new overhead effects for asleep enemies
	- Added new sound cue for when an enemy is fully tranquilized

#### Bugfixes and Misc:
- Fixed Blasphemy reload sound playing when reloading some guns
- Fixed visual bug during glow phase of Iron Maid causing projectiles to dim before glowing
- Fixed potential null dereference in Jugglernaut weapon panel sprite hook
- Fixed potential null dereference with Gyroscope and Drifter's Headgear custom dodge roll hook

## 1.6.0 (2023-12-30)

#### Features and Polish:
- Added Glockarina, Magunet, Wavefront, Cuppajoe, Safety Gloves, and Drab Outfit
- Added custom ammo clips for Alligator, Carpet Bomber, Crapshooter, Glockarina, King's Law, Lightwing, Subtractor Beam, and Uppskeruvel, thanks to Lynceus!
- Overhauled sprites for barter shopkeeper NPC thanks again to Lynceus!

#### Balance Changes:
- Nerfed Itemfinder: 
	+ reduced max items per floor from 6 (should have been 5 but I'm bad at programming) to 4
	+ reduced chances of finding more than 1 item per floor
- Nerfed Plot Armor: 
	+ reverted 1.2.1 changes so it once again gives at least 1 (not 2) armor and brings the player up to a minimum of 3 (not 4) armor
	+ kept change to A quality so it should still be more common than pre-1.2.1 Plot Armor
	
#### Bugfixes:
- Fixed barter shop not having items if first run was not started through quickstart
- Fixed barter shop and companion shop not spawning at all on the first run if the run was not started through quickstart
- Fixed cross-mod shop item injection not working if first run was not started through quickstart
- Fixed some modded guns with large idle animations dropping in weird places when spawned from chests
- Fixed null dereference in Library Cardtridge caused by scanning for items in nonexistent shops
- Fixed Iron Maid's offset in the weapon display panel

#### Misc:
- Made Adrenaline Shot use true UI sprites (rather than buggy fake UI sprites) for adrenaline hearts
- Made Mod actually properly shows up as "GungeonCraft" in the mod's list
- Removed debug output from Missiletoe that was polluting the console

## 1.5.1 (2023-12-20)
- Made Companion Shop items non-stealable
- Forced Barter Shop to go out of stock on successful steal
- Added steal dialogue to Barter Shop
- Added debug output to track down Barter Shop not having any items sometimes
- Fixed null dereference in Ki Blast caused by looking up sprites for non-existent enemies
- Fixed null dereference in Iron Maid caused by trying to update the Gun while the level is loading
- Fixed null dereference in Subtractor Beam caused by trying to update text to the position of non-existent enemies
- Fixed null dereference in Drifter's Headgear and Gyroscope from not checking if they actually have an owner when destroyed
- Fixed null dereference in Paintball Cannon during Dragun fight due to trying to color a non-existent AIActor

## 1.5.0 (2023-12-10)

#### Features and Polish:
- Added Companion Shop
- Added Bartering Shop
- Added Uppskeruvel

#### Bugfixes:
- Fixed vanilla bug where quick restarting wouldn't properly reset the chances of spawning "Once Per Run" rooms (e.g., specialty shops)
- Fixed vanilla bug in RNG where randomly selecting the first item from a list of two items would always return the first item
- Fixed guns having extremely large interaction ranges in shops
- Fixed beam sprite from Aimu Hakurei's max level not rendering 
- Fixed buggy ammo display caused by repeatedly switching between Aimu Hakurei and an infinite ammo gun 
- Fixed null dereference error caused by trying to use Borrowed Time in an unregistered room
- Fixed null dereference error caused by Alligator sparks trying to move towards a non-existent enemy 
- Fixed items not being added to modded shops that spawned on the first floor

#### Misc:
- Insurance Policy can no longer be sold to shops
- Optimized startup loading time to be about 1.5x faster
- Updated Alexandria to 0.3.26 for faster UI sprite loading (thanks SomeBunny!)

## 1.4.0 (2023-12-01)

#### Features and Polish:
- Added King's Law, Pincushion, Crapshooter, and Carpet Bomber
- Implemented proper mid-game saving behavior for Missiletoe, Alyx, Curator's Badge, Wedding Ring, Borrowed Time, and Warrior's Gi

#### Balance Changes:
- Reaching the floor's exit while Adrenaline Shot is active now automatically cancels the effect and brings you to half a heart

#### Bugfixes:
- Fixed vanilla bug preventing duct-taped guns from serializing properly
- Fixed issue with guns having extremely large pickup ranges
- Fixed issue with guns hovering far above / below reward pedestals if spawned

## 1.3.0 (2023-11-22)
- Added Subtractor Beam, Alligator, Lightwing, Stunt Helmet, Comfy Slippers, and Chamber Jammer
	- Stunt Helmet and Chamber Jammer sprites thanks to Dallan!
- Added muzzle effects for all guns!
- Added custom projectile and ammo clip to Platinum Star, thanks to Lynceus!
- Added hand animations for Ki Blast
- Added proper reload sound for Deadline
- Improved gun sprite for Alyx
- Improved projectile sprites for Soul Kaliber and Platinum Star
- Improved collision detection substantially for Deadline lasers

## 1.2.1 (2023-11-13)
- Buffed Plot Armor: changed from S to A quality, now always gives at least 2 armor and brings the player up to a minimum of 4 armor
- Added a sound cue to Vacuum Cleaner for when ammo is successfully restored
- Made blue bullets for boss' blue attack more...blue (they were purple-ish previously)
- Polished sprites for Itemfinder and gave it a small animation while active
- Fixed vanilla bug preventing second player in co-op turbo mode from receiving increased speed until their stats were changed at least once
- Fixed Vacuum Cleaner not being able to vacuum up some projectiles that were converted to debris
- Fixed occasional animation error when picking up Jugglernaut
- Fixed occasional null dereference error when Alyx tries to spawn poison goop
- Fixed boss floor music not looping cleanly

## 1.2.0 (2023-11-13)
- Added Platinum Star, Pistol Whip, Jugglernaut, and Adrenaline Shot
- Added synergy support for ItemTips in preparation for future synergies

## 1.1.3 (2023-11-06)
- Fixed chicks spawned from Hatchling Gun causing errors every frame of their existence D:
- Fixed items unwrapped from Missiletoe not being pickup-able
- Added more custom ammo clip sprites and Missiletoe projectile variants thanks to Lynceus!
- Nerfed Gunbrella ammo and Blackjack clip size slightly 

## 1.1.2 (2023-11-06)
- Fixed (kinda) null dereference randomly preventing the Dragun fight cutscene from triggering, and added some debug output in case anyone is ever able to reproduce it again
- Fixed Aimu Hakurei being able to graze bullets that have already despawned
- Fixed a few edge cases for Astral Projector that could get you stuck in walls when pausing, opening the Ammonomicon, or opening a debug console

## 1.1.1 (2023-11-05)
- New custom projectile and ammo clip sprites for several guns thanks to Lynceus :D
- Added full support for the [ItemTips](https://enter-the-gungeon.thunderstore.io/package/Glorfindel/ItemTips/?utm_source=discord) mod for all GungeonCraft items
- Updated to ModTheGungeon API 1.7.0 for faster load times (thanks SpecialAPI!)
- Fixed an issue where a few console commands from other mods would be disabled when GungeonCraft is loaded
- Fixed Tranquilizer being able to stun enemies immune to stun
- Fixed Thunderstore preview icon image link in Credits page

## 1.1.0 (2023-11-03)

#### Features and Polish:
- Added Aimu Hakurei, Seltzer Pelter, Missiletoe, Bubble Wand, Insurance Policy, and Ice Cream
	- Insurance Policy and Missiletoe sprites thanks to Dallan!
- Items can now be found in specialty shops (subshops), including support for modded shops (Planetside of Gunymede & Once More Into the Breach)
- Added a few new sprites for Bullet Kin (they love ice cream :>)
- All of the mod's guns now spawn with their idle animations playing by default (before, they would be stuck on the first frame)
- Use a new, faster exponent approximation in a few places to reduce lag

#### Balance Changes:
- Ticonderogun's damage now scales inversely with the number of enemies encircled rather than inversely with circle size, resulting in higher damage on average
- Dead Ringer now has a short grace period after activating to make it less likely you will immediately lose your cloak
- Halved max ammo of Schrodinger's Gat
- [REDACTED]'s active item effect can now be triggered by either the active item button or the fire button

#### Bugfixes:
- Fixed Alyx magically replenishing all of its ammo when dropped and picked up again
- Fixed an oversight where the contents of all chests could be replaced with a singular specific item rather than drawing from their normal loot table
- Fixed incorrect impact effects playing on some projectiles
- Updated to Alexandria 0.3.24 to fix co-op blank issue (thanks SomeBunny!)

## 1.0.4 (2023-10-21)

- Fix custom dodge rolls not properly putting out fires
- Fix bosses killed with Quarter Pounder not spawning reward pedestals
- Make gold statues created with Quarter Pounder pushable 
- Fix Z-axis rendering issues with statues created with Quarter Pounder
- Possibly fix issue with gold sprite from Kill Pillars killed with Quarter Pounder appearing randomly on the next floor
- Add cooldown to Soul Kaliber vfx / sfx so per-frame damage doesn't cause lag and noise
- Adjust audio levels for Soul Kaliber
- Clean up outline on Dead Ringer sprite
- Correct typo in Alyx's description

## 1.0.3 (2023-10-21)

- Fix custom floor failing to load after returning to the Breach (thanks SomeBunny!)

## 1.0.2 (2023-10-20)

- Remove unnecessary dependency on Kyle's Custom Rooms

## 1.0.1 (2023-10-20)

- Fix Thunderstore build manifest
- Fix Dead Ringer crash
- Fix null dereference in Update() of several items

## 1.0.0 (2023-10-20)
	
- Initial Release! :D
