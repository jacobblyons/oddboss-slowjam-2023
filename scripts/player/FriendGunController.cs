using Godot;
using System;

public partial class FriendGunController : Node
{
    public FireState fireState;

    [Export] public float chargeTime;
    [Export] public float dechargeFactor;
    [Export] public float recoilTime;

    private float charge;

    private Node3D tempChargeEffect;

    public override void _Ready() {
        fireState = FireState.DEFAULT;
        charge = 0f;

        tempChargeEffect = GetNode<Node3D>("TempChargeEffect");
    }

    public override void _PhysicsProcess(double delta) {
        float fdelta = (float)delta;
        if (fireState == FireState.CHARGING) {
            IncrementCharge(fdelta);
        }
        else if (fireState == FireState.DECHARGING) {
            DecrementCharge(fdelta);
        }

        UpdateTempChargeEffect();
        GD.Print(charge);
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
            //TODO: SHOOOOOT!
            ResetCharge();
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
        if (charge > chargeTime) {
            charge = chargeTime;
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

    private void UpdateTempChargeEffect() {
        float minScale = 0.1f;
        float maxScale = 1.0f;

        float newScale = minScale + (maxScale - minScale) * (charge / chargeTime);

        tempChargeEffect.Scale = new Vector3(1f,1f,1f) * newScale;
    }
}

public enum FireState {
    DEFAULT,    // Gun loaded & ready to fire.
    CHARGING,   // Gun is charging a shot.
    MAXCHARGE,  // 'Idling' at max charge.
    DECHARGING, // Canceling gun charge.
    RECOILING   // Gun is recoiling.
}
