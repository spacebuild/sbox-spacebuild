using System.Collections.Generic;
using System.Linq;
using System;
namespace Sandbox.Tools
{
	[Library( "tool_constraint", Title = "Constraint", Description = "Constrain stuff together", Group = "construction" )]
	public partial class ConstraintTool : BaseTool
	{

		protected ConstraintType type = ConstraintType.Weld;

		[Net, Predicted]
		private int stage { get; set; } = 0;
		private TraceResult trace1;
		private TraceResult trace2;
		private object createdJoint;
		private Func<string> createdUndo;


		// Dynamic entrypoint for optional Wirebox support, if installed
		public static Action<Player, TraceResult, ConstraintType, object, Func<string>> CreateWireboxConstraintController;
		private static bool WireboxSupport
		{
			get => CreateWireboxConstraintController != null;
		}

		public override void Simulate()
		{
			this.Description = CalculateDescription();

			using ( Prediction.Off() ) {
				if ( Input.Pressed( InputButton.Drop ) ) {
					SelectNextType();
				}

				if ( !Host.IsServer )
					return;

				var startPos = Owner.EyePos;
				var dir = Owner.EyeRot.Forward;

				var tr = Trace.Ray( startPos, startPos + dir * MaxTraceDistance )
					.Ignore( Owner )
					.Run();

				if ( !tr.Hit || !tr.Entity.IsValid() ) {
					return;
				}


				if ( Input.Pressed( InputButton.Attack1 ) ) {
					if ( stage == 0 ) {
						trace1 = tr;
						stage++;
					}
					else if ( stage == 1 ) {
						trace2 = tr;
						if ( !trace1.Entity.IsValid() ) {
							Reset();
							return;
						}
						if ( trace1.Entity.IsWorld && trace2.Entity.IsWorld ) {
							return; // can't both be world
						}

						if ( type == ConstraintType.Weld ) {
							var joint = PhysicsJoint.Weld
								.From( trace1.Body )
								.To( trace2.Body, trace2.Body.Transform.PointToLocal( trace1.Body.Position ), trace2.Body.Transform.RotationToLocal( trace1.Body.Rotation ) )
								.WithCollisionsEnabled()
								.Create();

							FinishConstraintCreation( joint, () => {
								if ( joint.IsValid() ) {
									joint.Remove();
									return $"Removed {type} constraint";
								}
								return "";
							} );
						}
						else if ( type == ConstraintType.Nocollide ) {
							var joint = PhysicsJoint.Generic
								.From( trace1.Body )
								.To( trace2.Body )
								.Create();
							FinishConstraintCreation( joint, () => {
								if ( joint.IsValid() ) {
									joint.Remove();
									return $"Removed {type} constraint";
								}
								return "";
							} );
						}
						else if ( type == ConstraintType.Spring ) {
							var length = trace1.EndPos.Distance( trace2.EndPos );
							var joint = PhysicsJoint.Spring
								.From( trace1.Body, trace1.Body.Transform.PointToLocal( trace1.EndPos ) )
								.To( trace2.Body, trace2.Body.Transform.PointToLocal( trace2.EndPos ) )
								.WithPivot( trace2.EndPos )
								.WithFrequency( 5.0f )
								.WithDampingRatio( 0.7f )
								.WithMinRestLength( 0 )
								.WithMaxRestLength( 0 )
								.WithCollisionsEnabled()
								// .WithFriction(1) // does this do anything?
								.Create();

							// todo: where to store rope refs? how to tidy up on prop remove?
							var rope = MakeRope( trace1, trace2 );

							FinishConstraintCreation( joint, () => {
								rope?.Destroy( true );
								if ( joint.IsValid() ) {
									joint.Remove();
									return $"Removed {type} constraint";
								}
								return "";
							} );
						}
						else if ( type == ConstraintType.Rope ) {
							var length = trace1.EndPos.Distance( trace2.EndPos );
							var joint = PhysicsJoint.Spring
								.From( trace1.Body, trace1.Body.Transform.PointToLocal( trace1.EndPos ) )
								.To( trace2.Body, trace2.Body.Transform.PointToLocal( trace2.EndPos ) )
								.WithFrequency( 1000.0f )
								.WithDampingRatio( 0.7f )
								.WithMinRestLength( 0 )
								.WithMaxRestLength( length )
								.WithCollisionsEnabled()
								.Create();

							var rope = MakeRope( trace1, trace2 );

							FinishConstraintCreation( joint, () => {
								rope?.Destroy( true );
								if ( joint.IsValid() ) {
									joint.Remove();
									return $"Removed {type} constraint";
								}
								return "";
							} );
						}
						else if ( type == ConstraintType.Axis ) {
							var pivot = Input.Down( InputButton.Run )
								? trace1.Body.MassCenter
								: trace1.EndPos;
							var joint = PhysicsJoint.Revolute
								.From( trace1.Body, pivot )
								.To( trace2.Body, trace2.EndPos )
								.WithPivot( pivot )
								.WithBasis( Rotation.LookAt( trace1.Normal, trace1.Direction ) * Rotation.From( new Angles( 90, 0, 0 ) ) )
								.WithCollisionsEnabled()
								// .WithFriction( 1 )
								.Create();
							FinishConstraintCreation( joint, () => {
								if ( joint.IsValid() ) {
									joint.Remove();
									return $"Removed {type} constraint";
								}
								return "";
							} );
						}
						else if ( type == ConstraintType.Slider ) {
							var joint = PhysicsJoint.Prismatic
								.From( trace1.Body, trace1.EndPos )
								.To( trace2.Body, trace2.EndPos )
								.WithBasis( Rotation.LookAt( trace1.Normal, trace1.Direction ) * Rotation.From( new Angles( 90, 0, 0 ) ) )
								.WithCollisionsEnabled()
								// .WithFriction( 1 )
								.Create();
							var rope = MakeRope( trace1, trace2 );
							FinishConstraintCreation( joint, () => {
								rope?.Destroy( true );
								if ( joint.IsValid() ) {
									joint.Remove();
									return $"Removed {type} constraint";
								}
								return "";
							} );
						}
					}
					else if ( stage == 2 ) {
						// only reachable if Wirebox's installed
						if ( WireboxSupport ) {
							CreateWireboxConstraintController( Owner, tr, type, createdJoint, createdUndo );
						}
						Reset();
					}
				}
				else if ( Input.Pressed( InputButton.Attack2 ) ) {
					Reset();
				}
				else if ( Input.Pressed( InputButton.Reload ) ) {
					if ( tr.Entity is not Prop prop ) {
						return;
					}

					// todo: how to remove all constraints from X, where are they stored?

					Reset();
				}
				else {
					return;
				}

				CreateHitEffects( tr.EndPos, tr.Normal );
			}
		}

