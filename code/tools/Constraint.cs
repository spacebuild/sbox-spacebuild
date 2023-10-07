﻿using System.Collections.Generic;
using System.Linq;
using System;
using Sandbox.UI;
using Sandbox.UI.Construct;
using Sandbox.Physics;
using Sandbox.Systems.Player;

namespace Sandbox.Tools
{
	[Library( "tool_constraint", Title = "Constraint", Description = "Constrain stuff together", Group = "construction" )]
	public partial class ConstraintTool : BaseTool
	{
		// ConVar.ClientData doesn't seem to network its wrapped property nicely, so lets make our own...
		[ConVar.ClientData( "tool_constraint_type" )]
		public static ConstraintType _ { get; set; } = ConstraintType.Weld;
		public ConstraintType Type
		{
			get
			{
				var _ = Enum.TryParse( GetConvarValue( "tool_constraint_type" ), out ConstraintType val );
				return val;
			}
			private set
			{
				ConsoleSystem.Run( "tool_constraint_type", value.ToString() );
			}
		}

		[ConVar.ClientData( "tool_constraint_nudge_distance" )] public static string _2 { get; set; } = "10";
		[ConVar.ClientData( "tool_constraint_nudge_percent" )] public static string _3 { get; set; } = "0";
		[ConVar.ClientData( "tool_constraint_move_target" )] public static string _4 { get; set; } = "1";
		[ConVar.ClientData( "tool_constraint_move_offset" )] public static string _5 { get; set; } = "0";
		[ConVar.ClientData( "tool_constraint_move_percent" )] public static string _6 { get; set; } = "0";
		[ConVar.ClientData( "tool_constraint_rotate_target" )] public static string _7 { get; set; } = "1";
		[ConVar.ClientData( "tool_constraint_rotate_snap" )] public static string _8 { get; set; } = "15";
		[ConVar.ClientData( "tool_constraint_freeze_target" )] public static string _9 { get; set; } = "1";
		[ConVar.ClientData( "tool_constraint_nocollide_target" )] public static string _10 { get; set; } = "1";
		[ConVar.ClientData( "tool_constraint_rope_extra_length" )] public static string _11 { get; set; } = "0";
		[ConVar.ClientData( "tool_constraint_rope_rigid" )] public static string _12 { get; set; } = "0";

		private enum ConstraintToolStage {
			Waiting,
			Moving,
			Rotating,
			Applying,
			ConstraintController,
			Removing,
		}
		
		[Net, Predicted]
		private ConstraintToolStage stage { get; set; } = ConstraintToolStage.Waiting;
		private TraceResult trace1;
		private TraceResult trace2;
		private PhysicsPoint point1;
		private PhysicsPoint point2;
		// TraceResult's aren't [Net]workable, so lets sync the parts we need for the Preview
		[Net] private ModelEntity ent1 { get; set; }
		[Net] private Vector3 normal1 { get; set; }
		[Net] private Vector3 endPos1 { get; set; }
		private PhysicsJoint createdJoint;
		private Func<string> createdUndo;
		private bool wasFrozen;
		private bool wasSleeping;
		private bool wasMotionEnabled;
		private float rotationBuildUp;
		private const float RotateSpeed = 30.0f;

		// Dynamic entrypoint for optional Wirebox support, if installed
		public static Action<BasePlayer, TraceResult, ConstraintType, PhysicsJoint, Func<string>> CreateWireboxConstraintController;
		private static bool WireboxSupport
		{
			get => CreateWireboxConstraintController != null;
		}

		public override void BuildInput()
		{
			if ( stage == ConstraintToolStage.Rotating )
			{
				// Lock view angles while we're using the mouse to rotate a prop
				if ( Owner is Player pl )
				{
					pl.ViewAngles = pl.OriginalViewAngles;
				}
			}
		}

