using Godot;
using Parallas;
using System;

namespace Hiker;

public partial class Player : CharacterBody3D
{
    public const float MaxStepHeight = 0.7f;
    public const float JumpHeight = 0.6f;

    [Export]
    public Node3D ModelRoot { get; set; }

    [Export]
    Skeleton3D Skeleton { get; set; }

    [Export]
    public CollisionShape3D Collider { get; set; }

    [Export]
    public float GravitySpeed { get; set; }

    [Export]
    public float TerminalVelocity { get; set; }

    [Export]
    public HikerCamera HikerCamera { get; set; }

    private Camera3D _mainCamera;

    private bool _jumpInput;

    private float _jumpVel;
    private Vector3 _motionVel;
    private Vector2 _inputDir;
    private Vector2 _targetDirection;

    private double _steepTimer = 3;

    private float _minSteepAngle = 45;
    private float _maxSteepAngle = 75;

    private float _maxStepAngle = 45;
    private bool _snappedToStairsLastFrame = false;
    private ulong _lastFrameWasOnFloor = ulong.MinValue;

    private float _backpackBasePos = 0;
    private float _backpackJiggle = 0;

    #region Godot Events

    public override void _Ready()
    {
        SetPhysicsProcess(true);

        _jumpVel = Mathf.Sqrt(2 * Math.Abs(GravitySpeed) * JumpHeight);

        _mainCamera = GetViewport().GetCamera3D();

        _maxSteepAngle = Mathf.RadToDeg(FloorMaxAngle);

        if (Skeleton.FindBone("backpack") is var backpackBone)
        {
            Vector3 bonePos = Skeleton.GetBonePosePosition(backpackBone);
            _backpackBasePos = bonePos.Y;
        }

        if (ModelRoot.FindChild("Hands") is MeshInstance3D meshHands)
            meshHands.SetInstanceShaderParameter("tint", Colors.Red);
        if (ModelRoot.FindChild("Shoes") is MeshInstance3D meshShoes)
            meshShoes.SetInstanceShaderParameter("tint", Colors.Chocolate);
        if (ModelRoot.FindChild("Scarf") is MeshInstance3D meshScarf)
            meshScarf.SetInstanceShaderParameter("tint", Colors.Maroon);
        if (ModelRoot.FindChild("Shirt") is MeshInstance3D meshShirt)
            meshShirt.SetInstanceShaderParameter("tint", Colors.SlateBlue);
        if (ModelRoot.FindChild("Hood", true) is MeshInstance3D meshHood)
            meshHood.SetInstanceShaderParameter("tint", Colors.SlateBlue);
        if (ModelRoot.FindChild("Backpack", true) is MeshInstance3D meshBackpack)
            meshBackpack.SetInstanceShaderParameter("tint", Colors.DarkSlateBlue);
        if (ModelRoot.FindChild("Face", true) is MeshInstance3D meshFace)
            meshFace.SetInstanceShaderParameter("tint", Colors.PeachPuff);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        _inputDir = Input.GetVector(
            "move_left",
            "move_right",
            "move_forward",
            "move_backward"
        ).Clamp(-1f, 1f);

        if(Input.IsActionJustPressed("jump"))
        {
            _jumpInput = true;
        }

        _backpackJiggle = MathUtil.ExpDecay(_backpackJiggle, 0f, 8f, (float)delta);
        float verticalSpeed = MathUtil.Project(Velocity, UpDirection).Dot(UpDirection);
        _backpackJiggle += -verticalSpeed * (float)delta * 0.25f;
        _backpackJiggle = Mathf.Clamp(_backpackJiggle, -0.05f, 0.1f);

        if (Skeleton.FindBone("backpack") is var backpackBone)
        {
            Vector3 bonePos = Skeleton.GetBonePosePosition(backpackBone);
            bonePos.Y = _backpackBasePos + _backpackJiggle;
            Skeleton.SetBonePosePosition(backpackBone, bonePos);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        bool moveAndSlideCollided = false;

        if (!SnapUpToStairsCheck(Velocity, delta))
        {
            moveAndSlideCollided = MoveAndSlide();
            SnapDownToStairsCheck(Velocity);
        }

        moveAndSlideCollided &= IsOnWall();

        var isOnFloor = IsOnFloor() || _snappedToStairsLastFrame;

        float targetMoveSpeed = 3f;

        Vector3 planarVelocity = MathUtil.ProjectOnPlane(Velocity, UpDirection);
        Vector3 verticalVelocity = MathUtil.Project(Velocity, UpDirection);
        verticalVelocity = MathUtil.Approach(verticalVelocity, -UpDirection * TerminalVelocity, -GravitySpeed * (float)delta);

        if(isOnFloor)
        {
            _lastFrameWasOnFloor = Engine.GetPhysicsFrames();
            UpdateSteepness(delta);
            HandleJumpInput(ref verticalVelocity);
        }

        Vector3 targetPlanarVel = targetMoveSpeed * new Vector3(_inputDir.X, 0, _inputDir.Y);
        targetPlanarVel = targetPlanarVel.Rotated(UpDirection, _mainCamera.GlobalRotation.Y);
        float inDirFactor = Mathf.Max(0f, targetPlanarVel.Normalized().Dot(Basis.Z));
        planarVelocity = MathUtil.ExpDecay(planarVelocity, targetPlanarVel * inDirFactor, 10f, (float)delta);

        Velocity = planarVelocity + verticalVelocity;

        UpdatePlayerRotation(targetPlanarVel, delta);
        UpdateHikerCam(moveAndSlideCollided, planarVelocity);
    }

    #endregion

    #region Input Handling

    private void HandleJumpInput(ref Vector3  verticalVelocity)
    {
        if(_jumpInput)
        {
            verticalVelocity = Vector3.Up * _jumpVel;
            _steepTimer = 0;
        }
        _jumpInput = false;
    }

    #endregion

    #region Basic Updating

    private void UpdateSteepness(double delta)
    {
        var degAngle = Mathf.RadToDeg(GetFloorAngle());
        var floorSteepnessFactor = MathUtil.InverseLerp01(_maxSteepAngle, _minSteepAngle, degAngle);
        var countdownSpeed = Mathf.Lerp(1.5f, 0.25f, floorSteepnessFactor);

        if (degAngle > _minSteepAngle)
        {
            _steepTimer -= delta * countdownSpeed;
            if (_steepTimer <= 0)
            {
                FloorMaxAngle = Mathf.DegToRad(_minSteepAngle);
            }
            return;
        }

        _steepTimer = 3;
        FloorMaxAngle = Mathf.DegToRad(_maxSteepAngle);
    }

    private void UpdateHikerCam(bool moveAndSlideCollided, Vector3 planarVelocity)
    {
        HikerCamera.TargetPosition = GlobalPosition + Vector3.Up * 1.45f;
        if(!moveAndSlideCollided)
        {
            HikerCamera.TargetPosition += planarVelocity * 0.05f;
        }
    }

    private void UpdatePlayerRotation(Vector3 cameraRelativeInput, double delta)
    {
        if (cameraRelativeInput.LengthSquared() > 0)
            _targetDirection = cameraRelativeInput.Normalized().ToVector2(true);
        var targetRotation = Quaternion.FromEuler(
            UpDirection * (-_targetDirection.Angle() + Mathf.Pi * 0.5f)
        );
        Quaternion = MathUtil.ExpDecay(Quaternion, targetRotation, 13f, (float)delta);
    }

    #endregion

    #region Stairs Handling

    private void SnapDownToStairsCheck(Vector3 currentVelocity)
    {
        var verticalVelocity = currentVelocity.Project(UpDirection);
        var didSnap = false;

        var floorBelow = false;
        if (CheckHitBelow(GlobalPosition, MaxStepHeight + Collider.Shape.Margin, out var floorBelowResult))
            floorBelow = !IsSurfaceTooSteep((Vector3)floorBelowResult["normal"]);

        var wasOnFloorLastFrame = Engine.GetPhysicsFrames() - _lastFrameWasOnFloor == 1;
        if (!IsOnFloor() && floorBelow && verticalVelocity.Dot(UpDirection) <= 0 && (wasOnFloorLastFrame || _snappedToStairsLastFrame))
        {
            if (TestMove(GlobalTransform, -UpDirection * MaxStepHeight, out var bodyTestResult))
            {
                var translateVertical = bodyTestResult.GetTravel().Dot(UpDirection);
                Position += UpDirection * translateVertical;
                ApplyFloorSnap();
                didSnap = true;
            }
        }

        _snappedToStairsLastFrame = didSnap;
    }

    private bool SnapUpToStairsCheck(Vector3 currentVelocity, double delta)
    {
        if (!IsOnFloor() && !_snappedToStairsLastFrame) return false;
        // Skip if jumping, or barely moving
        if (currentVelocity.Dot(UpDirection) > 0 ||
            MathUtil.ProjectOnPlane(currentVelocity, UpDirection).LengthSquared() <= 0.2f) return false;
        var expectedMoveMotion = MathUtil.ProjectOnPlane(currentVelocity, UpDirection) * (float)delta;

        // If the surface in exactly our moving direction is "basically a floor", don't try to step up. Let slopes handle that
        if (CheckHit(GlobalPosition, GlobalPosition + expectedMoveMotion * 3f, out var forwardHitResult))
        {
            var hitNormal = (Vector3)forwardHitResult["normal"];
            if (hitNormal.AngleTo(UpDirection) < _maxSteepAngle) return false;
        }

        var stepPosWithClearance = GlobalTransform.Translated(expectedMoveMotion + UpDirection * MaxStepHeight);
        var bodyTestResult =
            TestMove(
                stepPosWithClearance,
                -UpDirection * MaxStepHeight,
                out var downCheckResult
        );
        if (!bodyTestResult) return false;
        // var isHitOfValidType = downCheckResult.GetCollider().IsClass("StaticBody3D") ||
        //                        downCheckResult.GetCollider().IsClass("CSGShape3D");
        // if (!isHitOfValidType) return false;

        var stepHeight =
            (stepPosWithClearance.Origin + downCheckResult.GetTravel() - GlobalPosition).Dot(UpDirection);
        var downCheckCollisionPoint = downCheckResult.GetCollisionPoint();
        if (stepHeight > MaxStepHeight || stepHeight <= 0.01f ||
            (downCheckCollisionPoint - GlobalPosition).Dot(UpDirection) > MaxStepHeight)
            return false;

        var hitBelow = CheckHitBelow(
            downCheckCollisionPoint + UpDirection * MaxStepHeight + expectedMoveMotion,
            MaxStepHeight + Collider.Shape.Margin,
            out var floorBelowResult
        );
        if (!hitBelow) return false;
        if (IsSurfaceTooSteep((Vector3)floorBelowResult["normal"], Mathf.DegToRad(_maxStepAngle))) return false;

        GlobalPosition = stepPosWithClearance.Origin + downCheckResult.GetTravel();
        ApplyFloorSnap();
        _snappedToStairsLastFrame = true;
        return true;
    }

    #endregion

    #region Physics Testing

    private bool TestMove(Transform3D from, Vector3 motion, out PhysicsTestMotionResult3D result)
    {
        result = new PhysicsTestMotionResult3D();
        var parameters = new PhysicsTestMotionParameters3D() { From = from, Motion = motion };
        return PhysicsServer3D.BodyTestMotion(GetRid(), parameters, result);
    }

    private bool CheckHit(Vector3 startPos, Vector3 endPos, out Godot.Collections.Dictionary result)
    {
        result = new Godot.Collections.Dictionary();

        var spaceState = GetWorld3D().DirectSpaceState;
        var floorBelowQuery = PhysicsRayQueryParameters3D.Create(startPos, endPos);
        floorBelowQuery.Exclude = [GetRid()];
        result = spaceState.IntersectRay(floorBelowQuery);
        return result is not null && result.Count > 0;
    }

    private bool CheckHitBelow(Vector3 startPos, float distance, out Godot.Collections.Dictionary result) =>
        CheckHit(startPos, startPos - UpDirection * distance, out result);

    #endregion

    #region Helper Functions

    private bool IsSurfaceTooSteep(Vector3 normal, float? customAngle = null)
    {
        return normal.AngleTo(UpDirection) > (customAngle ?? FloorMaxAngle);
    }

    #endregion
}
