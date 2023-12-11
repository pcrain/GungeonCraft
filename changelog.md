# Changelog

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
- Fixed (kinda) null dereference randomly preventing the Dragun fight cutscene from triggers, and added some debug output in case anyone is ever able to reproduce it again
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