		public override void Simulate()
		{
			if ( Game.IsClient )
			{
				this.Description = CalculateDescription();

				if ( Input.Pressed( "drop" ) )
				{
					SelectNextType();
				}
			}

			using ( Prediction.Off() )
			{
				if ( !Game.IsServer )
					return;

				if ( stage == ConstraintToolStage.Rotating )
				{
					if ( !trace1.Body.IsValid() || !trace2.Body.IsValid() )
					{
						Reset();
					}

					var rotationAmount = Input.MouseDelta.x * RotateSpeed * Time.Delta;
					var rotationSnap = float.Parse( GetConvarValue( "tool_constraint_rotate_snap", "0" ) );
					if ( rotationSnap >= 0.001 )
					{
						// todo: snap rotation relative to the base prop (or World angles I guess), rather than the target prop
						rotationBuildUp += rotationAmount;

						if ( rotationBuildUp <= -rotationSnap )
						{
							rotationBuildUp = 0;
							var rotation = Rotation.FromAxis( trace2.Normal, -rotationSnap );
							trace1.Entity.Transform = trace1.Entity.Transform.RotateAround( trace2.HitPosition, rotation );
						}
						else if (rotationBuildUp >= rotationSnap)
						{
							rotationBuildUp = 0;
							var rotation = Rotation.FromAxis( trace2.Normal, rotationSnap );
							trace1.Entity.Transform = trace1.Entity.Transform.RotateAround( trace2.HitPosition, rotation );
						}
					}
					else
					{
						var rotation = Rotation.FromAxis( trace2.Normal, rotationAmount );
						trace1.Entity.Transform = trace1.Entity.Transform.RotateAround( trace2.HitPosition, rotation );
					}
				}

				var tr = DoTrace();

				if ( !tr.Hit || !tr.Entity.IsValid() )
				{
					return;
				}
				
				if ( Input.Pressed( "attack1" ) )
				{
					if ( stage == ConstraintToolStage.Waiting )
					{
						trace1 = tr;
						if ( tr.Entity is ModelEntity modelEnt )
						{
							ent1 = modelEnt;
						}
						normal1 = tr.Normal;
						endPos1 = tr.EndPosition;
						stage = ConstraintToolStage.Moving;
						CreateHitEffects( tr.EndPosition, tr.Normal );
						return;
					}

					if ( stage == ConstraintToolStage.Moving )
					{
						trace2 = tr;
						if ( !trace1.Entity.IsValid() )
						{
							Reset();
							return;
						}

						if ( trace1.Entity.IsWorld && trace2.Entity.IsWorld )
						{
							return; // can't both be world
						}
						
						if ( GetConvarValue( "tool_constraint_move_target" ) != "0" )
						{
							var wantsRotation = GetConvarValue( "tool_constraint_rotate_target" ) != "0" && !trace1.Entity.IsWorld;
						
							wasFrozen = (trace1.Body.BodyType == PhysicsBodyType.Static);
							wasMotionEnabled = trace1.Body.MotionEnabled;
							wasSleeping = trace1.Body.Sleeping;

							if ( wantsRotation )
							{
								trace1.Body.Sleeping = false;
								trace1.Body.BodyType = PhysicsBodyType.Keyframed;
								trace1.Body.MotionEnabled = false;
							}
							
							var offset = float.Parse( GetConvarValue( "tool_constraint_move_offset" ) );
							if ( GetConvarValue( "tool_constraint_move_percent" ) != "0" )
							{
								offset = GetEntityOffsetPercent( offset, trace1 );
							}

							trace1.Entity.Transform = CalculateNewTransform(
								trace1.Entity.Transform,
								trace1.Normal,
								trace1.EndPosition,
								trace2.Normal,
								trace2.EndPosition,
								offset
							);

							if ( wantsRotation )
							{
								stage = ConstraintToolStage.Rotating;
								rotationBuildUp = 0;
								CreateHitEffects( tr.EndPosition, tr.Normal );
								return;
							}
						}

						// Don't return here because we can skip straight to applying
						stage = ConstraintToolStage.Applying;
					}

					if ( stage == ConstraintToolStage.Rotating )
					{
						// Don't return here because we can skip straight to applying
						stage = ConstraintToolStage.Applying;
					}

					if ( stage == ConstraintToolStage.Applying )
					{
						if ( !trace1.Entity.IsWorld )
						{
							if ( GetConvarValue( "tool_constraint_freeze_target" ) != "0" )
							{
								trace1.Body.Sleeping = true;
								trace1.Body.MotionEnabled = wasMotionEnabled;
								trace1.Body.BodyType = PhysicsBodyType.Static;
							}
							else
							{
								trace1.Body.MotionEnabled = wasMotionEnabled;
								trace1.Body.Sleeping = wasSleeping;
								trace1.Body.BodyType = wasFrozen ? PhysicsBodyType.Static : PhysicsBodyType.Dynamic;
							}
						}

						point1 = PhysicsPoint.World( trace1.Body, trace2.EndPosition, trace2.Entity.Rotation );
						point2 = PhysicsPoint.World( trace2.Body, trace2.EndPosition, trace2.Entity.Rotation );
						
						if ( Type == ConstraintType.Weld )
						{
							var joint = PhysicsJoint.CreateFixed(
								point1,
								point2
							);
							joint.Collisions = GetConvarValue( "tool_constraint_nocollide_target" ) == "0";

							FinishConstraintCreation( joint, () =>
							{
								if ( joint.IsValid() )
								{
									joint.Remove();
									return $"Removed {Type} constraint";
								}
								return "";
							} );
						}
						else if ( Type == ConstraintType.Nocollide )
						{
							var joint = PhysicsJoint.CreateFixed(
								point1,
								point2
							);
							joint.EnableAngularConstraint = false;
							joint.EnableLinearConstraint = false;
							joint.Collisions = false;
							FinishConstraintCreation( joint, () =>
							{
								if ( joint.IsValid() )
								{
									joint.Remove();
									return $"Removed {Type} constraint";
								}
								return "";
							} );
						}
						else if ( Type == ConstraintType.Spring )
						{
							var length = trace1.EndPosition.Distance( trace2.EndPosition );
							var joint = PhysicsJoint.CreateSpring(
								point1,
								point2,
								length,
								length
							);
							joint.SpringLinear = new( 5.0f, 0.7f );
							joint.Collisions = GetConvarValue( "tool_constraint_nocollide_target" ) == "0";
							joint.EnableAngularConstraint = false;

							var rope = MakeRope( trace1, trace2 );

							FinishConstraintCreation( joint, () =>
							{
								rope?.Destroy( true );
								if ( joint.IsValid() )
								{
									joint.Remove();
									return $"Removed {Type} constraint";
								}
								return "";
							} );
						}
						else if ( Type == ConstraintType.Rope )
						{
							var lengthOffset = float.Parse( GetConvarValue( "tool_constraint_rope_extra_length" ) );
							var length = trace1.EndPosition.Distance( trace2.EndPosition ) + lengthOffset;
							var joint = PhysicsJoint.CreateLength(
								point1,
								point2,
								length
							);
							joint.SpringLinear = new( 1000.0f, 0.7f );
							joint.Collisions = GetConvarValue( "tool_constraint_nocollide_target" ) == "0";
							joint.EnableAngularConstraint = false;

							if ( GetConvarValue( "tool_constraint_rope_rigid" ) == "1" )
							{
								joint.MinLength = length;
							}

							var rope = MakeRope( trace1, trace2 );

							FinishConstraintCreation( joint, () =>
							{
								rope?.Destroy( true );
								if ( joint.IsValid() )
								{
									joint.Remove();
									return $"Removed {Type} constraint";
								}
								return "";
							} );
						}
						else if ( Type == ConstraintType.Axis )
						{
							var pivot = Input.Down( "run" )
								? trace1.Body.MassCenter
								: trace2.EndPosition;

							var joint = PhysicsJoint.CreateHinge(
								trace1.Body,
								trace2.Body,
								pivot,
								trace2.Normal
							);
							joint.Collisions = GetConvarValue( "tool_constraint_nocollide_target" ) == "0";

							FinishConstraintCreation( joint, () =>
							{
								if ( joint.IsValid() )
								{
									joint.Remove();
									return $"Removed {Type} constraint";
								}
								return "";
							} );
						}
						else if ( Type == ConstraintType.BallSocket )
						{
							var pivot = Input.Down( "run" )
								? trace1.Body.MassCenter
								: trace1.EndPosition;

							var joint = PhysicsJoint.CreateBallSocket(
								trace1.Body,
								trace2.Body,
								pivot
							);
							joint.Collisions = GetConvarValue( "tool_constraint_nocollide_target" ) == "0";

							FinishConstraintCreation( joint, () =>
							{
								if ( joint.IsValid() )
								{
									joint.Remove();
									return $"Removed {Type} constraint";
								}
								return "";
							} );
						}
						else if ( Type == ConstraintType.Slider )
						{
							var joint = PhysicsJoint.CreateSlider(
								point1,
								point2,
								0,
								0 // can be used like a rope hybrid, to limit max length
							);
							joint.Collisions = GetConvarValue( "tool_constraint_nocollide_target" ) == "0";
							var rope = MakeRope( trace1, trace2 );
							FinishConstraintCreation( joint, () =>
							{
								rope?.Destroy( true );
								if ( joint.IsValid() )
								{
									joint.Remove();
									return $"Removed {Type} constraint";
								}
								return "";
							} );
						}
						if ( GetConvarValue( "tool_constraint_freeze_target" ) == "0" && !trace1.Entity.IsWorld )
						{
							trace1.Body.Sleeping = false;
						}
					}
					else if ( stage == ConstraintToolStage.ConstraintController )
					{
						// only reachable if Wirebox's installed
						if ( WireboxSupport )
						{
							CreateWireboxConstraintController( Owner, tr, Type, createdJoint, createdUndo );
						}
						Reset();
					}
				}
				else if ( Input.Pressed( "attack2" ) )
				{
					Nudge( tr, Input.Down( "run" ) ? 1 : -1 );

					Reset();
				}
				else if ( Input.Pressed( "reload" ) )
				{
					if ( !tr.Entity.IsValid() )
					{
						return;
					}
					if ( stage == ConstraintToolStage.Waiting )
					{
						trace1 = tr;
						stage = ConstraintToolStage.Removing;
						if ( Input.Down( "walk" ) )
						{
							RemoveConstraints( Type, tr );
							Reset();
						}
					}
					else if ( stage == ConstraintToolStage.Removing )
					{
						trace2 = tr;
						if ( !trace1.Entity.IsValid() )
						{
							Reset();
							return;
						}
						RemoveConstraintBetweenEnts( Type, trace1, trace2 );
						Reset();
					}
				}
				else
				{
					return;
				}

				CreateHitEffects( tr.EndPosition, tr.Normal );
			}
		}

