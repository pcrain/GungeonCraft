/*using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Brave.BulletScript;
using EnemyAPI;
using UnityEngine;

public class HardModeCreechBehavior : OverrideBehavior
{

    public override string OverrideAIActorGUID => "37340393f97f41b2822bc02d14654172"; // Replace the GUID with whatever enemy you want to modify. This GUID is for the bullet kin.

    public override void DoOverride()
    {
        // In this method, you can do whatever you want with the enemy using the fields "actor", "healthHaver", "behaviorSpec", and "bulletBank".

        //actor.MovementSpeed *= 2; // Doubles the enemy movement speed

        //healthHaver.SetHealthMaximum(healthHaver.GetMaxHealth() * 0.5f); // Halves the enemy health

        // The BehaviorSpeculator is responsible for almost everything an enemy does, from shooting a gun to teleporting.
        // Tip: To debug an enemy's BehaviorSpeculator, you can uncomment the line below. This will print all the behavior information to the console.
        //Tools.DebugInformation(behaviorSpec);

        // For this first change, we're just going to increase the lead amount of the bullet kin's ShootGunBehavior so its shots fire like veteran kin.
        ShootBehavior shootBehavior = behaviorSpec.AttackBehaviorGroup.AttackBehaviors[0].Behavior as ShootBehavior; // Get the ShootGunBehavior, at index 0 of the AttackBehaviors list
        //shootGunBehavior.LeadAmount = 0.62f;

        // Next, we're going to change another few things on the ShootGunBehavior so that it has a custom BulletScript.
        shootBehavior.BulletScript = new CustomBulletScriptSelector(typeof(TestBulletScript)); // Sets the bullet kin's bullet script to our custom bullet script.
    }

    public class TestBulletScript : Script // This BulletScript is just a modified version of the script BulletManShroomed, which you can find with dnSpy.
    {
        protected override IEnumerator Top() // This is just a simple example, but bullet scripts can do so much more.
        {
            base.Fire(new Direction(-5f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null);
            base.Fire(new Direction(-3f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null);
            base.Fire(new Direction(0f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null); // Shoot a bullet -40 degrees from the enemy aim angle, with a bullet speed of 6.
            base.Fire(new Direction(3f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null); // Shoot a bullet 40 degrees from the enemy aim angle, with a bullet speed of 6.
            base.Fire(new Direction(5f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null);

            base.Fire(new Direction(175f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null);
            base.Fire(new Direction(177f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null);
            base.Fire(new Direction(180f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null); // Shoot a bullet -40 degrees from the enemy aim angle, with a bullet speed of 6.
            base.Fire(new Direction(183f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null); // Shoot a bullet 40 degrees from the enemy aim angle, with a bullet speed of 6.
            base.Fire(new Direction(185f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null);

            base.Fire(new Direction(85f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null);
            base.Fire(new Direction(87f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null);
            base.Fire(new Direction(90f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null); // Shoot a bullet -40 degrees from the enemy aim angle, with a bullet speed of 6.
            base.Fire(new Direction(93f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null); // Shoot a bullet 40 degrees from the enemy aim angle, with a bullet speed of 6.
            base.Fire(new Direction(95f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null);

            base.Fire(new Direction(-85f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null);
            base.Fire(new Direction(-87f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null);
            base.Fire(new Direction(-90f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null); // Shoot a bullet -40 degrees from the enemy aim angle, with a bullet speed of 6.
            base.Fire(new Direction(-93f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null); // Shoot a bullet 40 degrees from the enemy aim angle, with a bullet speed of 6.
            base.Fire(new Direction(-95f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null);

            yield return Wait(40);

            base.Fire(new Direction(35f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null);
            base.Fire(new Direction(37f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null);
            base.Fire(new Direction(40f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null); // Shoot a bullet -40 degrees from the enemy aim angle, with a bullet speed of 6.
            base.Fire(new Direction(43f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null); // Shoot a bullet 40 degrees from the enemy aim angle, with a bullet speed of 6.
            base.Fire(new Direction(45f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null);

            base.Fire(new Direction(135f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null);
            base.Fire(new Direction(137f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null);
            base.Fire(new Direction(140f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null); // Shoot a bullet -40 degrees from the enemy aim angle, with a bullet speed of 6.
            base.Fire(new Direction(143f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null); // Shoot a bullet 40 degrees from the enemy aim angle, with a bullet speed of 6.
            base.Fire(new Direction(145f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null);

            base.Fire(new Direction(-35f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null);
            base.Fire(new Direction(-37f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null);
            base.Fire(new Direction(-40f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null); // Shoot a bullet -40 degrees from the enemy aim angle, with a bullet speed of 6.
            base.Fire(new Direction(-43f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null); // Shoot a bullet 40 degrees from the enemy aim angle, with a bullet speed of 6.
            base.Fire(new Direction(-45f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null);

            base.Fire(new Direction(-135f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null);
            base.Fire(new Direction(-137f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null);
            base.Fire(new Direction(-140f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null); // Shoot a bullet -40 degrees from the enemy aim angle, with a bullet speed of 6.
            base.Fire(new Direction(-143f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null); // Shoot a bullet 40 degrees from the enemy aim angle, with a bullet speed of 6.
            base.Fire(new Direction(-145f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null);

            yield return Wait(40);

            base.Fire(new Direction(-5f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null);
            base.Fire(new Direction(-3f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null);
            base.Fire(new Direction(0f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null); // Shoot a bullet -40 degrees from the enemy aim angle, with a bullet speed of 6.
            base.Fire(new Direction(3f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null); // Shoot a bullet 40 degrees from the enemy aim angle, with a bullet speed of 6.
            base.Fire(new Direction(5f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null);

            base.Fire(new Direction(175f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null);
            base.Fire(new Direction(177f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null);
            base.Fire(new Direction(180f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null); // Shoot a bullet -40 degrees from the enemy aim angle, with a bullet speed of 6.
            base.Fire(new Direction(183f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null); // Shoot a bullet 40 degrees from the enemy aim angle, with a bullet speed of 6.
            base.Fire(new Direction(185f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null);

            base.Fire(new Direction(85f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null);
            base.Fire(new Direction(87f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null);
            base.Fire(new Direction(90f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null); // Shoot a bullet -40 degrees from the enemy aim angle, with a bullet speed of 6.
            base.Fire(new Direction(93f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null); // Shoot a bullet 40 degrees from the enemy aim angle, with a bullet speed of 6.
            base.Fire(new Direction(95f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null);

            base.Fire(new Direction(-85f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null);
            base.Fire(new Direction(-87f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null);
            base.Fire(new Direction(-90f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null); // Shoot a bullet -40 degrees from the enemy aim angle, with a bullet speed of 6.
            base.Fire(new Direction(-93f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null); // Shoot a bullet 40 degrees from the enemy aim angle, with a bullet speed of 6.
            base.Fire(new Direction(-95f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), null);
            yield break;
        }
    }
}
public class HardModeFallenBulletKinBehavior : OverrideBehavior
{
    public override string OverrideAIActorGUID => "5f3abc2d561b4b9c9e72b879c6f10c7e"; // Replace the GUID with whatever enemy you want to modify. This GUID is for the bullet kin.

    public override void DoOverride()
    {
        // In this method, you can do whatever you want with the enemy using the fields "actor", "healthHaver", "behaviorSpec", and "bulletBank".

        //actor.MovementSpeed *= 2; // Doubles the enemy movement speed

        //healthHaver.SetHealthMaximum(healthHaver.GetMaxHealth() * 0.5f); // Halves the enemy health

        // The BehaviorSpeculator is responsible for almost everything an enemy does, from shooting a gun to teleporting.
        // Tip: To debug an enemy's BehaviorSpeculator, you can uncomment the line below. This will print all the behavior information to the console.
        //Tools.DebugInformation(behaviorSpec);

        // For this first change, we're just going to increase the lead amount of the bullet kin's ShootGunBehavior so its shots fire like veteran kin.
        ShootGunBehavior shootGunBehavior = behaviorSpec.AttackBehaviors[0] as ShootGunBehavior; // Get the ShootGunBehavior, at index 0 of the AttackBehaviors list
        shootGunBehavior.LeadAmount = 0.62f;

        // Next, we're going to change another few things on the ShootGunBehavior so that it has a custom BulletScript.
        shootGunBehavior.WeaponType = WeaponType.BulletScript; // Makes it so the bullet kin will shoot our bullet script instead of his own gun shot.
        shootGunBehavior.BulletScript = new CustomBulletScriptSelector(typeof(TestBulletScript)); // Sets the bullet kin's bullet script to our custom bullet script.
    }

    public class TestBulletScript : Script // This BulletScript is just a modified version of the script BulletManShroomed, which you can find with dnSpy.
    {
        protected override IEnumerator Top() // This is just a simple example, but bullet scripts can do so much more.
        {
            float aimDirection = base.GetAimDirection((float)((UnityEngine.Random.value >= 0.25f) ? 0 : 1), 10f);
            base.Fire(new Direction(aimDirection, DirectionType.Absolute, -1f), new Speed(16f, SpeedType.Absolute), new TombstoneCrossAttack1.CrossBullet(new Vector2(0.7f, 0f), 0, 20));
            base.Fire(new Direction(aimDirection, DirectionType.Absolute, -1f), new Speed(16f, SpeedType.Absolute), new TombstoneCrossAttack1.CrossBullet(new Vector2(1.4f, 0f), 0, 20));
            base.Fire(new Direction(aimDirection, DirectionType.Absolute, -1f), new Speed(16f, SpeedType.Absolute), new TombstoneCrossAttack1.CrossBullet(new Vector2(2.1f, 0f), 0, 20));
            base.Fire(new Direction(aimDirection, DirectionType.Absolute, -1f), new Speed(16f, SpeedType.Absolute), new TombstoneCrossAttack1.CrossBullet(new Vector2(0f, 0f), 0, 20));
            base.Fire(new Direction(aimDirection, DirectionType.Absolute, -1f), new Speed(16f, SpeedType.Absolute), new TombstoneCrossAttack1.CrossBullet(new Vector2(-0.7f, 0f), 0, 20));
            base.Fire(new Direction(aimDirection, DirectionType.Absolute, -1f), new Speed(16f, SpeedType.Absolute), new TombstoneCrossAttack1.CrossBullet(new Vector2(-1.4f, 0f), 0, 20));
            base.Fire(new Direction(aimDirection, DirectionType.Absolute, -1f), new Speed(16f, SpeedType.Absolute), new TombstoneCrossAttack1.CrossBullet(new Vector2(-2.1f, 0f), 0, 20));
            base.Fire(new Direction(aimDirection, DirectionType.Absolute, -1f), new Speed(16f, SpeedType.Absolute), new TombstoneCrossAttack1.CrossBullet(new Vector2(-2.8f, 0f), 0, 20));
            base.Fire(new Direction(aimDirection, DirectionType.Absolute, -1f), new Speed(16f, SpeedType.Absolute), new TombstoneCrossAttack1.CrossBullet(new Vector2(0f, 0.7f), 18, 15));
            base.Fire(new Direction(aimDirection, DirectionType.Absolute, -1f), new Speed(16f, SpeedType.Absolute), new TombstoneCrossAttack1.CrossBullet(new Vector2(0f, -0.7f), 18, 15));
            base.Fire(new Direction(aimDirection, DirectionType.Absolute, -1f), new Speed(16f, SpeedType.Absolute), new TombstoneCrossAttack1.CrossBullet(new Vector2(0.7f, 1.4f), 18, 15));
            base.Fire(new Direction(aimDirection, DirectionType.Absolute, -1f), new Speed(16f, SpeedType.Absolute), new TombstoneCrossAttack1.CrossBullet(new Vector2(0.7f, -1.4f), 18, 15));
            base.Fire(new Direction(aimDirection, DirectionType.Absolute, -1f), new Speed(16f, SpeedType.Absolute), new TombstoneCrossAttack1.CrossBullet(new Vector2(1.4f, 1.4f), 18, 15));
            base.Fire(new Direction(aimDirection, DirectionType.Absolute, -1f), new Speed(16f, SpeedType.Absolute), new TombstoneCrossAttack1.CrossBullet(new Vector2(1.4f, -1.4f), 18, 15));
            base.Fire(new Direction(aimDirection, DirectionType.Absolute, -1f), new Speed(16f, SpeedType.Absolute), new TombstoneCrossAttack1.CrossBullet(new Vector2(2.1f, 1.4f), 18, 15));
            base.Fire(new Direction(aimDirection, DirectionType.Absolute, -1f), new Speed(16f, SpeedType.Absolute), new TombstoneCrossAttack1.CrossBullet(new Vector2(2.1f, -1.4f), 18, 15));

            yield return Wait(10f);

            aimDirection = base.GetAimDirection((float)((UnityEngine.Random.value >= 0.25f) ? 0 : 1), 10f);
            aimDirection += 25f;
            base.Fire(new Direction(aimDirection, DirectionType.Absolute, -1f), new Speed(10f, SpeedType.Absolute), new TombstoneCrossAttack1.CrossBullet(new Vector2(0.7f, 0f), 0, 20));
            base.Fire(new Direction(aimDirection, DirectionType.Absolute, -1f), new Speed(10f, SpeedType.Absolute), new TombstoneCrossAttack1.CrossBullet(new Vector2(1.4f, 0f), 0, 20));
            base.Fire(new Direction(aimDirection, DirectionType.Absolute, -1f), new Speed(10f, SpeedType.Absolute), new TombstoneCrossAttack1.CrossBullet(new Vector2(2.1f, 0f), 0, 20));
            base.Fire(new Direction(aimDirection, DirectionType.Absolute, -1f), new Speed(10f, SpeedType.Absolute), new TombstoneCrossAttack1.CrossBullet(new Vector2(0f, 0f), 0, 20));
            base.Fire(new Direction(aimDirection, DirectionType.Absolute, -1f), new Speed(10f, SpeedType.Absolute), new TombstoneCrossAttack1.CrossBullet(new Vector2(-0.7f, 0f), 0, 20));
            base.Fire(new Direction(aimDirection, DirectionType.Absolute, -1f), new Speed(10f, SpeedType.Absolute), new TombstoneCrossAttack1.CrossBullet(new Vector2(-1.4f, 0f), 0, 20));
            base.Fire(new Direction(aimDirection, DirectionType.Absolute, -1f), new Speed(10f, SpeedType.Absolute), new TombstoneCrossAttack1.CrossBullet(new Vector2(-2.1f, 0f), 0, 20));
            base.Fire(new Direction(aimDirection, DirectionType.Absolute, -1f), new Speed(10f, SpeedType.Absolute), new TombstoneCrossAttack1.CrossBullet(new Vector2(-2.8f, 0f), 0, 20));
            base.Fire(new Direction(aimDirection, DirectionType.Absolute, -1f), new Speed(10f, SpeedType.Absolute), new TombstoneCrossAttack1.CrossBullet(new Vector2(0f, 0.7f), 18, 15));
            base.Fire(new Direction(aimDirection, DirectionType.Absolute, -1f), new Speed(10f, SpeedType.Absolute), new TombstoneCrossAttack1.CrossBullet(new Vector2(0f, -0.7f), 18, 15));
            base.Fire(new Direction(aimDirection, DirectionType.Absolute, -1f), new Speed(10f, SpeedType.Absolute), new TombstoneCrossAttack1.CrossBullet(new Vector2(0.7f, 1.4f), 18, 15));
            base.Fire(new Direction(aimDirection, DirectionType.Absolute, -1f), new Speed(10f, SpeedType.Absolute), new TombstoneCrossAttack1.CrossBullet(new Vector2(0.7f, -1.4f), 18, 15));
            base.Fire(new Direction(aimDirection, DirectionType.Absolute, -1f), new Speed(10f, SpeedType.Absolute), new TombstoneCrossAttack1.CrossBullet(new Vector2(1.4f, 1.4f), 18, 15));
            base.Fire(new Direction(aimDirection, DirectionType.Absolute, -1f), new Speed(10f, SpeedType.Absolute), new TombstoneCrossAttack1.CrossBullet(new Vector2(1.4f, -1.4f), 18, 15));
            base.Fire(new Direction(aimDirection, DirectionType.Absolute, -1f), new Speed(10f, SpeedType.Absolute), new TombstoneCrossAttack1.CrossBullet(new Vector2(2.1f, 1.4f), 18, 15));
            base.Fire(new Direction(aimDirection, DirectionType.Absolute, -1f), new Speed(10f, SpeedType.Absolute), new TombstoneCrossAttack1.CrossBullet(new Vector2(2.1f, -1.4f), 18, 15));

            yield return Wait(10f);

            aimDirection = base.GetAimDirection((float)((UnityEngine.Random.value >= 0.25f) ? 0 : 1), 10f);
            aimDirection -= 25f;
            base.Fire(new Direction(aimDirection, DirectionType.Absolute, -1f), new Speed(10f, SpeedType.Absolute), new TombstoneCrossAttack1.CrossBullet(new Vector2(0.7f, 0f), 0, 20));
            base.Fire(new Direction(aimDirection, DirectionType.Absolute, -1f), new Speed(10f, SpeedType.Absolute), new TombstoneCrossAttack1.CrossBullet(new Vector2(1.4f, 0f), 0, 20));
            base.Fire(new Direction(aimDirection, DirectionType.Absolute, -1f), new Speed(10f, SpeedType.Absolute), new TombstoneCrossAttack1.CrossBullet(new Vector2(2.1f, 0f), 0, 20));
            base.Fire(new Direction(aimDirection, DirectionType.Absolute, -1f), new Speed(10f, SpeedType.Absolute), new TombstoneCrossAttack1.CrossBullet(new Vector2(0f, 0f), 0, 20));
            base.Fire(new Direction(aimDirection, DirectionType.Absolute, -1f), new Speed(10f, SpeedType.Absolute), new TombstoneCrossAttack1.CrossBullet(new Vector2(-0.7f, 0f), 0, 20));
            base.Fire(new Direction(aimDirection, DirectionType.Absolute, -1f), new Speed(10f, SpeedType.Absolute), new TombstoneCrossAttack1.CrossBullet(new Vector2(-1.4f, 0f), 0, 20));
            base.Fire(new Direction(aimDirection, DirectionType.Absolute, -1f), new Speed(10f, SpeedType.Absolute), new TombstoneCrossAttack1.CrossBullet(new Vector2(-2.1f, 0f), 0, 20));
            base.Fire(new Direction(aimDirection, DirectionType.Absolute, -1f), new Speed(10f, SpeedType.Absolute), new TombstoneCrossAttack1.CrossBullet(new Vector2(-2.8f, 0f), 0, 20));
            base.Fire(new Direction(aimDirection, DirectionType.Absolute, -1f), new Speed(10f, SpeedType.Absolute), new TombstoneCrossAttack1.CrossBullet(new Vector2(0f, 0.7f), 18, 15));
            base.Fire(new Direction(aimDirection, DirectionType.Absolute, -1f), new Speed(10f, SpeedType.Absolute), new TombstoneCrossAttack1.CrossBullet(new Vector2(0f, -0.7f), 18, 15));
            base.Fire(new Direction(aimDirection, DirectionType.Absolute, -1f), new Speed(10f, SpeedType.Absolute), new TombstoneCrossAttack1.CrossBullet(new Vector2(0.7f, 1.4f), 18, 15));
            base.Fire(new Direction(aimDirection, DirectionType.Absolute, -1f), new Speed(10f, SpeedType.Absolute), new TombstoneCrossAttack1.CrossBullet(new Vector2(0.7f, -1.4f), 18, 15));
            base.Fire(new Direction(aimDirection, DirectionType.Absolute, -1f), new Speed(10f, SpeedType.Absolute), new TombstoneCrossAttack1.CrossBullet(new Vector2(1.4f, 1.4f), 18, 15));
            base.Fire(new Direction(aimDirection, DirectionType.Absolute, -1f), new Speed(10f, SpeedType.Absolute), new TombstoneCrossAttack1.CrossBullet(new Vector2(1.4f, -1.4f), 18, 15));
            base.Fire(new Direction(aimDirection, DirectionType.Absolute, -1f), new Speed(10f, SpeedType.Absolute), new TombstoneCrossAttack1.CrossBullet(new Vector2(2.1f, 1.4f), 18, 15));
            base.Fire(new Direction(aimDirection, DirectionType.Absolute, -1f), new Speed(10f, SpeedType.Absolute), new TombstoneCrossAttack1.CrossBullet(new Vector2(2.1f, -1.4f), 18, 15));

            yield break;
        }
    }
}
public class HardModeGargoyleBehavior : OverrideBehavior
{

    public override string OverrideAIActorGUID => "981d358ffc69419bac918ca1bdf0c7f7"; // Replace the GUID with whatever enemy you want to modify. This GUID is for the bullet kin.

    public override void DoOverride()
    {
        // In this method, you can do whatever you want with the enemy using the fields "actor", "healthHaver", "behaviorSpec", and "bulletBank".

        //actor.MovementSpeed *= 2; // Doubles the enemy movement speed

        //healthHaver.SetHealthMaximum(healthHaver.GetMaxHealth() * 0.5f); // Halves the enemy health

        // The BehaviorSpeculator is responsible for almost everything an enemy does, from shooting a gun to teleporting.
        // Tip: To debug an enemy's BehaviorSpeculator, you can uncomment the line below. This will print all the behavior information to the console.
        //Tools.DebugInformation(behaviorSpec);

        // For this first change, we're just going to increase the lead amount of the bullet kin's ShootGunBehavior so its shots fire like veteran kin.
        ShootBehavior shootBehavior = behaviorSpec.AttackBehaviorGroup.AttackBehaviors[0].Behavior as ShootBehavior; // Get the ShootGunBehavior, at index 0 of the AttackBehaviors list
        //shootGunBehavior.LeadAmount = 0.62f;

        // Next, we're going to change another few things on the ShootGunBehavior so that it has a custom BulletScript.
        shootBehavior.BulletScript = new CustomBulletScriptSelector(typeof(TestBulletScript)); // Sets the bullet kin's bullet script to our custom bullet script.
        bulletBank.Bullets.Add(EnemyDatabase.GetOrLoadByGuid("39dca963ae2b4688b016089d926308ab").bulletBank.GetBullet("bat"));
    }

    public class TestBulletScript : Script // This BulletScript is just a modified version of the script BulletManShroomed, which you can find with dnSpy.
    {
        protected override IEnumerator Top() // This is just a simple example, but bullet scripts can do so much more.
        {
            float angle = 90;
            float angle2 = -90;
            bool rotDirectionCounterClock = true;
            if (UnityEngine.Random.value <= 0.5) rotDirectionCounterClock = false;
            for (int i = 0; i < 150; i++)
            {
                base.Fire(new Direction(angle, DirectionType.Absolute, -1f), new Speed(12f, SpeedType.Absolute), new Bullet("bat"));
                base.Fire(new Direction(angle2, DirectionType.Absolute, -1f), new Speed(12f, SpeedType.Absolute), new Bullet("bat"));
                if (UnityEngine.Random.value < 0.1) base.Fire(new Direction(0f, DirectionType.Aim, -1f), new Speed(20f, SpeedType.Absolute), null);
                yield return Wait(1.01f);
                if (rotDirectionCounterClock)
                {
                    angle += 5f;
                    angle2 += 5;
                }
                else
                {
                    angle -= 5f;
                    angle2 -= 5;
                }
            }
            yield break;
        }
    }
}
public class HardModePirateBulletBehavior : OverrideBehavior
{

    public override string OverrideAIActorGUID => "6f818f482a5c47fd8f38cce101f6566c"; // Replace the GUID with whatever enemy you want to modify. This GUID is for the bullet kin.

    public override void DoOverride()
    {
        // In this method, you can do whatever you want with the enemy using the fields "actor", "healthHaver", "behaviorSpec", and "bulletBank".

        //actor.MovementSpeed *= 2; // Doubles the enemy movement speed

        //healthHaver.SetHealthMaximum(healthHaver.GetMaxHealth() * 0.5f); // Halves the enemy health

        // The BehaviorSpeculator is responsible for almost everything an enemy does, from shooting a gun to teleporting.
        // Tip: To debug an enemy's BehaviorSpeculator, you can uncomment the line below. This will print all the behavior information to the console.
        //Tools.DebugInformation(behaviorSpec);

        // For this first change, we're just going to increase the lead amount of the bullet kin's ShootGunBehavior so its shots fire like veteran kin.
        ShootGunBehavior shootGunBehavior = behaviorSpec.AttackBehaviors[0] as ShootGunBehavior; // Get the ShootGunBehavior, at index 0 of the AttackBehaviors list
        shootGunBehavior.LeadAmount = 0.62f;

        // Next, we're going to change another few things on the ShootGunBehavior so that it has a custom BulletScript.
        shootGunBehavior.WeaponType = WeaponType.BulletScript; // Makes it so the bullet kin will shoot our bullet script instead of his own gun shot.
        shootGunBehavior.BulletScript = new CustomBulletScriptSelector(typeof(TestBulletScript));  // Sets the bullet kin's bullet script to our custom bullet script.
        bulletBank.Bullets.Add(EnemyDatabase.GetOrLoadByGuid("cd88c3ce60c442e9aa5b3904d31652bc").bulletBank.GetBullet("burst"));
    }

    public class TestBulletScript : Script // This BulletScript is just a modified version of the script BulletManShroomed, which you can find with dnSpy.
    {
        protected override IEnumerator Top() // This is just a simple example, but bullet scripts can do so much more.
        {
            for (int i = 0; i < 6; i++)
            {
                float randomVariance = UnityEngine.Random.Range(-10, 11);
                base.Fire(new Direction(randomVariance, DirectionType.Aim, -1f), new Speed(15f, SpeedType.Absolute), new Bullet("burst"));
                yield return Wait(7f);
            }
            yield break;
        }
    }
}
public class HardModePirateShotgunBehavior : OverrideBehavior
{

    public override string OverrideAIActorGUID => "86dfc13486ee4f559189de53cfb84107"; // Replace the GUID with whatever enemy you want to modify. This GUID is for the bullet kin.

    public override void DoOverride()
    {
        // In this method, you can do whatever you want with the enemy using the fields "actor", "healthHaver", "behaviorSpec", and "bulletBank".

        //actor.MovementSpeed *= 2; // Doubles the enemy movement speed

        //healthHaver.SetHealthMaximum(healthHaver.GetMaxHealth() * 0.5f); // Halves the enemy health

        // The BehaviorSpeculator is responsible for almost everything an enemy does, from shooting a gun to teleporting.
        // Tip: To debug an enemy's BehaviorSpeculator, you can uncomment the line below. This will print all the behavior information to the console.
        //Tools.DebugInformation(behaviorSpec);

        // For this first change, we're just going to increase the lead amount of the bullet kin's ShootGunBehavior so its shots fire like veteran kin.
        ShootGunBehavior shootGunBehavior = behaviorSpec.AttackBehaviors[0] as ShootGunBehavior; // Get the ShootGunBehavior, at index 0 of the AttackBehaviors list
        shootGunBehavior.LeadAmount = 0.62f;

        // Next, we're going to change another few things on the ShootGunBehavior so that it has a custom BulletScript.
        shootGunBehavior.WeaponType = WeaponType.BulletScript; // Makes it so the bullet kin will shoot our bullet script instead of his own gun shot.
        shootGunBehavior.BulletScript = new CustomBulletScriptSelector(typeof(TestBulletScript));  // Sets the bullet kin's bullet script to our custom bullet script.
        bulletBank.Bullets.Add(EnemyDatabase.GetOrLoadByGuid("cd88c3ce60c442e9aa5b3904d31652bc").bulletBank.GetBullet("burst"));
    }

    public class TestBulletScript : Script // This BulletScript is just a modified version of the script BulletManShroomed, which you can find with dnSpy.
    {
        protected override IEnumerator Top() // This is just a simple example, but bullet scripts can do so much more.
        {
            for (int i = 0; i < 6; i++)
            {
                float randomVariance = UnityEngine.Random.Range(-10, 11);
                base.Fire(new Direction(randomVariance, DirectionType.Aim, -1f), new Speed(15f, SpeedType.Absolute), new Bullet("burst"));
                yield return Wait(7f);
            }
            yield break;
        }
    }
}
public class HardModeShotgrubBehaviour : OverrideBehavior
{

    public override string OverrideAIActorGUID => "044a9f39712f456597b9762893fbc19c"; // Replace the GUID with whatever enemy you want to modify. This GUID is for the bullet kin.

    public override void DoOverride()
    {

        // For this first change, we're just going to increase the lead amount of the bullet kin's ShootGunBehavior so its shots fire like veteran kin.
        ShootGunBehavior shootGunBehavior = behaviorSpec.AttackBehaviors[0] as ShootGunBehavior; // Get the ShootGunBehavior, at index 0 of the AttackBehaviors list
        shootGunBehavior.LeadAmount = 0.62f;

        // Next, we're going to change another few things on the ShootGunBehavior so that it has a custom BulletScript.
        shootGunBehavior.Cooldown = 2f;
        shootGunBehavior.WeaponType = WeaponType.BulletScript; // Makes it so the bullet kin will shoot our bullet script instead of his own gun shot.
        shootGunBehavior.BulletScript = new CustomBulletScriptSelector(typeof(TestBulletScript));  // Sets the bullet kin's bullet script to our custom bullet script.
        bulletBank.Bullets.Add(EnemyDatabase.GetOrLoadByGuid("383175a55879441d90933b5c4e60cf6f").bulletBank.GetBullet("bigBullet"));

    }
    public class TestBulletScript : Script // This BulletScript is just a modified version of the script BulletManShroomed, which you can find with dnSpy.
    {
        protected override IEnumerator Top() // This is just a simple example, but bullet scripts can do so much more.
        {
            float angle = 25;
            for (int i = 0; i < 3; i++)
            {
                base.Fire(new Direction(angle, DirectionType.Aim, -1f), new Speed(15f, SpeedType.Absolute), new NewShotgrubBigBullet());
                angle -= 25f;
            }
            yield break;
        }
    }

    public class NewShotgrubBigBullet : Bullet 
    {
        public NewShotgrubBigBullet() : base("bigBullet", false, false, false)
        {

        }
        protected override IEnumerator Top() 
        {
            this.Projectile.specRigidbody.OnTileCollision += this.OnTileCollision;
            yield break;
        }
        private void OnTileCollision(CollisionData tileCollision)
        {
            var direction = tileCollision.Normal.ToAngle();
            float num = -22.5f;
            float num2 = 9f;
            for (int i = 0; i < 5; i++)
            {
                base.Fire(new Direction(direction, DirectionType.Absolute, -1f), new Speed(9f, SpeedType.Absolute), new GrossBullet(num + (float)i * num2));
            }
            this.Projectile.BulletScriptSettings.surviveTileCollisions = false;
            tileCollision.MyRigidbody.OnTileCollision -= this.OnTileCollision;           
        }
        public class GrossBullet : Bullet
        {
            // Token: 0x06000BAB RID: 2987 RVA: 0x00036B70 File Offset: 0x00034D70
            public GrossBullet(float deltaAngle) : base("gross", false, false, false)
            {
                this.deltaAngle = deltaAngle;
            }

            // Token: 0x06000BAC RID: 2988 RVA: 0x00036B88 File Offset: 0x00034D88
            protected override IEnumerator Top()
            {
                yield return this.Wait(20);
                this.Direction += this.deltaAngle;
                this.Speed += UnityEngine.Random.Range(-1f, 1f);
                yield break;
            }

            // Token: 0x06000BAD RID: 2989 RVA: 0x00036BA4 File Offset: 0x00034DA4
            public override void OnBulletDestruction(Bullet.DestroyType destroyType, SpeculativeRigidbody hitRigidbody, bool preventSpawningProjectiles)
            {
                if (preventSpawningProjectiles)
                {
                    return;
                }
                float num = base.RandomAngle();
                float num2 = 60f;
                for (int i = 0; i < 6; i++)
                {
                    base.Fire(new Direction(num + num2 * (float)i, DirectionType.Absolute, -1f), new Speed(8f, SpeedType.Absolute), new ShotgrubManAttack1.GrubBullet());
                }
            }

            // Token: 0x04000C83 RID: 3203
            private float deltaAngle;
        }

        // Token: 0x020002F3 RID: 755
        public class GrubBullet : Bullet
        {
            // Token: 0x06000BB4 RID: 2996 RVA: 0x00036CBA File Offset: 0x00034EBA
            public GrubBullet() : base(null, false, false, false)
            {
                base.SuppressVfx = true;
            }

            // Token: 0x06000BB5 RID: 2997 RVA: 0x00036CD0 File Offset: 0x00034ED0
            protected override IEnumerator Top()
            {
                this.ManualControl = true;
                Vector2 truePosition = this.Position;
                float startVal = UnityEngine.Random.value;
                for (int i = 0; i < 360; i++)
                {
                    float offsetMagnitude = Mathf.SmoothStep(-0.75f, 0.75f, Mathf.PingPong(startVal + (float)i / 60f * 3f, 1f));
                    truePosition += BraveMathCollege.DegreesToVector(this.Direction, this.Speed / 60f);
                    this.Position = truePosition + BraveMathCollege.DegreesToVector(this.Direction - 90f, offsetMagnitude);
                    yield return this.Wait(1);
                }
                this.Vanish(false);
                yield break;
            }
        }
    }
}
public class HardModeKalibulletBehaviour : OverrideBehavior
{

    public override string OverrideAIActorGUID => "ff4f54ce606e4604bf8d467c1279be3e"; // Replace the GUID with whatever enemy you want to modify. This GUID is for the bullet kin.

    public override void DoOverride()
    {
        // In this method, you can do whatever you want with the enemy using the fields "actor", "healthHaver", "behaviorSpec", and "bulletBank".

        //actor.MovementSpeed *= 2; // Doubles the enemy movement speed

        //healthHaver.SetHealthMaximum(healthHaver.GetMaxHealth() * 0.5f); // Halves the enemy health

        // The BehaviorSpeculator is responsible for almost everything an enemy does, from shooting a gun to teleporting.
        // Tip: To debug an enemy's BehaviorSpeculator, you can uncomment the line below. This will print all the behavior information to the console.
        //Tools.DebugInformation(behaviorSpec);

        // For this first change, we're just going to increase the lead amount of the bullet kin's ShootGunBehavior so its shots fire like veteran kin.
        ShootGunBehavior shootGunBehavior = behaviorSpec.AttackBehaviors[0] as ShootGunBehavior; // Get the ShootGunBehavior, at index 0 of the AttackBehaviors list

        // Next, we're going to change another few things on the ShootGunBehavior so that it has a custom BulletScript.
        shootGunBehavior.WeaponType = WeaponType.BulletScript; // Makes it so the bullet kin will shoot our bullet script instead of his own gun shot.
        shootGunBehavior.BulletScript = new CustomBulletScriptSelector(typeof(TestBulletScript));  // Sets the bullet kin's bullet script to our custom bullet script.
        bulletBank.Bullets.Add(EnemyDatabase.GetOrLoadByGuid("383175a55879441d90933b5c4e60cf6f").bulletBank.GetBullet("bigBullet"));

    }

    public class TestBulletScript : Script // This BulletScript is just a modified version of the script BulletManShroomed, which you can find with dnSpy.
    {
        protected override IEnumerator Top() // This is just a simple example, but bullet scripts can do so much more.
        {
            for (int i = 0; i < 10; i++)
            {
                float randomVariance = UnityEngine.Random.Range(-20, 20);
                if (UnityEngine.Random.value <= 0.20f) base.Fire(new Direction(randomVariance, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), new NewBigBurster());
                else base.Fire(new Direction(randomVariance, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), new WiggleBullet());
                yield return Wait(7f);
            }
            yield break;
        }
    }
    public class WiggleBullet : Bullet
    {
        // Token: 0x06000BB4 RID: 2996 RVA: 0x00036CBA File Offset: 0x00034EBA
        public WiggleBullet() : base(null, false, false, false)
        {
            base.SuppressVfx = true;
        }

        // Token: 0x06000BB5 RID: 2997 RVA: 0x00036CD0 File Offset: 0x00034ED0
        protected override IEnumerator Top()
        {
            this.ManualControl = true;
            Vector2 truePosition = this.Position;
            float startVal = UnityEngine.Random.value;
            for (int i = 0; i < 360; i++)
            {
                float offsetMagnitude = Mathf.SmoothStep(-0.75f, 0.75f, Mathf.PingPong(startVal + (float)i / 60f * 3f, 1f));
                truePosition += BraveMathCollege.DegreesToVector(this.Direction, this.Speed / 60f);
                this.Position = truePosition + BraveMathCollege.DegreesToVector(this.Direction - 90f, offsetMagnitude);
                yield return this.Wait(1);
            }
            this.Vanish(false);
            yield break;
        }
    }
    public class NewBigBurster : Bullet
    {
        // Token: 0x060006C0 RID: 1728 RVA: 0x0001F513 File Offset: 0x0001D713
        public NewBigBurster() : base("bigBullet", false, false, false)
        {

        }

        // Token: 0x060006C1 RID: 1729 RVA: 0x0001F534 File Offset: 0x0001D734
        public override void OnBulletDestruction(Bullet.DestroyType destroyType, SpeculativeRigidbody hitRigidbody, bool preventSpawningProjectiles)
        {
            if (preventSpawningProjectiles)
            {
                return;
            }
            float num = base.RandomAngle();
            float num2 = 20f;
            for (int i = 0; i < 18; i++)
            {
                Bullet bullet = new Bullet(null, false, false, false);
                base.Fire(new Direction(num + (float)i * num2, DirectionType.Absolute, -1f), new Speed(7f, SpeedType.Absolute), bullet);
            }
        }
    }
}*/

