using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NevernamedsItems
{
    class DeconstructGun
    {
        public static void Init()
        {
            ETGModConsole.Commands.GetGroup("nn").AddUnit("deconstructgun", delegate (string[] args)
            {
                bool deconstructVFX = false;
                bool deconstructProjectileComponents = false;
                if (args != null && args.Length > 0 && args[0] != null)
                {
                    if (!string.IsNullOrEmpty(args[0]))
                    {
                        if (args[0] == "vfx") deconstructVFX = true;
                        if (args[0] == "projcomp") deconstructProjectileComponents = true;
                    }
                }
                if (GameManager.Instance == null)
                {
                    ETGModConsole.Log("Somehow the fucking game manager was null lol rip get fucked mate.");
                    return;
                }
                if (GameManager.Instance.PrimaryPlayer == null)
                {
                    ETGModConsole.Log("<size=100><color=#ff0000ff>ERROR: There is no player 1 to check for a gun.</color></size>", false);
                    return;
                }
                if (GameManager.Instance.PrimaryPlayer.CurrentGun == null)
                {
                    ETGModConsole.Log("<size=100><color=#ff0000ff>ERROR: Player 1 is not holding a gun to deconstruct.</color></size>", false);
                    return;
                }
                Gun target = GameManager.Instance.PrimaryPlayer.CurrentGun;

                ETGModConsole.Log("<color=#09b022>-------------------------------------</color>");
                ETGModConsole.Log("<color=#09b022>Base Gun Stats:</color>");
                ETGModConsole.Log("<color=#ff0000ff>Display Name: </color>" + target.DisplayName); ;
                ETGModConsole.Log("<color=#ff0000ff>Object Name: </color>" + target.name);
                ETGModConsole.Log("<color=#ff0000ff>Internal Name:</color>" + target.gunName);
                ETGModConsole.Log("<color=#ff0000ff>ID: </color>" + target.PickupObjectId);
                ETGModConsole.Log("<color=#ff0000ff>Class: </color>" + target.gunClass);
                ETGModConsole.Log("<color=#09b022>Numerical Base Stats:</color>");
                ETGModConsole.Log("<color=#ff0000ff>Infinite Ammo: </color>" + target.InfiniteAmmo);
                ETGModConsole.Log("<color=#ff0000ff>Ammo Max: </color>" + target.GetBaseMaxAmmo());
                ETGModConsole.Log("<color=#ff0000ff>Reload Time: </color>" + target.reloadTime);
                if (deconstructVFX)
                {
                    ETGModConsole.Log("<color=#09b022>Muzzle Flash Effects:</color>");
                    if (target.muzzleFlashEffects != null)
                    {
                        ETGModConsole.Log("<color=#ff0000ff>    Type: </color>" + target.muzzleFlashEffects.type);
                        if (target.muzzleFlashEffects.effects != null && target.muzzleFlashEffects.effects.Length > 0)
                        {
                            int index = 0;
                            foreach (VFXComplex vfxcomplex in target.muzzleFlashEffects.effects)
                            {
                                ETGModConsole.Log($"<color=#ff0000ff>    VFXComplex  [{index}]</color>");

                                index++;
                            }
                        }
                        else
                        {
                            ETGModConsole.Log("<color=#ff0000ff>    Gun's Muzzle Flash Effects contain no list of VFXComplexes? </color>");
                        }
                    }
                    else
                    {
                        ETGModConsole.Log("<color=#ff0000ff>    Gun has no Muzzle Flash Effects.</color>" + target.reloadTime);
                    }
                }
                ETGModConsole.Log("<color=#ff0000ff>Components:</color>");
                foreach (Component component in target.GetComponents<Component>())
                {
                    ETGModConsole.Log(component.GetType().ToString());
                    if (component is DualWieldSynergyProcessor)
                    {
                        ETGModConsole.Log("<color=#ff0000ff>    Req Synergy: </color>" + (component as DualWieldSynergyProcessor).SynergyToCheck);
                        ETGModConsole.Log("<color=#ff0000ff>    Other Gun: </color>" + PickupObjectDatabase.GetById((component as DualWieldSynergyProcessor).PartnerGunID).DisplayName + " (" + (component as DualWieldSynergyProcessor).PartnerGunID + ")");
                    }
                    else if (component is FireOnReloadSynergyProcessor)
                    {
                        ETGModConsole.Log("<color=#ff0000ff>    Req Synergy: </color>" + (component as FireOnReloadSynergyProcessor).SynergyToCheck);
                        ETGModConsole.Log("<color=#ff0000ff>    Use Gun Proj: </color>" + (component as FireOnReloadSynergyProcessor).DirectedBurstSettings.ProjectileInterface.UseCurrentGunProjectile);
                        if ((component as FireOnReloadSynergyProcessor).DirectedBurstSettings.ProjectileInterface.SpecifiedProjectile != null)
                        {
                            ETGModConsole.Log("<color=#ff0000ff>    Proj Name: </color>" + (component as FireOnReloadSynergyProcessor).DirectedBurstSettings.ProjectileInterface.SpecifiedProjectile.name);
                            ETGModConsole.Log("<color=#ff0000ff>    Proj Damage: </color>" + (component as FireOnReloadSynergyProcessor).DirectedBurstSettings.ProjectileInterface.SpecifiedProjectile.baseData.damage);
                            ETGModConsole.Log("<color=#ff0000ff>    Proj Speed: </color>" + (component as FireOnReloadSynergyProcessor).DirectedBurstSettings.ProjectileInterface.SpecifiedProjectile.baseData.speed);
                            ETGModConsole.Log("<color=#ff0000ff>    Proj Range: </color>" + (component as FireOnReloadSynergyProcessor).DirectedBurstSettings.ProjectileInterface.SpecifiedProjectile.baseData.range);
                            ETGModConsole.Log("<color=#ff0000ff>    Proj Knockback: </color>" + (component as FireOnReloadSynergyProcessor).DirectedBurstSettings.ProjectileInterface.SpecifiedProjectile.baseData.force);
                            ETGModConsole.Log("<color=#ff0000ff>    Proj BossDMGMult: </color>" + (component as FireOnReloadSynergyProcessor).DirectedBurstSettings.ProjectileInterface.SpecifiedProjectile.BossDamageMultiplier);
                            ETGModConsole.Log("<color=#ff0000ff>    Proj DMGTypes: </color>" + (component as FireOnReloadSynergyProcessor).DirectedBurstSettings.ProjectileInterface.SpecifiedProjectile.damageTypes);
                        }
                    }
                    if (component is TransformGunSynergyProcessor)
                    {
                        ETGModConsole.Log("<color=#ff0000ff>    Req Synergy: </color>" + (component as TransformGunSynergyProcessor).SynergyToCheck);
                        ETGModConsole.Log("<color=#ff0000ff>    Transform Target: </color>" + PickupObjectDatabase.GetById((component as TransformGunSynergyProcessor).SynergyGunId).DisplayName + " (" + (component as TransformGunSynergyProcessor).SynergyGunId + ")");
                    }
                }
                ETGModConsole.Log("<color=#09b022>Volleys and Modules:</color>");
                int iteratedVolleyModules = 0;
                if (target.RawSourceVolley != null)
                {
                    if (target.RawSourceVolley.projectiles != null)
                    {
                        foreach (ProjectileModule module in target.RawSourceVolley.projectiles)
                        {
                            ETGModConsole.Log("<color=#09b022>Volley Module: </color>" + iteratedVolleyModules);
                            DeconstructProjModule(module, deconstructVFX, deconstructProjectileComponents, "This");
                            iteratedVolleyModules++;
                        }
                    }
                    else { ETGModConsole.Log("<color=#ff0000ff>Gun has a volley, but no ProjectileModules within it.</color>"); }
                }
                else { ETGModConsole.Log("<color=#ff0000ff>Gun has no volley.</color>"); }

                if (target.DefaultModule != null)
                {
                    ProjectileModule module = target.DefaultModule;
                    ETGModConsole.Log("<color=#09b022>Default Module: </color>");
                    DeconstructProjModule(module, deconstructVFX, deconstructProjectileComponents, "Default");
                }
                else { ETGModConsole.Log("<color=#ff0000ff>Gun has no Default Module.</color>"); }

                if (target.modifiedFinalVolley != null)
                {
                    int iteratedFinalModules = 0;
                    foreach (ProjectileModule module in target.modifiedFinalVolley.projectiles)
                    {
                        ETGModConsole.Log("<color=#09b022>Modified Final Volley Module: </color>" + iteratedFinalModules);
                        DeconstructProjModule(module, deconstructVFX, deconstructProjectileComponents, "This Final Volley");
                        iteratedFinalModules++;
                    }                 
                }
                else { ETGModConsole.Log("<color=#ff0000ff>Gun has no Modified Final Volleys.</color>"); }


            }, DeconstructGun.deconstructAutocomplete);
        }
        protected static AutocompletionSettings deconstructAutocomplete = new AutocompletionSettings(delegate (string input)
        {
            List<string> list = new List<string>() { "projcomp", "vfx" };
            return list.ToArray();
        });
        public static void DeconstructProjModule(ProjectileModule module, bool deconstructVFX, bool deconstructProjectileComponents, string ModName = "Unspecified")
        {
            ETGModConsole.Log("<color=#ff0000ff>    Module Stats:</color>");
            ETGModConsole.Log("<color=#ff0000ff>    ShootStyle: </color>" + module.shootStyle);
            ETGModConsole.Log("<color=#ff0000ff>    Sequence: </color>" + module.sequenceStyle);
            ETGModConsole.Log("<color=#ff0000ff>    Fire Delay: </color>" + module.cooldownTime);
            ETGModConsole.Log("<color=#ff0000ff>    Ammo Cost: </color>" + module.ammoCost);
            ETGModConsole.Log("<color=#ff0000ff>    UI Ammo Type: </color>" + module.ammoType);
            if (module.ammoType == GameUIAmmoType.AmmoType.CUSTOM) ETGModConsole.Log("<color=#ff0000ff>    Custom UI Ammo Type: </color>" + module.customAmmoType);
            ETGModConsole.Log("<color=#ff0000ff>    AngleFromAim: </color>" + module.angleFromAim);
            ETGModConsole.Log("<color=#ff0000ff>    AngleVariance: </color>" + module.angleVariance);
            ETGModConsole.Log("<color=#ff0000ff>    ClipShots: </color>" + module.numberOfShotsInClip);
            if (module.projectiles != null && module.projectiles.Count > 0)
            {
                ETGModConsole.Log("<color=#09b022>    Projectiles:</color>");
                int iteratedBullets = 0;
                foreach (Projectile bullet in module.projectiles)
                {
                    ETGModConsole.Log("<color=#09b022>        Bullet: </color>" + iteratedBullets);

                    iteratedBullets++;
                    if (bullet != null)
                    {
                        DeconstructProjectile(bullet, deconstructVFX, deconstructProjectileComponents);

                    }
                    else ETGModConsole.Log("<color=#ff0000ff>       Bullet is somehow null.</color>");
                }
            }
            else { ETGModConsole.Log($"<color=#ff0000ff>    {ModName} module has no normal projectiles.</color>"); }

            ETGModConsole.Log("<color=#09b022>    Charge Projectiles:</color>");
            if (module.chargeProjectiles != null && module.chargeProjectiles.Count > 0)
            {
                int iteratedBullets = 0;
                foreach (ProjectileModule.ChargeProjectile chargie in module.chargeProjectiles)
                {
                    if (chargie.Projectile != null)
                    {
                        Projectile bullet = chargie.Projectile;
                        ETGModConsole.Log("<color=#09b022>        Bullet: </color>" + iteratedBullets);
                        ETGModConsole.Log("<color=#ff0000ff>        Charge Time: </color>" + chargie.ChargeTime);

                        DeconstructProjectile(bullet, deconstructVFX, deconstructProjectileComponents);

                        iteratedBullets++;

                    }
                    else ETGModConsole.Log("<color=#ff0000ff>       Bullet is somehow null.</color>");
                }
            }
            else { ETGModConsole.Log($"<color=#ff0000ff>    {ModName} module has no charge projectiles.</color>"); }
        }
        public static void DeconstructProjectile(Projectile bullet, bool vfx, bool deconstructProjectileComponents)
        {
            if (!string.IsNullOrEmpty(bullet.name)) ETGModConsole.Log("<color=#ff0000ff>        Name: </color>" + bullet.name);
            else ETGModConsole.Log("<color=#ff0000ff>        Bullet has no name.</color>");

            ETGModConsole.Log("<color=#ff0000ff>        BaseData:</color>");
            if (bullet.baseData != null)
            {
                ETGModConsole.Log("<color=#ff0000ff>        Damage: </color>" + bullet.baseData.damage);
                ETGModConsole.Log("<color=#ff0000ff>        Speed: </color>" + bullet.baseData.speed);
                ETGModConsole.Log("<color=#ff0000ff>        Range: </color>" + bullet.baseData.range);
                ETGModConsole.Log("<color=#ff0000ff>        Knockback: </color>" + bullet.baseData.force);
            }
            else ETGModConsole.Log("<color=#ff0000ff>           BaseData is somehow null?</color>");
            ETGModConsole.Log("<color=#ff0000ff>        Other Stats:</color>");
            ETGModConsole.Log("<color=#ff0000ff>        BossDMGMult: </color>" + bullet.BossDamageMultiplier);
            ETGModConsole.Log("<color=#ff0000ff>        Damage Types: </color>" + bullet.damageTypes);

            if (deconstructProjectileComponents)
            {
                ETGModConsole.Log("<color=#ff0000ff>        Components:</color>");
                foreach (Component component in bullet.GetComponents<Component>())
                {
                    ETGModConsole.Log("        " + component.GetType().ToString());
                }

                int childIterator = 0;
                foreach (var child in bullet.gameObject.transform)
                {
                    ETGModConsole.Log($"<color=#ff0000ff>        Found Child [{childIterator}]:</color>");
                    if ((child as Transform).gameObject)
                    {
                        GameObject childObj = (child as Transform).gameObject;
                        ETGModConsole.Log($"<color=#ff0000ff>           Child Name:</color> {childObj.name}");

                        foreach (Component component in childObj.GetComponents<Component>())
                        {
                            ETGModConsole.Log("               " + component.GetType().ToString());
                            if (component.GetType().ToString().ToLower().Contains("trailcontroller"))
                            {
                                TrailController trail = component as TrailController;
                                ETGModConsole.Log("<color=#ff0000ff>                UsesStartAnim: </color>" + trail.usesStartAnimation);
                                ETGModConsole.Log("<color=#ff0000ff>                StartAnim: </color>" + trail.startAnimation);
                                ETGModConsole.Log("<color=#ff0000ff>                UsesAnim: </color>" + trail.usesAnimation);
                                ETGModConsole.Log("<color=#ff0000ff>                Anim: </color>" + trail.animation);
                                ETGModConsole.Log("<color=#ff0000ff>                UsesCascadeTimer: </color>" + trail.usesCascadeTimer);
                                ETGModConsole.Log("<color=#ff0000ff>                CascadeTimer: </color>" + trail.cascadeTimer);
                                ETGModConsole.Log("<color=#ff0000ff>                UsesSoftMaxLength: </color>" + trail.usesSoftMaxLength);
                                ETGModConsole.Log("<color=#ff0000ff>                SoftMaxLength: </color>" + trail.softMaxLength);
                                ETGModConsole.Log("<color=#ff0000ff>                UsesGlobalTimer: </color>" + trail.usesGlobalTimer);
                                ETGModConsole.Log("<color=#ff0000ff>                GlobalTimer: </color>" + trail.globalTimer);
                                ETGModConsole.Log("<color=#ff0000ff>                DestroyOnEmpty: </color>" + trail.destroyOnEmpty);
                                ETGModConsole.Log("<color=#ff0000ff>                GlobalTimer: </color>" + trail.globalTimer);
                                ETGModConsole.Log("<color=#ff0000ff>                UsesDispersalParticles: </color>" + trail.UsesDispersalParticles);
                                ETGModConsole.Log("<color=#ff0000ff>                DispersalDensity: </color>" + trail.DispersalDensity);
                                ETGModConsole.Log("<color=#ff0000ff>                DispersalMinCoherency: </color>" + trail.DispersalMinCoherency);
                                ETGModConsole.Log("<color=#ff0000ff>                DispersalMaxCoherency: </color>" + trail.DispersalMaxCoherency);
                                if (trail.DispersalParticleSystemPrefab != null)
                                {
                                    ETGModConsole.Log("<color=#ff0000ff>                Dispersal Particle System: </color>" + trail.DispersalParticleSystemPrefab.name);
                                    foreach (Component component2 in trail.DispersalParticleSystemPrefab.GetComponentsInChildren<Component>())
                                    {
                                        ETGModConsole.Log("                        " + component2.GetType().ToString());
                                    }
                                }
                            }
                        }

                    }
                    childIterator++;
                }
            }
        }
    }
}