		private void Nudge( TraceResult tr, int direction )
		{
			if ( !tr.Entity.IsValid() || tr.Entity.IsWorld )
			{
				return;
			}
			var offset = float.Parse( GetConvarValue( "tool_constraint_nudge_distance" ) );
			if ( GetConvarValue( "tool_constraint_nudge_percent" ) != "0" )
			{
				offset = GetEntityOffsetPercent( offset, tr );
			}
			tr.Entity.Position += tr.Normal * offset * direction;
			tr.Body.Sleeping = true;
		}

		private float GetEntityOffsetPercent( float percent, TraceResult tr )
		{
			if ( Math.Abs( tr.Normal.Dot( tr.Entity.Rotation.Forward ) ) > 0.8f )
			{
				return tr.Entity.WorldSpaceBounds.Size.x * percent / 100f;
			}
			else if ( Math.Abs( tr.Normal.Dot( tr.Entity.Rotation.Left ) ) > 0.8f )
			{
				return tr.Entity.WorldSpaceBounds.Size.y * percent / 100f;
			}
			else
			{
				return tr.Entity.WorldSpaceBounds.Size.z * percent / 100f;
			}
		}

		private void RemoveConstraints( ConstraintType type, TraceResult tr )
		{
			tr.Entity.GetJoints().ForEach( j =>
			{
				if ( j.GetConstraintType() == type )
				{
					j.Remove();
				}
			} );
		}