		private void SelectNextType()
		{
			IEnumerable<ConstraintType> possibleEnums = Enum.GetValues<ConstraintType>();
			if ( Input.Down( InputButton.Run ) ) {
				possibleEnums = possibleEnums.Reverse();
			}
			type = possibleEnums.SkipWhile( e => e != type ).Skip( 1 ).FirstOrDefault();
		}

		private string CalculateDescription()
		{
			var desc = $"Constraint entities together using a {type} constraint";
			if ( type == ConstraintType.Axis ) {
				if ( stage == 0 ) {
					desc += $"\nFirst, shoot the part that spins (eg. wheel).";
				}
				else if ( stage == 1 ) {
					desc += $"\nSecond, shoot the base. Hold shift to use wheel's center of mass.";
				}
			}
			else {
				if ( stage == 1 ) {
					desc += $"\nSecond, shoot the base.";
				}
			}
			if ( WireboxSupport ) {
				if ( stage == 1 ) {
					desc += $"\nHold alt to begin creating a Wire Constraint Controller";
				}
				else if ( stage == 2 ) {
					desc += $"\nFinally, place the Wire Constraint Controller";
				}
			}
			return desc;
		}

		private void FinishConstraintCreation( object joint, Func<string> undo )
		{
			Sandbox.Hooks.Undos.AddUndo( undo, Owner );

			if ( WireboxSupport && Input.Down( InputButton.Walk ) ) {
				createdJoint = joint;
				createdUndo = undo;
				stage = 2;
				return;
			}
			Reset();
		}

		private static Particles MakeRope( TraceResult trace1, TraceResult trace2 )
		{
			var rope = Particles.Create( "particles/rope.vpcf" );

			if ( trace1.Entity.IsWorld ) {
				rope.SetEntity( 0, trace1.Entity, trace1.Entity.Transform.PointToLocal( trace1.EndPos ) );
			}
			else {
				rope.SetEntityBone( 0, trace1.Entity, trace1.Bone, new Transform( trace1.Entity.Transform.PointToLocal( trace1.EndPos ) ) );
			}
			if ( trace2.Entity.IsWorld ) {
				rope.SetEntity( 1, trace2.Entity, trace2.Entity.Transform.PointToLocal( trace2.EndPos ) );
			}
			else {
				rope.SetEntityBone( 1, trace2.Entity, trace2.Bone, new Transform( trace2.Entity.Transform.PointToLocal( trace2.EndPos ) ) );
			}
			return rope;
		}

		private void Reset()
		{
			stage = 0;
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
	}

	public enum ConstraintType
	{
		Weld,
		Nocollide, // Generic
		Spring, // Winch/Hydraulic
		Rope,
		Axis, // Revolute
		BallSocket, // Spherical
		Slider, // Prismatic
		Conical,
	}
}
