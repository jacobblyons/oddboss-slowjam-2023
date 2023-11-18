using Godot;
using System;

public partial class FriendGunController : Node
{
    public FireState fireState;

    [Export] public float chargeTime;
    [Export] public float dechargeFactor;
    [Export] public float recoilTime;
    [Export] public float chargeEffectRotationSpeed;
    [Export] public float dechargeEffectRotationSpeed;

    public WeaponUpgradeData upgradeData;

    private float charge;
    private float timeHeldAtMaxCharge;

    private Node3D tempChargeEffect;
    private PackedScene projectilePrefab;

    public override void _Ready() {
        fireState = FireState.DEFAULT;
        upgradeData.Reset();
        charge = 0f;
        timeHeldAtMaxCharge = 0f;
        tempChargeEffect = GetNode<Node3D>("ChargeEffect");
        projectilePrefab = GD.Load<PackedScene>("res://scenes/player/projectile.tscn");
    }

    public override void _PhysicsProcess(double delta) {
        float fdelta = (float)delta;
        if (fireState == FireState.CHARGING) {
            IncrementCharge(fdelta);
        }
        else if (fireState == FireState.DECHARGING) {
            DecrementCharge(fdelta);
        }
        else if (fireState == FireState.MAXCHARGE) {
            timeHeldAtMaxCharge += (float)delta;
        }

        UpdateTempChargeEffect(fdelta);
    }

    public void PullTrigger() {
        if (fireState == FireState.DEFAULT) {
            BeginCharge();
        }
    }

    public void ReleaseTrigger() {
        if (fireState == FireState.CHARGING) {
            EndCharge();
        }
        else if (fireState == FireState.MAXCHARGE) {
            Fire();
        }
    }

    public void BeginCharge() {
        if (fireState == FireState.DEFAULT) {
            fireState = FireState.CHARGING;
        }
    }

    public void EndCharge() {
        if (fireState == FireState.CHARGING || fireState == FireState.MAXCHARGE) {
            fireState = FireState.DECHARGING;
        }
    }

    public void ResetCharge() {
        charge = 0f;
        fireState = FireState.DEFAULT;
    }

    private void IncrementCharge(float fdelta) {
        charge += fdelta;
        if (charge > chargeTime / ((float)upgradeData.chargeLvl + 1f)) {
            //charge = chargeTime;
            fireState = FireState.MAXCHARGE;
        }
    }

    // Decrement weapon charge. Set fireState to DEFAULT if charge = 0
    private void DecrementCharge(float fdelta) {
        charge -= fdelta * dechargeFactor;
        if (charge < 0f) {
            charge = 0f;
            fireState = FireState.DEFAULT;
        }
    }

    private void Fire() {
        // Spawn new fire effect.
        Node3D newProj = projectilePrefab.Instantiate<Node3D>();
        GetNode("//root").AddChild(newProj);
        newProj.Transform = tempChargeEffect.GlobalTransform;

        if (upgradeData.powerLvl >= 1) {
            newProj = projectilePrefab.Instantiate<Node3D>();
            GetNode("//root").AddChild(newProj);
            newProj.Transform = tempChargeEffect.GlobalTransform;
            newProj.RotateY(0.07f);

            newProj = projectilePrefab.Instantiate<Node3D>();
            GetNode("//root").AddChild(newProj);
            newProj.Transform = tempChargeEffect.GlobalTransform;
            newProj.RotateY(-0.07f);
        }

        if (upgradeData.powerLvl >= 2) {
            newProj = projectilePrefab.Instantiate<Node3D>();
            GetNode("//root").AddChild(newProj);
            newProj.Transform = tempChargeEffect.GlobalTransform;
            newProj.RotateY(0.14f);

            newProj = projectilePrefab.Instantiate<Node3D>();
            GetNode("//root").AddChild(newProj);
            newProj.Transform = tempChargeEffect.GlobalTransform;
            newProj.RotateY(-0.14f);
        }
        ResetCharge();
    }

    private void UpdateTempChargeEffect(float fdelta) {
        // Update scale.
        float minScale = 0.01f;
        float maxScale = 0.5f;
        float newScale = minScale + (maxScale - minScale) * (charge / (chargeTime / ((float)upgradeData.chargeLvl + 1f)));
        tempChargeEffect.Scale = new Vector3(1f,1f,1f) * newScale;

        // Update rotation.
        if (fireState == FireState.CHARGING) {
            tempChargeEffect.RotateZ(chargeEffectRotationSpeed * fdelta);
        }
        else if (fireState == FireState.MAXCHARGE) {
            tempChargeEffect.RotateZ(dechargeEffectRotationSpeed * fdelta);
        }
    }
}

public enum FireState {
    DEFAULT,    // Gun loaded & ready to fire.
    CHARGING,   // Gun is charging a shot.
    MAXCHARGE,  // 'Idling' at max charge.
    DECHARGING, // Canceling gun charge.
    RECOILING   // Gun is recoiling.
}

public struct WeaponUpgradeData {
    public int chargeLvl;
    public int powerLvl;

    private static int maxChargeLvl = 2;
    private static int maxPowerLvl = 2;

    public void Reset() {
        chargeLvl = 0;
        powerLvl = 0;
    }

    public void IncrementChargeLvl() {
        chargeLvl += 1;
        if (chargeLvl > maxChargeLvl) { chargeLvl = maxChargeLvl; }
    }

    public void DecrementChargeLvl() {
        chargeLvl -= 1;
        if (chargeLvl < 0) { chargeLvl = 0; }
    }

    public void IncrementPowerLvl() {
        powerLvl += 1;
        if (powerLvl > maxPowerLvl) { powerLvl = maxPowerLvl; }
    }

    public void DecrementPowerLvl() {
        powerLvl -= 1;
        if (powerLvl < 0) { powerLvl = 0; }
    }
}