		private void RemoveConstraintBetweenEnts( ConstraintType type, TraceResult trace1, TraceResult trace2 )
		{
			trace1.Entity.GetJoints().ForEach( j =>
			{
				if ( (j.Body1 == trace1.Body || j.Body2 == trace1.Body) && (j.Body1 == trace2.Body || j.Body2 == trace2.Body) )
				{
					if ( j.GetConstraintType() == type )
					{
						j.Remove();
					}
				}
			} );
		}

		private void SelectNextType()
		{
			IEnumerable<ConstraintType> possibleEnums = Enum.GetValues<ConstraintType>();
			if ( Input.Down( "run" ) )
			{
				possibleEnums = possibleEnums.Reverse();
			}
			Type = possibleEnums.SkipWhile( e => e != Type ).Skip( 1 ).FirstOrDefault();
		}

		private string CalculateDescription()
		{
			var desc = $"Constraint entities together using {Type} constraint";
			if ( Type == ConstraintType.Axis )
			{
				if ( stage == ConstraintToolStage.Waiting )
				{
					desc += $"\nFirst, {Input.GetButtonOrigin( "attack1" )} the part that spins (eg. wheel).";
				}
				else if ( stage == ConstraintToolStage.Moving )
				{
					desc += $"\nSecond, {Input.GetButtonOrigin( "attack1" )} the base. Hold {Input.GetButtonOrigin( "run" )} to use wheel's center of mass.";
				}
			}
			else
			{
				if ( stage == ConstraintToolStage.Waiting )
				{
					desc += $"\nFirst, {Input.GetButtonOrigin( "attack1" )} the part to attach.";
				}
				else if ( stage == ConstraintToolStage.Moving )
				{
					desc += $"\nSecond, {Input.GetButtonOrigin( "attack1" )} the base.";
				}
			}
			if ( stage == ConstraintToolStage.Waiting )
			{
				desc += $"\n{Input.GetButtonOrigin( "attack2" )} to nudge ({Input.GetButtonOrigin( "run" )} for reverse)";
				desc += $"\n{Input.GetButtonOrigin( "reload" )} to select an entity to remove {Type} constraint ({Input.GetButtonOrigin( "walk" )} to remove all {Type} constraints)";
				desc += $"\n{Input.GetButtonOrigin( "drop" )} to cycle to next constraint type";
			}
			if ( WireboxSupport )
			{
				if ( stage == ConstraintToolStage.Moving )
				{
					desc += $"\nHold {Input.GetButtonOrigin( "walk" )} to begin creating a Wire Constraint Controller";
				}
				else if ( stage == ConstraintToolStage.ConstraintController )
				{
					desc += $"\nFinally, place the Wire Constraint Controller";
				}
			}
			return desc;
		}

