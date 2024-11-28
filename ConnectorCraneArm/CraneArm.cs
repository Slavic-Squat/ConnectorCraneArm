using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class CraneArm
        {
            private int ID;
            private Program program;
            private IMyShipController controller;
            private IMyMotorAdvancedStator joint0Rotor;
            private bool joint0RotorInv;
            private IMyMotorAdvancedStator joint1Rotor;
            private bool joint1RotorInv;
            private IMyMotorAdvancedStator joint2Rotor;
            private bool joint2RotorInv;
            private IMyMotorAdvancedStator eePitchHinge;
            private bool eePitchHingeInv;
            private IMyMotorAdvancedStator eeYawRotor;
            private bool eeYawRotorInv;
            private IMyMotorAdvancedStator eeRollRotor;
            private bool eeRollRotorInv;

            private Vector3 targetCoord;
            private Vector3 targetCoordPrev;

            private float joint0Angle;
            private float joint0LowerLimit;
            private float joint0UpperLimit;
            private float joint0TargetAngle;

            private float joint1Angle;
            private float joint1LowerLimit;
            private float joint1UpperLimit;
            private float joint1TargetAngle;

            private float joint2Angle;
            private float joint2LowerLimit;
            private float joint2UpperLimit;
            private float joint2TargetAngle;

            private Vector3 seg0Vector;
            private float seg0Length;
            private Vector3 seg1Vector;
            private float seg1Length;

            private float eePitchAngle;
            private float eePitchLowerLimit;
            private float eePitchUpperLimit;
            private float eePitchTargetAngle;

            private float eeYawAngle;
            private float eeYawLowerLimit;
            private float eeYawUpperLimit;
            private float eeYawTargetAngle;

            private float eeRollAngle;
            private float eeRollLowerLimit;
            private float eeRollUpperLimit;
            private float eeRollTargetAngle;

            private float targetYaw_base;
            private float targetPitch_base;
            private float targetRoll_base;

            private float sensitivity;
            private float speed;
            private bool cylindricalMode;
            private bool OOB = false;
            public bool eeControlled = true;
            public bool armControlled = true;

            public CraneArm(Program program, int ID, IMyShipController controller, float sensitivity, float speed, bool cylindricalMode)
            {
                this.program = program;
                this.ID = ID;
                this.controller = controller;

                joint0Rotor = (IMyMotorAdvancedStator)program.GridTerminalSystem.GetBlockWithName($"Crane Joint0 Rotor0 [{ID}]");
                joint1Rotor = (IMyMotorAdvancedStator)program.GridTerminalSystem.GetBlockWithName($"Crane Joint1 Rotor0 [{ID}]");
                joint2Rotor = (IMyMotorAdvancedStator)program.GridTerminalSystem.GetBlockWithName($"Crane Joint2 Rotor0 [{ID}]");
                eePitchHinge = (IMyMotorAdvancedStator)program.GridTerminalSystem.GetBlockWithName($"End Effector Pitch Hinge [{ID}]");
                eeYawRotor = (IMyMotorAdvancedStator)program.GridTerminalSystem.GetBlockWithName($"End Effector Yaw Rotor [{ID}]");
                eeRollRotor = (IMyMotorAdvancedStator)program.GridTerminalSystem.GetBlockWithName($"End Effector Roll Rotor [{ID}]");

                this.sensitivity = sensitivity;
                this.speed = speed;
                this.cylindricalMode = cylindricalMode;

                

                joint0RotorInv = joint0Rotor.CustomData.Contains("Inverted");
                joint1RotorInv = joint1Rotor.CustomData.Contains("Inverted");
                joint2RotorInv = joint2Rotor.CustomData.Contains("Inverted");
                eePitchHingeInv = eePitchHinge.CustomData.Contains("Inverted");
                eeYawRotorInv = eeYawRotor.CustomData.Contains("Inverted");
                eeRollRotorInv = eeRollRotor.CustomData.Contains("Inverted");

                seg0Vector = new Vector3(0, 0, 0);
                seg1Vector = new Vector3(0, 0, 0);

                this.Init(); 
            }

            public void Init()
            {
                joint0Angle = joint0RotorInv ? -joint0Rotor.Angle : joint0Rotor.Angle;
                joint0Angle = Math.Abs(joint0Angle) > (float)Math.PI ? (2 * (float)Math.PI - Math.Abs(joint0Angle)) * -Math.Sign(joint0Angle) : joint0Angle;
                joint0LowerLimit = joint0RotorInv ? -joint0Rotor.UpperLimitRad : joint0Rotor.LowerLimitRad;
                joint0UpperLimit = joint0RotorInv ? -joint0Rotor.LowerLimitRad : joint0Rotor.UpperLimitRad;

                joint1Angle = joint1RotorInv ? -joint1Rotor.Angle : joint1Rotor.Angle;
                joint1Angle = Math.Abs(joint1Angle) > (float)Math.PI ? (2 * (float)Math.PI - Math.Abs(joint1Angle)) * -Math.Sign(joint1Angle) : joint1Angle;
                joint1LowerLimit = joint1RotorInv ? -joint1Rotor.UpperLimitRad : joint1Rotor.LowerLimitRad;
                joint1UpperLimit = joint1RotorInv ? -joint1Rotor.LowerLimitRad : joint1Rotor.UpperLimitRad;

                joint2Angle = joint2RotorInv ? -joint2Rotor.Angle : joint2Rotor.Angle;
                joint2Angle = Math.Abs(joint2Angle) > (float)Math.PI ? (2 * (float)Math.PI - Math.Abs(joint2Angle)) * -Math.Sign(joint2Angle) : joint2Angle;
                joint2LowerLimit = joint2RotorInv ? -joint2Rotor.UpperLimitRad : joint2Rotor.LowerLimitRad;
                joint2UpperLimit = joint2RotorInv ? -joint2Rotor.LowerLimitRad : joint2Rotor.UpperLimitRad;

                eePitchAngle = eePitchHingeInv ? -eePitchHinge.Angle : eePitchHinge.Angle;
                eePitchAngle += (float)Math.PI / 2;
                eePitchAngle = Math.Abs(eePitchAngle) > (float)Math.PI ? (2 * (float)Math.PI - Math.Abs(eePitchAngle)) * -Math.Sign(eePitchAngle) : eePitchAngle;
                eePitchLowerLimit = eePitchHingeInv ? -eePitchHinge.UpperLimitRad : eePitchHinge.LowerLimitRad;
                eePitchLowerLimit += (float)Math.PI / 2;
                eePitchUpperLimit = eePitchHingeInv ? -eePitchHinge.LowerLimitRad : eePitchHinge.UpperLimitRad;
                eePitchUpperLimit += (float)Math.PI / 2;

                eeYawAngle = eeYawRotorInv ? -eeYawRotor.Angle : eeYawRotor.Angle;
                eeYawAngle = Math.Abs(eeYawAngle) > (float)Math.PI ? (2 * (float)Math.PI - Math.Abs(eeYawAngle)) * -Math.Sign(eeYawAngle) : eeYawAngle;
                eeYawLowerLimit = eeYawRotorInv ? -eeYawRotor.UpperLimitRad : eeYawRotor.LowerLimitRad;
                eeYawUpperLimit = eeYawRotorInv ? -eeYawRotor.LowerLimitRad : eeYawRotor.UpperLimitRad;

                eeRollAngle = eeRollRotorInv ? -eeRollRotor.Angle : eeRollRotor.Angle;
                eeRollAngle = Math.Abs(eeRollAngle) > (float)Math.PI ? (2 * (float)Math.PI - Math.Abs(eeRollAngle)) * -Math.Sign(eeRollAngle) : eeRollAngle;
                eeRollLowerLimit = eeRollRotorInv ? -eeRollRotor.UpperLimitRad : eeRollRotor.LowerLimitRad;
                eeRollUpperLimit = eeRollRotorInv ? -eeRollRotor.LowerLimitRad : eeRollRotor.UpperLimitRad;

                seg0Length = seg0Vector.Length();
                seg1Length = seg1Vector.Length();

                Matrix H0 = Matrix.CreateRotationY(joint0Angle);
                Matrix H1 = Matrix.CreateRotationX(joint1Angle);
                Matrix H2 = Matrix.CreateRotationX(joint2Angle);
                H2.Translation = seg0Vector;
                Matrix H3 = Matrix.CreateRotationX(-(float)Math.PI / 2);
                H3.Translation = seg1Vector;

                if (cylindricalMode == false)
                {
                    Matrix HT = H3 * H2 * H1 * H0;
                    targetCoord = HT.Translation;

                    Matrix R0_ee = Matrix.CreateRotationY(eeYawAngle);
                    Matrix R1_ee = Matrix.CreateRotationX(eePitchAngle);
                    Matrix R2_ee = Matrix.CreateRotationZ(eeRollAngle);
                    Matrix RT_ee = R2_ee * R1_ee * R0_ee;
                    Matrix RT_base = RT_ee * HT;

                    targetPitch_base = (float)Math.Asin(-RT_base.M32);
                    if (Math.Round(RT_base.M32, 2) == -1)
                    {
                        targetRoll_base = 0;
                        targetYaw_base = (float)Math.Atan2(RT_base.M21, RT_base.M11);
                    }
                    else if (Math.Round(RT_base.M32, 2) == 1)
                    {
                        targetRoll_base = 0;
                        targetYaw_base = (float)Math.Atan2(-RT_base.M21, RT_base.M11);
                    }
                    else
                    {
                        targetRoll_base = (float)Math.Atan2(RT_base.M12, RT_base.M22);
                        targetYaw_base = (float)Math.Atan2(RT_base.M31, RT_base.M33);
                    }
                }
                else
                {
                    Matrix HT = H3 * H2 * H1;
                    targetCoord = HT.Translation;

                    Matrix R0_ee = Matrix.CreateRotationY(eeYawAngle);
                    Matrix R1_ee = Matrix.CreateRotationX(eePitchAngle);
                    Matrix R2_ee = Matrix.CreateRotationZ(eeRollAngle);
                    Matrix RT_ee = R2_ee * R1_ee * R0_ee;
                    Matrix RT_base = RT_ee * HT;

                    targetPitch_base = (float)Math.Asin(-RT_base.M32);
                    if (Math.Round(RT_base.M32, 2) == -1)
                    {
                        targetRoll_base = 0;
                        targetYaw_base = (float)Math.Atan2(RT_base.M21, RT_base.M11);
                    }
                    else if (Math.Round(RT_base.M32, 2) == 1)
                    {
                        targetRoll_base = 0;
                        targetYaw_base = (float)Math.Atan2(-RT_base.M21, RT_base.M11);
                    }
                    else
                    {
                        targetRoll_base = (float)Math.Atan2(RT_base.M12, RT_base.M22);
                        targetYaw_base = (float)Math.Atan2(RT_base.M31, RT_base.M33);
                    }
                }
            }

            public void Run()
            {
                joint0Angle = joint0RotorInv ? -joint0Rotor.Angle : joint0Rotor.Angle;
                joint0Angle = Math.Abs(joint0Angle) > (float)Math.PI ? (2 * (float)Math.PI - Math.Abs(joint0Angle)) * -Math.Sign(joint0Angle) : joint0Angle;

                joint1Angle = joint1RotorInv ? -joint1Rotor.Angle : joint1Rotor.Angle;
                joint1Angle = Math.Abs(joint1Angle) > (float)Math.PI ? (2 * (float)Math.PI - Math.Abs(joint1Angle)) * -Math.Sign(joint1Angle) : joint1Angle;

                joint2Angle = joint2RotorInv ? -joint2Rotor.Angle : joint2Rotor.Angle;
                joint2Angle = Math.Abs(joint2Angle) > (float)Math.PI ? (2 * (float)Math.PI - Math.Abs(joint2Angle)) * -Math.Sign(joint2Angle) : joint2Angle;

                eePitchAngle = eePitchHingeInv ? -eePitchHinge.Angle : eePitchHinge.Angle;
                eePitchAngle += (float)Math.PI / 2;
                eePitchAngle = Math.Abs(eePitchAngle) > (float)Math.PI ? (2 * (float)Math.PI - Math.Abs(eePitchAngle)) * -Math.Sign(eePitchAngle) : eePitchAngle;

                eeYawAngle = eeYawRotorInv ? -eeYawRotor.Angle : eeYawRotor.Angle;
                eeYawAngle = Math.Abs(eeYawAngle) > (float)Math.PI ? (2 * (float)Math.PI - Math.Abs(eeYawAngle)) * -Math.Sign(eeYawAngle) : eeYawAngle;

                eeRollAngle = eeRollRotorInv ? -eeRollRotor.Angle : eeRollRotor.Angle;
                eeRollAngle = Math.Abs(eeRollAngle) > (float)Math.PI ? (2 * (float)Math.PI - Math.Abs(eeRollAngle)) * -Math.Sign(eeRollAngle) : eeRollAngle;

                float minTargetDistance = Math.Abs(seg1Length - seg0Length) + 1;
                float maxTargetDistance = seg0Length + seg1Length;
                
                if (controller.RotationIndicator.X != 0 && eeControlled == true)
                {
                    targetPitch_base -= 0.05f * sensitivity * controller.RotationIndicator.X;
                }
                if (controller.RotationIndicator.Y != 0 && eeControlled == true)
                {
                    targetYaw_base -= 0.05f * sensitivity * controller.RotationIndicator.Y;
                }
                if (controller.RollIndicator != 0 && eeControlled == true)
                {
                    targetRoll_base -= 0.05f * sensitivity * controller.RollIndicator;
                }

                targetPitch_base = (float)Math.Min(Math.Max(-Math.PI / 2, targetPitch_base), Math.PI / 2);
                targetRoll_base = Math.Abs(targetRoll_base) > (float)Math.PI ? (2 * (float)Math.PI - Math.Abs(targetRoll_base)) * -Math.Sign(targetRoll_base) : targetRoll_base;
                targetYaw_base = Math.Abs(targetYaw_base) > (float)Math.PI ? (2 * (float)Math.PI - Math.Abs(targetYaw_base)) * -Math.Sign(targetYaw_base) : targetYaw_base;

                if (cylindricalMode == false)
                {
                    if (controller.MoveIndicator.Z != 0)
                    {
                        targetCoord.Z += sensitivity * controller.MoveIndicator.Z;
                    }

                    if (controller.MoveIndicator.Y != 0)
                    {
                        targetCoord.Y += sensitivity * controller.MoveIndicator.Y;
                    }

                    if (controller.MoveIndicator.X != 0)
                    {
                        targetCoord.X += sensitivity * controller.MoveIndicator.X;
                    }

                    float targetDistance = targetCoord.Length();

                    if (targetDistance > maxTargetDistance || targetDistance < minTargetDistance)
                    {
                        OOB = true;
                        goto OutOfBounds;
                    }

                    float X = ((targetDistance * targetDistance) - (seg1Length * seg1Length) + (seg0Length * seg0Length)) / (2 * targetDistance);
                    float Y = (float)Math.Sqrt((seg0Length * seg0Length) - (X * X));
                    
                    joint0TargetAngle = (float)Math.Atan2(-targetCoord.X, -targetCoord.Z);
                    if (joint0TargetAngle < joint0LowerLimit || joint0TargetAngle > joint0UpperLimit)
                    {
                        OOB = true;
                        goto OutOfBounds;
                    }
                    joint1TargetAngle = (float)Math.Atan2(Y, X);
                    if (joint1TargetAngle < joint1LowerLimit || joint1TargetAngle > joint1UpperLimit)
                    {
                        OOB = true;
                        goto OutOfBounds;
                    }
                    joint2TargetAngle = (float)Math.Atan2((0 - Y), (targetDistance - X)) - joint1TargetAngle;
                    if (joint2TargetAngle < joint2LowerLimit || joint2TargetAngle > joint2UpperLimit)
                    {
                        OOB = true;
                        goto OutOfBounds;
                    }

                    Matrix H0 = Matrix.CreateRotationY(joint0TargetAngle);
                    Matrix H1 = Matrix.CreateRotationX(joint1TargetAngle);
                    Matrix H2 = Matrix.CreateRotationX(joint2TargetAngle);
                    H2.Translation = seg0Vector;
                    Matrix H3 = Matrix.CreateRotationX(-(float)Math.PI / 2);
                    H3.Translation = seg1Vector;

                    Matrix HT = H3 * H2 * H1 * H0;

                    Matrix R0_base = Matrix.CreateRotationY(targetYaw_base);
                    Matrix R1_base = Matrix.CreateRotationX(targetPitch_base);
                    Matrix R2_base = Matrix.CreateRotationZ(targetRoll_base);
                    Matrix RT_base = R2_base * R1_base * R0_base;
                    Matrix RT_ee = RT_base * Matrix.Transpose(HT.GetOrientation());
                    float targetPitch_ee = (float)Math.Asin(-RT_ee.M32);
                    float targetYaw_ee;
                    float targetRoll_ee;
                    if (Math.Round(RT_ee.M32, 2) == -1)
                    {
                        targetRoll_ee = 0;
                        targetYaw_ee = (float)Math.Atan2(RT_ee.M21, RT_ee.M11);
                    }
                    else if (Math.Round(RT_ee.M32, 2) == 1)
                    {
                        targetRoll_ee = 0;
                        targetYaw_ee = (float)Math.Atan2(-RT_ee.M21, RT_ee.M11);
                    }
                    else
                    {
                        targetRoll_ee = (float)Math.Atan2(RT_ee.M12, RT_ee.M22);
                        targetYaw_ee = (float)Math.Atan2(RT_ee.M31, RT_ee.M33);
                    }

                    eeYawTargetAngle = targetYaw_ee;
                    if (eeYawTargetAngle < eeYawLowerLimit || eeYawTargetAngle > eeYawUpperLimit)
                    {
                        OOB = true;
                        goto OutOfBounds;
                    }
                    eePitchTargetAngle = targetPitch_ee;
                    if (eePitchTargetAngle < eePitchLowerLimit || eePitchTargetAngle > eePitchUpperLimit)
                    {
                        OOB = true;
                        goto OutOfBounds;
                    }
                    eeRollTargetAngle = targetRoll_ee;
                    if (eeRollTargetAngle < eeRollLowerLimit || eeRollTargetAngle > eeRollUpperLimit)
                    {
                        OOB = true;
                        goto OutOfBounds;
                    }

                    targetCoordPrev = targetCoord;

                    OutOfBounds:
                    
                    if (OOB)
                    {
                        program.Echo("OOB");
                        targetCoord = targetCoordPrev;
                        OOB = false;
                    }
                    

                    float joint0AngleError = joint0TargetAngle - joint0Angle;
                    joint0AngleError = Math.Abs(joint0AngleError) > (float)Math.PI ? (2 * (float)Math.PI - Math.Abs(joint0AngleError)) * -Math.Sign(joint0AngleError) : joint0AngleError;

                    float joint1AngleError = joint1TargetAngle - joint1Angle;
                    joint1AngleError = Math.Abs(joint1AngleError) > (float)Math.PI ? (2 * (float)Math.PI - Math.Abs(joint1AngleError)) * -Math.Sign(joint1AngleError) : joint1AngleError;

                    float joint2AngleError = joint2TargetAngle - joint2Angle;
                    joint2AngleError = Math.Abs(joint2AngleError) > (float)Math.PI ? (2 * (float)Math.PI - Math.Abs(joint2AngleError)) * -Math.Sign(joint2AngleError) : joint2AngleError;

                    float eeYawAngleError = eeYawTargetAngle - eeYawAngle;
                    eeYawAngleError = Math.Abs(eeYawAngleError) > (float)Math.PI ? (2 * (float)Math.PI - Math.Abs(eeYawAngleError)) * -Math.Sign(eeYawAngleError) : eeYawAngleError;

                    float eePitchAngleError = eePitchTargetAngle - eePitchAngle;
                    eePitchAngleError = Math.Abs(eePitchAngleError) > (float)Math.PI ? (2 * (float)Math.PI - Math.Abs(eePitchAngleError)) * -Math.Sign(eePitchAngleError) : eePitchAngleError;

                    float eeRollAngleError = eeRollTargetAngle - eeRollAngle;
                    eeRollAngleError = Math.Abs(eeRollAngleError) > (float)Math.PI ? (2 * (float)Math.PI - Math.Abs(eeRollAngleError)) * -Math.Sign(eeRollAngleError) : eeRollAngleError;

                    if (!float.IsNaN(joint0AngleError) && !float.IsNaN(joint1AngleError) && !float.IsNaN(joint2AngleError) && !float.IsNaN(eeYawAngleError) && !float.IsNaN(eePitchAngleError) && !float.IsNaN(eeRollAngleError))
                    {
                        joint0Rotor.TargetVelocityRad = joint0RotorInv ? -(speed * joint0AngleError) : (speed * joint0AngleError);

                        joint1Rotor.TargetVelocityRad = joint1RotorInv ? -(speed * joint1AngleError) : (speed * joint1AngleError);

                        joint2Rotor.TargetVelocityRad = joint2RotorInv ? -(speed * joint2AngleError) : (speed * joint2AngleError);

                        eeYawRotor.TargetVelocityRad = eeYawRotorInv ? -(speed * eeYawAngleError) : (speed * eeYawAngleError);

                        eePitchHinge.TargetVelocityRad = eePitchHingeInv ? -(speed * eePitchAngleError) : (speed * eePitchAngleError);

                        eeRollRotor.TargetVelocityRad = eeRollRotorInv ? -(speed * eeRollAngleError) : (speed * eeRollAngleError);

                    }
                    else
                    {
                        joint0Rotor.TargetVelocityRad = 0;

                        joint1Rotor.TargetVelocityRad = 0;

                        joint2Rotor.TargetVelocityRad = 0;

                        eeYawRotor.TargetVelocityRad = 0;

                        eePitchHinge.TargetVelocityRad = 0;

                        eeRollRotor.TargetVelocityRad = 0;
                    }

                }
                else
                {
                    if (controller.MoveIndicator.X != 0)
                    {
                        joint0Rotor.TargetVelocityRad = 0.1f * speed * controller.MoveIndicator.X;
                    }
                    else
                    {
                        joint0Rotor.TargetVelocityRad = 0;
                    }

                    if (controller.MoveIndicator.Y != 0)
                    {
                        targetCoord.Y += sensitivity * controller.MoveIndicator.Y;
                    }

                    if (controller.MoveIndicator.Z != 0)
                    {
                        targetCoord.Z += sensitivity * controller.MoveIndicator.Z;
                    }

                    float targetDistance = targetCoord.Length();

                    if (targetDistance > maxTargetDistance || targetDistance < minTargetDistance)
                    {
                        OOB = true;
                        goto OutOfBounds;
                    }

                    float X = ((targetDistance * targetDistance) - (seg1Length * seg1Length) + (seg0Length * seg0Length)) / (2 * targetDistance);
                    float Y = (float)Math.Sqrt((seg0Length * seg0Length) - (X * X));

                    joint1TargetAngle = (float)Math.Atan2(Y, X);
                    if (joint1TargetAngle < joint1LowerLimit || joint1TargetAngle > joint1UpperLimit)
                    {
                        OOB = true;
                        goto OutOfBounds;
                    }
                    joint2TargetAngle = (float)Math.Atan2((0 - Y), (targetDistance - X)) - joint1TargetAngle;
                    if (joint2TargetAngle < joint2LowerLimit || joint2TargetAngle > joint2UpperLimit)
                    {
                        OOB = true;
                        goto OutOfBounds;
                    }

                    Matrix H0 = Matrix.CreateRotationY(joint0Angle);
                    Matrix H1 = Matrix.CreateRotationX(joint1TargetAngle);
                    Matrix H2 = Matrix.CreateRotationX(joint2TargetAngle);
                    H2.Translation = seg0Vector;
                    Matrix H3 = Matrix.CreateRotationX(-(float)Math.PI / 2);
                    H3.Translation = seg1Vector;

                    Matrix HT = H3 * H2 * H1;

                    Matrix R0_base = Matrix.CreateRotationY(targetYaw_base);
                    Matrix R1_base = Matrix.CreateRotationX(targetPitch_base);
                    Matrix R2_base = Matrix.CreateRotationZ(targetRoll_base);
                    Matrix RT_base = R2_base * R1_base * R0_base;
                    Matrix RT_ee = RT_base * Matrix.Transpose(HT.GetOrientation());
                    float targetPitch_ee = (float)Math.Asin(-RT_ee.M32);
                    float targetYaw_ee;
                    float targetRoll_ee;
                    if (Math.Round(RT_ee.M32, 2) == -1)
                    {
                        targetRoll_ee = 0;
                        targetYaw_ee = (float)Math.Atan2(RT_ee.M21, RT_ee.M11);
                    }
                    else if (Math.Round(RT_ee.M32, 2) == 1)
                    {
                        targetRoll_ee = 0;
                        targetYaw_ee = (float)Math.Atan2(-RT_ee.M21, RT_ee.M11);
                    }
                    else
                    {
                        targetRoll_ee = (float)Math.Atan2(RT_ee.M12, RT_ee.M22);
                        targetYaw_ee = (float)Math.Atan2(RT_ee.M31, RT_ee.M33);
                    }

                    eeYawTargetAngle = targetYaw_ee;
                    if (eeYawTargetAngle < eeYawLowerLimit || eeYawTargetAngle > eeYawUpperLimit)
                    {
                        OOB = true;
                        goto OutOfBounds;
                    }
                    eePitchTargetAngle = targetPitch_ee;
                    if (eePitchTargetAngle < eePitchLowerLimit || eePitchTargetAngle > eePitchUpperLimit)
                    {
                        OOB = true;
                        goto OutOfBounds;
                    }
                    eeRollTargetAngle = targetRoll_ee;
                    if (eeRollTargetAngle < eeRollLowerLimit || eeRollTargetAngle > eeRollUpperLimit)
                    {
                        OOB = true;
                        goto OutOfBounds;
                    }

                    targetCoordPrev = targetCoord;

                    OutOfBounds:

                    if (OOB)
                    {
                        program.Echo("OOB");
                        targetCoord = targetCoordPrev;
                        OOB = false;
                    }

                    float joint1AngleError = joint1TargetAngle - joint1Angle;
                    joint1AngleError = Math.Abs(joint1AngleError) > (float)Math.PI ? (2 * (float)Math.PI - Math.Abs(joint1AngleError)) * -Math.Sign(joint1AngleError) : joint1AngleError;

                    float joint2AngleError = joint2TargetAngle - joint2Angle;
                    joint2AngleError = Math.Abs(joint2AngleError) > (float)Math.PI ? (2 * (float)Math.PI - Math.Abs(joint2AngleError)) * -Math.Sign(joint2AngleError) : joint2AngleError;

                    float eeYawAngleError = eeYawTargetAngle - eeYawAngle;
                    eeYawAngleError = Math.Abs(eeYawAngleError) > (float)Math.PI ? (2 * (float)Math.PI - Math.Abs(eeYawAngleError)) * -Math.Sign(eeYawAngleError) : eeYawAngleError;

                    float eePitchAngleError = eePitchTargetAngle - eePitchAngle;
                    eePitchAngleError = Math.Abs(eePitchAngleError) > (float)Math.PI ? (2 * (float)Math.PI - Math.Abs(eePitchAngleError)) * -Math.Sign(eePitchAngleError) : eePitchAngleError;

                    float eeRollAngleError = eeRollTargetAngle - eeRollAngle;
                    eeRollAngleError = Math.Abs(eeRollAngleError) > (float)Math.PI ? (2 * (float)Math.PI - Math.Abs(eeRollAngleError)) * -Math.Sign(eeRollAngleError) : eeRollAngleError;

                    if (!float.IsNaN(joint1AngleError) && !float.IsNaN(joint2AngleError) && !float.IsNaN(eeYawAngleError) && !float.IsNaN(eePitchAngleError) && !float.IsNaN(eeRollAngleError))
                    {
                        joint1Rotor.TargetVelocityRad = joint1RotorInv ? -(speed * joint1AngleError) : (speed * joint1AngleError);

                        joint2Rotor.TargetVelocityRad = joint2RotorInv ? -(speed * joint2AngleError) : (speed * joint2AngleError);

                        eeYawRotor.TargetVelocityRad = eeYawRotorInv ? -(speed * eeYawAngleError) : (speed * eeYawAngleError);

                        eePitchHinge.TargetVelocityRad = eePitchHingeInv ? -(speed * eePitchAngleError) : (speed * eePitchAngleError);

                        eeRollRotor.TargetVelocityRad = eeRollRotorInv ? -(speed * eeRollAngleError) : (speed * eeRollAngleError);

                    }
                    else
                    {
                        joint1Rotor.TargetVelocityRad = 0;

                        joint2Rotor.TargetVelocityRad = 0;

                        eeYawRotor.TargetVelocityRad = 0;

                        eePitchHinge.TargetVelocityRad = 0;

                        eeRollRotor.TargetVelocityRad = 0;
                    }
                }
            }
        }
    }
}
