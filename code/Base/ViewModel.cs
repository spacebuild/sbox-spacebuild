﻿using Sandbox;

public class ViewModel : BaseViewModel
{
	protected float SwingInfluence => 0.05f;
	protected float ReturnSpeed => 5.0f;
	protected float MaxOffsetLength => 10.0f;
	protected float BobCycleTime => 7;
	protected Vector3 BobDirection => new Vector3( 0.0f, 1.0f, 0.5f );
	protected float InertiaDamping => 20.0f;

	private Vector3 swingOffset;
	private float lastPitch;
	private float lastYaw;
	private float bobAnim;
	private float bobSpeed;

	private bool activated = false;

	public bool EnableSwingAndBob = true;

	public float YawInertia { get; private set; }
	public float PitchInertia { get; private set; }

	public override void PlaceViewmodel()
	{
		if ( !Game.LocalPawn.IsValid() )
			return;

		var inPos = Camera.Position;
		var inRot = Camera.Rotation;

		if ( !activated )
		{
			lastPitch = inRot.Pitch();
			lastYaw = inRot.Yaw();

			YawInertia = 0;
			PitchInertia = 0;

			activated = true;
		}

		var cameraBoneIndex = GetBoneIndex( "camera" );
		if ( cameraBoneIndex != -1 )
		{
			var bone = GetBoneTransform( cameraBoneIndex, worldspace: false );
			Camera.Position += bone.Position;
			Camera.Rotation *= bone.Rotation;
		}

		Position = inPos;
		Rotation = inRot;

		var newPitch = Rotation.Pitch();
		var newYaw = Rotation.Yaw();

		var pitchDelta = Angles.NormalizeAngle( newPitch - lastPitch );
		var yawDelta = Angles.NormalizeAngle( lastYaw - newYaw );

		PitchInertia += pitchDelta;
		YawInertia += yawDelta;

		if ( EnableSwingAndBob )
		{
			var playerVelocity = Game.LocalPawn.Velocity;

			if ( Game.LocalPawn is Player player )
			{
				var controller = player.GetActiveController();
				if ( controller != null && controller.HasTag( "noclip" ) )
				{
					playerVelocity = Vector3.Zero;
				}
			}

			var verticalDelta = playerVelocity.z * Time.Delta;
			var viewDown = Rotation.FromPitch( newPitch ).Up * -1.0f;
			verticalDelta *= 1.0f - System.MathF.Abs( viewDown.Cross( Vector3.Down ).y );
			pitchDelta -= verticalDelta * 1.0f;

			var speed = playerVelocity.WithZ( 0 ).Length;
			speed = speed > 10.0 ? speed : 0.0f;
			bobSpeed = bobSpeed.LerpTo( speed, Time.Delta * InertiaDamping );

			var offset = CalcSwingOffset( pitchDelta, yawDelta );
			offset += CalcBobbingOffset( bobSpeed );

			Position += Rotation * offset;
		}
		else
		{
			SetAnimParameter( "aim_yaw_inertia", YawInertia );
			SetAnimParameter( "aim_pitch_inertia", PitchInertia );
		}

		lastPitch = newPitch;
		lastYaw = newYaw;

		YawInertia = YawInertia.LerpTo( 0, Time.Delta * InertiaDamping );
		PitchInertia = PitchInertia.LerpTo( 0, Time.Delta * InertiaDamping );
	}

	protected Vector3 CalcSwingOffset( float pitchDelta, float yawDelta )
	{
		var swingVelocity = new Vector3( 0, yawDelta, pitchDelta );

		swingOffset -= swingOffset * ReturnSpeed * Time.Delta;
		swingOffset += (swingVelocity * SwingInfluence);

		if ( swingOffset.Length > MaxOffsetLength )
		{
			swingOffset = swingOffset.Normal * MaxOffsetLength;
		}

		return swingOffset;
	}

	protected Vector3 CalcBobbingOffset( float speed )
	{
		bobAnim += Time.Delta * BobCycleTime;

		var twoPI = System.MathF.PI * 2.0f;

		if ( bobAnim > twoPI )
		{
			bobAnim -= twoPI;
		}

		var offset = BobDirection * (speed * 0.005f) * System.MathF.Cos( bobAnim );
		offset = offset.WithZ( -System.MathF.Abs( offset.z ) );

		return offset;
	}

	protected override void OnAnimGraphCreated()
	{
		base.OnAnimGraphCreated();

		SetAnimParameter( "b_deploy", true );
	}
}