		private void FinishConstraintCreation( PhysicsJoint joint, Func<string> undo )
		{
			joint.OnBreak += () => { undo(); };

			Event.Run( "joint.spawned", joint, Owner );
			Event.Run( "undo.add", undo, Owner );
			Analytics.ServerIncrement( To.Single( Owner ), "constraint.created" );

			if ( WireboxSupport && Input.Down( "walk" ) )
			{
				createdJoint = joint;
				createdUndo = undo;
				stage = ConstraintToolStage.ConstraintController;
				return;
			}
			Reset();
		}

		private static Particles MakeRope( TraceResult trace1, TraceResult trace2 )
		{
			var rope = Particles.Create( "particles/rope.vpcf" );

			if ( trace1.Body.GetEntity().IsWorld )
			{
				rope.SetPosition( 0, trace1.EndPosition );
			}
			else
			{
				rope.SetEntityBone( 0, trace1.Body.GetEntity(), trace1.Bone, new Transform( trace1.Body.GetEntity().Transform.PointToLocal( trace1.EndPosition ) ) );
			}
			if ( trace2.Body.GetEntity().IsWorld )
			{
				rope.SetPosition( 1, trace2.EndPosition );
			}
			else
			{
				rope.SetEntityBone( 1, trace2.Body.GetEntity(), trace2.Bone, new Transform( trace2.Body.GetEntity().Transform.PointToLocal( trace2.EndPosition ) ) );
			}
			return rope;
		}

		public override void CreateToolPanel()
		{
			if ( Game.IsClient )
			{
				var toolConfigUi = new ConstraintToolConfig();
				SpawnMenu.Instance?.ToolPanel?.AddChild( toolConfigUi );
			}
		}

		private void Reset()
		{
			stage = ConstraintToolStage.Waiting;
		}

		public override void Activate()
		{
			base.Activate();

			Reset();
		}

		public override void Deactivate()
		{
			base.Deactivate();

			Reset();
		}

		PreviewEntity previewModel;

		protected override bool IsPreviewTraceValid( TraceResult tr )
		{
			if ( stage != ConstraintToolStage.Moving || !ent1.IsValid() )
				return false;

			if ( !base.IsPreviewTraceValid( tr ) )
				return false;

			return true;
		}

		public override void CreatePreviews()
		{
			if ( !Game.IsClient )
			{
				return;
			}
			TryCreatePreview( ref previewModel, "models/citizen_props/crate01.vmdl" );
		}

		public override void UpdatePreviews()
		{
			if ( !Game.IsClient )
			{
				return;
			}
			CreatePreviews();
			base.UpdatePreviews();

			var tr2 = DoTrace();
			if ( !IsPreviewTraceValid( tr2 ) )
			{
				return;
			}
			if ( previewModel.IsValid() )
			{
				previewModel.Model = ent1.Model;
				previewModel.Transform = CalculateNewTransform( ent1.Transform, normal1, endPos1, tr2.Normal, tr2.EndPosition, float.Parse( GetConvarValue( "tool_constraint_move_offset" ) ) );
			}
		}

		private static Transform CalculateNewTransform( Transform originalTransform, Vector3 normal1, Vector3 endPos1, Vector3 normal2, Vector3 endPos2, float offset = 0f )
		{
			// Calculate the new rotation
			var transform = originalTransform;
			var rotation = transform.Rotation;
			var axis1Rotation = transform.RotationToLocal( Rotation.LookAt( normal1 ) );
			var axis2Rotation = transform.RotationToLocal( Rotation.LookAt( -normal2 ) );
			var rotationDifference = Rotation.Difference( axis1Rotation, axis2Rotation );
			var newRotation = rotation * rotationDifference;

			// The position offset has to be calculated before we apply the rotation
			var localOffset = transform.PointToLocal( endPos1 + normal1 * offset );
			transform.Rotation = newRotation;
			var newPosition = endPos2 - transform.PointToWorld( localOffset );
			transform.Position += newPosition;

			return transform;
		}
	}

	public enum ConstraintType
	{
		Weld,
		Nocollide, // Generic
		Axis, // Revolute
		BallSocket, // Spherical
		Rope,
		Spring, // Winch/Hydraulic
		Slider, // Prismatic
	}
}
