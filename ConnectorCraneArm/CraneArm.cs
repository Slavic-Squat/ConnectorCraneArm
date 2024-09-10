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
            private float seg1BaseLength;
            private float seg2BaseLength;
            private float maxArmLength;
            private float minArmLength;
            private float currentArmLength;
            private IMyShipController controller;
            private IMyMotorAdvancedStator baseRotor;
            private bool baseRotorInv;
            private IMyMotorAdvancedStator seg1Rotor;
            private bool seg1RotorInv;
            private IMyMotorAdvancedStator seg2Rotor;
            private bool seg2RotorInv;
            private IMyMotorAdvancedStator eePitchHinge;
            private bool eePitchHingeInv;
            private IMyMotorAdvancedStator eeYawRotor;
            private bool eeYawRotorInv;
            private IMyMotorAdvancedStator eeRollRotor;
            private bool eeRollRotorInv;

            private float ZCoord = 0;
            private float YCoord = 0;
            private float XCoord = 0;

            private float baseRotorAngle;
            private float baseRotorLowerLimit;
            private float baseRotorUpperLimit;
            private float baseRotorTargetAngle;

            private float seg1RotorAngle;
            private float seg1RotorLowerLimit;
            private float seg1RotorUpperLimit;
            private float seg1RotorTargetAngle;

            private float seg2RotorAngle;
            private float seg2RotorLowerLimit;
            private float seg2RotorUpperLimit;
            private float seg2RotorTargetAngle;

            private float eePitchHingeAngle;
            private float eePitchHingeLowerLimit;
            private float eePitchHingeUpperLimit;
            private float eePitchHingeTargetAngle;

            private float eeYawRotorAngle;
            private float eeYawRotorLowerLimit;
            private float eeYawRotorUpperLimit;
            private float eeYawRotorTargetAngle;

            private float eeRollRotorAngle;
            private float eeRollRotorLowerLimit;
            private float eeRollRotorUpperLimit;
            private float eeRollRotorTargetAngle;

            private float targetYaw_base;
            private float targetPitch_base;
            private float targetRoll_base;

            private float sensitivity;
            private float speed;
            private bool cylindricalMode;
            private bool OOB = false;
            public bool eeControlled = true;
            public bool armControlled = true;

            public CraneArm(Program program, int ID, float seg1BaseLength, float seg2BaseLength, IMyShipController controller, float sensitivity, float speed, bool cylindricalMode)
            {
                this.program = program;
                this.ID = ID;
                this.seg1BaseLength = seg1BaseLength;
                this.seg2BaseLength = seg2BaseLength;
                this.controller = controller;

                baseRotor = (IMyMotorAdvancedStator)program.GridTerminalSystem.GetBlockWithName($"Crane Base Rotor {ID}");
                seg1Rotor = (IMyMotorAdvancedStator)program.GridTerminalSystem.GetBlockWithName($"Crane Segment One Rotor {ID}");
                seg2Rotor = (IMyMotorAdvancedStator)program.GridTerminalSystem.GetBlockWithName($"Crane Segment Two Rotor {ID}");
                eePitchHinge = (IMyMotorAdvancedStator)program.GridTerminalSystem.GetBlockWithName($"End Effector Pitch Hinge {ID}");
                eeYawRotor = (IMyMotorAdvancedStator)program.GridTerminalSystem.GetBlockWithName($"End Effector Yaw Rotor {ID}");
                eeRollRotor = (IMyMotorAdvancedStator)program.GridTerminalSystem.GetBlockWithName($"End Effector Roll Rotor {ID}");

                this.sensitivity = sensitivity;
                this.speed = speed;
                this.cylindricalMode = cylindricalMode;

                

                baseRotorInv = baseRotor.CustomData.Contains("Inverted");
                seg1RotorInv = seg1Rotor.CustomData.Contains("Inverted");
                seg2RotorInv = seg2Rotor.CustomData.Contains("Inverted");
                eePitchHingeInv = eePitchHinge.CustomData.Contains("Inverted");
                eeYawRotorInv = eeYawRotor.CustomData.Contains("Inverted");
                eeRollRotorInv = eeRollRotor.CustomData.Contains("Inverted");

                this.Init();         
            }

            public void Init()
            {
                baseRotorAngle = baseRotorInv ? -baseRotor.Angle : baseRotor.Angle;
                baseRotorAngle = Math.Abs(baseRotorAngle) > (float)Math.PI ? (2 * (float)Math.PI - Math.Abs(baseRotorAngle)) * -Math.Sign(baseRotorAngle) : baseRotorAngle;
                baseRotorLowerLimit = baseRotorInv ? -baseRotor.UpperLimitRad : baseRotor.LowerLimitRad;
                baseRotorUpperLimit = baseRotorInv ? -baseRotor.LowerLimitRad : baseRotor.UpperLimitRad;

                seg1RotorAngle = seg1RotorInv ? -seg1Rotor.Angle : seg1Rotor.Angle;
                seg1RotorAngle = Math.Abs(seg1RotorAngle) > (float)Math.PI ? (2 * (float)Math.PI - Math.Abs(seg1RotorAngle)) * -Math.Sign(seg1RotorAngle) : seg1RotorAngle;
                seg1RotorLowerLimit = seg1RotorInv ? -seg1Rotor.UpperLimitRad : seg1Rotor.LowerLimitRad;
                seg1RotorUpperLimit = seg1RotorInv ? -seg1Rotor.LowerLimitRad : seg1Rotor.UpperLimitRad;

                seg2RotorAngle = seg2RotorInv ? -seg2Rotor.Angle : seg2Rotor.Angle;
                seg2RotorAngle = Math.Abs(seg2RotorAngle) > (float)Math.PI ? (2 * (float)Math.PI - Math.Abs(seg2RotorAngle)) * -Math.Sign(seg2RotorAngle) : seg2RotorAngle;
                seg2RotorLowerLimit = seg2RotorInv ? -seg2Rotor.UpperLimitRad : seg2Rotor.LowerLimitRad;
                seg2RotorUpperLimit = seg2RotorInv ? -seg2Rotor.LowerLimitRad : seg2Rotor.UpperLimitRad;

                eePitchHingeAngle = eePitchHingeInv ? -eePitchHinge.Angle : eePitchHinge.Angle;
                eePitchHingeAngle += (float)Math.PI / 2;
                eePitchHingeAngle = Math.Abs(eePitchHingeAngle) > (float)Math.PI ? (2 * (float)Math.PI - Math.Abs(eePitchHingeAngle)) * -Math.Sign(eePitchHingeAngle) : eePitchHingeAngle;
                eePitchHingeLowerLimit = eePitchHingeInv ? -eePitchHinge.UpperLimitRad : eePitchHinge.LowerLimitRad;
                eePitchHingeLowerLimit += (float)Math.PI / 2;
                eePitchHingeUpperLimit = eePitchHingeInv ? -eePitchHinge.LowerLimitRad : eePitchHinge.UpperLimitRad;
                eePitchHingeUpperLimit += (float)Math.PI / 2;

                eeYawRotorAngle = eeYawRotorInv ? -eeYawRotor.Angle : eeYawRotor.Angle;
                eeYawRotorAngle = Math.Abs(eeYawRotorAngle) > (float)Math.PI ? (2 * (float)Math.PI - Math.Abs(eeYawRotorAngle)) * -Math.Sign(eeYawRotorAngle) : eeYawRotorAngle;
                eeYawRotorLowerLimit = eeYawRotorInv ? -eeYawRotor.UpperLimitRad : eeYawRotor.LowerLimitRad;
                eeYawRotorUpperLimit = eeYawRotorInv ? -eeYawRotor.LowerLimitRad : eeYawRotor.UpperLimitRad;

                eeRollRotorAngle = eeRollRotorInv ? -eeRollRotor.Angle : eeRollRotor.Angle;
                eeRollRotorAngle = Math.Abs(eeRollRotorAngle) > (float)Math.PI ? (2 * (float)Math.PI - Math.Abs(eeRollRotorAngle)) * -Math.Sign(eeRollRotorAngle) : eeRollRotorAngle;
                eeRollRotorLowerLimit = eeRollRotorInv ? -eeRollRotor.UpperLimitRad : eeRollRotor.LowerLimitRad;
                eeRollRotorUpperLimit = eeRollRotorInv ? -eeRollRotor.LowerLimitRad : eeRollRotor.UpperLimitRad;

                float seg1Length = seg1BaseLength;
                float seg2Length = seg2BaseLength;

                Matrix H1 = Matrix.CreateRotationY(baseRotorAngle);
                Matrix H2 = Matrix.CreateRotationX(seg1RotorAngle);
                Matrix H3 = Matrix.CreateRotationX(-seg2RotorAngle);
                H3.Translation = new Vector3(0, 0, -seg1Length);
                Matrix H4 = Matrix.CreateRotationX(-(float)Math.PI / 2);
                H4.Translation = new Vector3(0, 0, -seg2Length);

                if (cylindricalMode == false)
                {
                    Matrix HT = H4 * H3 * H2 * H1;
                    XCoord = HT.Translation.X;
                    YCoord = HT.Translation.Y;
                    ZCoord = HT.Translation.Z;

                    currentArmLength = (float)Math.Sqrt(ZCoord * ZCoord + YCoord * YCoord + XCoord * XCoord);

                    Matrix R1_ee = Matrix.CreateRotationY(eeYawRotorAngle);
                    Matrix R2_ee = Matrix.CreateRotationX(eePitchHingeAngle);
                    Matrix R3_ee = Matrix.CreateRotationZ(eeRollRotorAngle);
                    Matrix RT_ee = R3_ee * R2_ee * R1_ee;
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
                    Matrix HT = H4 * H3 * H2;
                    YCoord = HT.Translation.Y;
                    ZCoord = HT.Translation.Z;

                    currentArmLength = (float)Math.Sqrt(YCoord * YCoord + ZCoord * ZCoord);

                    Matrix R1_ee = Matrix.CreateRotationY(eeYawRotorAngle);
                    Matrix R2_ee = Matrix.CreateRotationX(eePitchHingeAngle);
                    Matrix R3_ee = Matrix.CreateRotationZ(eeRollRotorAngle);
                    Matrix RT_ee = R3_ee * R2_ee * R1_ee;
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
                baseRotorAngle = baseRotorInv ? -baseRotor.Angle : baseRotor.Angle;
                baseRotorAngle = Math.Abs(baseRotorAngle) > (float)Math.PI ? (2 * (float)Math.PI - Math.Abs(baseRotorAngle)) * -Math.Sign(baseRotorAngle) : baseRotorAngle;

                seg1RotorAngle = seg1RotorInv ? -seg1Rotor.Angle : seg1Rotor.Angle;
                seg1RotorAngle = Math.Abs(seg1RotorAngle) > (float)Math.PI ? (2 * (float)Math.PI - Math.Abs(seg1RotorAngle)) * -Math.Sign(seg1RotorAngle) : seg1RotorAngle;

                seg2RotorAngle = seg2RotorInv ? -seg2Rotor.Angle : seg2Rotor.Angle;
                seg2RotorAngle = Math.Abs(seg2RotorAngle) > (float)Math.PI ? (2 * (float)Math.PI - Math.Abs(seg2RotorAngle)) * -Math.Sign(seg2RotorAngle) : seg2RotorAngle;

                eePitchHingeAngle = eePitchHingeInv ? -eePitchHinge.Angle : eePitchHinge.Angle;
                eePitchHingeAngle += (float)Math.PI / 2;
                eePitchHingeAngle = Math.Abs(eePitchHingeAngle) > (float)Math.PI ? (2 * (float)Math.PI - Math.Abs(eePitchHingeAngle)) * -Math.Sign(eePitchHingeAngle) : eePitchHingeAngle;

                eeYawRotorAngle = eeYawRotorInv ? -eeYawRotor.Angle : eeYawRotor.Angle;
                eeYawRotorAngle = Math.Abs(eeYawRotorAngle) > (float)Math.PI ? (2 * (float)Math.PI - Math.Abs(eeYawRotorAngle)) * -Math.Sign(eeYawRotorAngle) : eeYawRotorAngle;

                eeRollRotorAngle = eeRollRotorInv ? -eeRollRotor.Angle : eeRollRotor.Angle;
                eeRollRotorAngle = Math.Abs(eeRollRotorAngle) > (float)Math.PI ? (2 * (float)Math.PI - Math.Abs(eeRollRotorAngle)) * -Math.Sign(eeRollRotorAngle) : eeRollRotorAngle;

                float seg1Length = seg1BaseLength;
                float seg2Length = seg2BaseLength;

                maxArmLength = seg1Length + seg2Length;
                minArmLength = Math.Abs(seg2Length - seg1Length) + 1;

                Matrix H1 = Matrix.CreateRotationY(baseRotorAngle);
                Matrix H2 = Matrix.CreateRotationX(seg1RotorAngle);
                Matrix H3 = Matrix.CreateRotationX(-seg2RotorAngle);
                H3.Translation = new Vector3(0, 0, -seg1Length);
                Matrix H4 = Matrix.CreateRotationX(-(float)Math.PI / 2);
                H4.Translation = new Vector3(0, 0, -seg2Length);
                
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
                        ZCoord += sensitivity * controller.MoveIndicator.Z;
                        currentArmLength = (float)Math.Sqrt(ZCoord * ZCoord + YCoord * YCoord + XCoord * XCoord);
                        if (currentArmLength > maxArmLength)
                        {
                            currentArmLength = maxArmLength;
                            ZCoord = (float)Math.Sqrt(maxArmLength * maxArmLength - YCoord * YCoord - XCoord * XCoord) * (float)Math.Sign(ZCoord);
                        }
                        else if (currentArmLength < minArmLength)
                        {
                            currentArmLength = minArmLength;
                            ZCoord = (float)Math.Sqrt(minArmLength * minArmLength - YCoord * YCoord - XCoord * XCoord) * (float)Math.Sign(ZCoord);
                        }
                    }

                    if (controller.MoveIndicator.Y != 0)
                    {
                        YCoord += sensitivity * controller.MoveIndicator.Y;
                        currentArmLength = (float)Math.Sqrt(ZCoord * ZCoord + YCoord * YCoord + XCoord * XCoord);
                        if (currentArmLength > maxArmLength)
                        {
                            currentArmLength = maxArmLength;
                            YCoord = (float)Math.Sqrt(maxArmLength * maxArmLength - ZCoord * ZCoord - XCoord * XCoord) * Math.Sign(YCoord);
                        }
                        else if (currentArmLength < minArmLength)
                        {
                            currentArmLength = minArmLength;
                            YCoord = (float)Math.Sqrt(minArmLength * minArmLength - ZCoord * ZCoord - XCoord * XCoord) * Math.Sign(YCoord);
                        }
                    }

                    if (controller.MoveIndicator.X != 0)
                    {
                        XCoord += sensitivity * controller.MoveIndicator.X;
                        currentArmLength = (float)Math.Sqrt(ZCoord * ZCoord + YCoord * YCoord + XCoord * XCoord);
                        if (currentArmLength > maxArmLength)
                        {
                            currentArmLength = maxArmLength;
                            XCoord = (float)Math.Sqrt(maxArmLength * maxArmLength - YCoord * YCoord - ZCoord * ZCoord) * Math.Sign(XCoord);
                        }
                        else if (currentArmLength < minArmLength)
                        {
                            currentArmLength = minArmLength;
                            XCoord = (float)Math.Sqrt(minArmLength * minArmLength - YCoord * YCoord - ZCoord * ZCoord) * Math.Sign(XCoord);
                        }
                    }

                    currentArmLength = (float)Math.Sqrt(ZCoord * ZCoord + YCoord * YCoord + XCoord * XCoord);

                    Matrix HT = H4 * H3 * H2 * H1;

                    Matrix R1_base = Matrix.CreateRotationY(targetYaw_base);
                    Matrix R2_base = Matrix.CreateRotationX(targetPitch_base);
                    Matrix R3_base = Matrix.CreateRotationZ(targetRoll_base);
                    Matrix RT_base = R3_base * R2_base * R1_base;
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

                    if (currentArmLength > maxArmLength || currentArmLength < minArmLength)
                    {
                        XCoord = HT.Translation.X;
                        YCoord = HT.Translation.Y;
                        ZCoord = HT.Translation.Z;

                        currentArmLength = (float)Math.Sqrt(ZCoord * ZCoord + YCoord * YCoord + XCoord * XCoord);

                    }
                    
                    baseRotorTargetAngle = (float)Math.Atan2(-XCoord, -ZCoord);
                    if (baseRotorTargetAngle < baseRotorLowerLimit)
                    {
                        baseRotorTargetAngle = baseRotorLowerLimit;
                        OOB = true;
                    }
                    else if (baseRotorTargetAngle > baseRotorUpperLimit)
                    {
                        baseRotorTargetAngle = baseRotorUpperLimit;
                        OOB = true;
                    }
                    seg1RotorTargetAngle = (float)Math.Acos((currentArmLength * currentArmLength + seg1Length * seg1Length - seg2Length * seg2Length) / (2 * currentArmLength * seg1Length)) + (float)Math.Asin(YCoord / currentArmLength);
                    if (seg1RotorTargetAngle < seg1RotorLowerLimit)
                    {
                        seg1RotorTargetAngle = seg1RotorLowerLimit;
                        OOB = true;
                    }
                    else if (seg1RotorTargetAngle > seg1RotorUpperLimit)
                    {
                        seg1RotorTargetAngle = seg1RotorUpperLimit;
                        OOB = true;
                    }
                    seg2RotorTargetAngle = (float)Math.PI - (float)Math.Acos((seg2Length * seg2Length + seg1Length * seg1Length - currentArmLength * currentArmLength) / (2 * seg2Length * seg1Length));
                    if (seg2RotorTargetAngle < seg2RotorLowerLimit)
                    {
                        seg2RotorTargetAngle = seg2RotorLowerLimit;
                        OOB = true;
                    }
                    else if (seg2RotorTargetAngle > seg2RotorUpperLimit)
                    {
                        seg2RotorTargetAngle = seg2RotorUpperLimit;
                        OOB = true;
                    }
                    eeYawRotorTargetAngle = targetYaw_ee;
                    if (eeYawRotorTargetAngle < eeYawRotorLowerLimit)
                    {
                        eeYawRotorTargetAngle = eeYawRotorLowerLimit;
                        OOB = true;
                    }
                    else if (eeYawRotorTargetAngle > eeYawRotorUpperLimit)
                    {
                        eeYawRotorTargetAngle = eeYawRotorUpperLimit;
                        OOB = true;
                    }
                    eePitchHingeTargetAngle = targetPitch_ee;
                    if (eePitchHingeTargetAngle < eePitchHingeLowerLimit)
                    {
                        eePitchHingeTargetAngle = eePitchHingeLowerLimit;
                        OOB = true;
                    }
                    else if (eePitchHingeTargetAngle > eePitchHingeUpperLimit)
                    {
                        eePitchHingeTargetAngle = eePitchHingeUpperLimit;
                        OOB = true;
                    }
                    eeRollRotorTargetAngle = targetRoll_ee;
                    if (eeRollRotorTargetAngle < eeRollRotorLowerLimit)
                    {
                        eeRollRotorTargetAngle = eeRollRotorLowerLimit;
                        OOB = true;
                    }
                    else if (eeRollRotorTargetAngle > eeRollRotorUpperLimit)
                    {
                        eeRollRotorTargetAngle = eeRollRotorUpperLimit;
                        OOB = true;
                    }
                    

                    
                    if (OOB)
                    {
                        XCoord = HT.Translation.X;
                        YCoord = HT.Translation.Y;
                        ZCoord = HT.Translation.Z;

                        currentArmLength = (float)Math.Sqrt(ZCoord * ZCoord + YCoord * YCoord + XCoord * XCoord);

                        Matrix R1_ee = Matrix.CreateRotationY(eeYawRotorAngle);
                        Matrix R2_ee = Matrix.CreateRotationX(eePitchHingeAngle);
                        Matrix R3_ee = Matrix.CreateRotationZ(eeRollRotorAngle);
                        RT_ee = R3_ee * R2_ee * R1_ee;
                        RT_base = RT_ee * HT;

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

                        eeYawRotorTargetAngle = eeYawRotorAngle;
                        eePitchHingeTargetAngle = eePitchHingeAngle;
                        eeRollRotorTargetAngle = eeRollRotorAngle;

                        baseRotorTargetAngle = baseRotorAngle;
                        seg1RotorTargetAngle = seg1RotorAngle;
                        seg2RotorTargetAngle = seg2RotorAngle;

                        OOB = false;
                    }
                    

                    float baseRotorAngleError = baseRotorTargetAngle - baseRotorAngle;
                    baseRotorAngleError = Math.Abs(baseRotorAngleError) > (float)Math.PI ? (2 * (float)Math.PI - Math.Abs(baseRotorAngleError)) * -Math.Sign(baseRotorAngleError) : baseRotorAngleError;

                    float seg1RotorAngleError = seg1RotorTargetAngle - seg1RotorAngle;
                    seg1RotorAngleError = Math.Abs(seg1RotorAngleError) > (float)Math.PI ? (2 * (float)Math.PI - Math.Abs(seg1RotorAngleError)) * -Math.Sign(seg1RotorAngleError) : seg1RotorAngleError;

                    float seg2RotorAngleError = seg2RotorTargetAngle - seg2RotorAngle;
                    seg2RotorAngleError = Math.Abs(seg2RotorAngleError) > (float)Math.PI ? (2 * (float)Math.PI - Math.Abs(seg2RotorAngleError)) * -Math.Sign(seg2RotorAngleError) : seg2RotorAngleError;

                    float eeYawRotorAngleError = eeYawRotorTargetAngle - eeYawRotorAngle;
                    eeYawRotorAngleError = Math.Abs(eeYawRotorAngleError) > (float)Math.PI ? (2 * (float)Math.PI - Math.Abs(eeYawRotorAngleError)) * -Math.Sign(eeYawRotorAngleError) : eeYawRotorAngleError;

                    float eePitchHingeAngleError = eePitchHingeTargetAngle - eePitchHingeAngle;
                    eePitchHingeAngleError = Math.Abs(eePitchHingeAngleError) > (float)Math.PI ? (2 * (float)Math.PI - Math.Abs(eePitchHingeAngleError)) * -Math.Sign(eePitchHingeAngleError) : eePitchHingeAngleError;

                    float eeRollRotorAngleError = eeRollRotorTargetAngle - eeRollRotorAngle;
                    eeRollRotorAngleError = Math.Abs(eeRollRotorAngleError) > (float)Math.PI ? (2 * (float)Math.PI - Math.Abs(eeRollRotorAngleError)) * -Math.Sign(eeRollRotorAngleError) : eeRollRotorAngleError;

                    if (!float.IsNaN(seg1RotorAngleError) && !float.IsNaN(seg2RotorAngleError))
                    {
                        baseRotor.TargetVelocityRad = baseRotorInv ? -(speed * baseRotorAngleError) : (speed * baseRotorAngleError);

                        seg1Rotor.TargetVelocityRad = seg1RotorInv ? -(speed * seg1RotorAngleError) : (speed * seg1RotorAngleError);

                        seg2Rotor.TargetVelocityRad = seg2RotorInv ? -(speed * seg2RotorAngleError) : (speed * seg2RotorAngleError);

                        eeYawRotor.TargetVelocityRad = eeYawRotorInv ? -(speed * eeYawRotorAngleError) : (speed * eeYawRotorAngleError);

                        eePitchHinge.TargetVelocityRad = eePitchHingeInv ? -(speed * eePitchHingeAngleError) : (speed * eePitchHingeAngleError);

                        eeRollRotor.TargetVelocityRad = eeRollRotorInv ? -(speed * eeRollRotorAngleError) : (speed * eeRollRotorAngleError);

                    }
                    else
                    {
                        baseRotor.TargetVelocityRad = 0;

                        seg1Rotor.TargetVelocityRad = 0;

                        seg2Rotor.TargetVelocityRad = 0;

                        eeYawRotor.TargetVelocityRad = 0;

                        eePitchHinge.TargetVelocityRad = 0;

                        eeRollRotor.TargetVelocityRad = 0;
                    }

                }
                else
                {
                    if (controller.MoveIndicator.X != 0)
                    {
                        baseRotor.TargetVelocityRad = 0.1f * speed * controller.MoveIndicator.X;
                    }
                    else
                    {
                        baseRotor.TargetVelocityRad = 0;
                    }

                    if (controller.MoveIndicator.Y != 0)
                    {
                        YCoord += sensitivity * controller.MoveIndicator.Y;
                        currentArmLength = (float)Math.Sqrt(YCoord * YCoord + ZCoord * ZCoord);
                        if (currentArmLength > maxArmLength)
                        {
                            currentArmLength = maxArmLength;
                            YCoord = (float)Math.Sqrt(maxArmLength * maxArmLength - ZCoord * ZCoord) * Math.Sign(YCoord);
                        }
                        else if (currentArmLength < minArmLength)
                        {
                            currentArmLength = minArmLength;
                            YCoord = (float)Math.Sqrt(minArmLength * minArmLength - ZCoord * ZCoord) * Math.Sign(YCoord);
                        }
                    }

                    if (controller.MoveIndicator.Z != 0)
                    {
                        ZCoord += sensitivity * controller.MoveIndicator.Z;
                        currentArmLength = (float)Math.Sqrt(YCoord * YCoord + ZCoord * ZCoord);
                        if (currentArmLength > maxArmLength)
                        {
                            currentArmLength = maxArmLength;
                            ZCoord = (float)Math.Sqrt(maxArmLength * maxArmLength - YCoord * YCoord) * Math.Sign(ZCoord);
                        }
                        else if (currentArmLength < minArmLength)
                        {
                            currentArmLength = minArmLength;
                            ZCoord = (float)Math.Sqrt(minArmLength * minArmLength - YCoord * YCoord) * Math.Sign(ZCoord);
                        }
                    }

                    currentArmLength = (float)Math.Sqrt(YCoord * YCoord + ZCoord * ZCoord);

                    Matrix HT = H4 * H3 * H2;

                    Matrix R1_base = Matrix.CreateRotationY(targetYaw_base);
                    Matrix R2_base = Matrix.CreateRotationX(targetPitch_base);
                    Matrix R3_base = Matrix.CreateRotationZ(targetRoll_base);
                    Matrix RT_base = R3_base * R2_base * R1_base;
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

                    if (currentArmLength > maxArmLength || currentArmLength < minArmLength)
                    {
                        YCoord = HT.Translation.Y;
                        ZCoord = HT.Translation.Z;

                        currentArmLength = (float)Math.Sqrt(YCoord * YCoord + ZCoord * ZCoord);

                    }

                    seg1RotorTargetAngle = (float)Math.Acos((currentArmLength * currentArmLength + seg1Length * seg1Length - seg2Length * seg2Length) / (2 * currentArmLength * seg1Length)) + (float)Math.Asin(YCoord / currentArmLength);
                    if (seg1RotorTargetAngle < seg1RotorLowerLimit)
                    {
                        seg1RotorTargetAngle = seg1RotorLowerLimit;
                        OOB = true;
                    }
                    else if (seg1RotorTargetAngle > seg1RotorUpperLimit)
                    {
                        seg1RotorTargetAngle = seg1RotorUpperLimit;
                        OOB = true;
                    }
                    seg2RotorTargetAngle = (float)Math.PI - (float)Math.Acos((seg2Length * seg2Length + seg1Length * seg1Length - currentArmLength * currentArmLength) / (2 * seg2Length * seg1Length));
                    if (seg2RotorTargetAngle < seg2RotorLowerLimit)
                    {
                        seg2RotorTargetAngle = seg2RotorLowerLimit;
                        OOB = true;
                    }
                    else if (seg2RotorTargetAngle > seg2RotorUpperLimit)
                    {
                        seg2RotorTargetAngle = seg2RotorUpperLimit;
                        OOB = true;
                    }
                    eeYawRotorTargetAngle = targetYaw_ee;
                    if (eeYawRotorTargetAngle < eeYawRotorLowerLimit)
                    {
                        eeYawRotorTargetAngle = eeYawRotorLowerLimit;
                        OOB = true;
                    }
                    else if (eeYawRotorTargetAngle > eeYawRotorUpperLimit)
                    {
                        eeYawRotorTargetAngle = eeYawRotorUpperLimit;
                        OOB = true;
                    }
                    eePitchHingeTargetAngle = targetPitch_ee;
                    if (eePitchHingeTargetAngle < eePitchHingeLowerLimit)
                    {
                        eePitchHingeTargetAngle = eePitchHingeLowerLimit;
                        OOB = true;
                    }
                    else if (eePitchHingeTargetAngle > eePitchHingeUpperLimit)
                    {
                        eePitchHingeTargetAngle = eePitchHingeUpperLimit;
                        OOB = true;
                    }
                    eeRollRotorTargetAngle = targetRoll_ee;
                    if (eeRollRotorTargetAngle < eeRollRotorLowerLimit)
                    {
                        eeRollRotorTargetAngle = eeRollRotorLowerLimit;
                        OOB = true;
                    }
                    else if (eeRollRotorTargetAngle > eeRollRotorUpperLimit)
                    {
                        eeRollRotorTargetAngle = eeRollRotorUpperLimit;
                        OOB = true;
                    }

                    if (OOB)
                    {
                        YCoord = HT.Translation.Y;
                        ZCoord = HT.Translation.Z;

                        currentArmLength = (float)Math.Sqrt(YCoord * YCoord + ZCoord * ZCoord);

                        Matrix R1_ee = Matrix.CreateRotationY(eeYawRotorAngle);
                        Matrix R2_ee = Matrix.CreateRotationX(eePitchHingeAngle);
                        Matrix R3_ee = Matrix.CreateRotationZ(eeRollRotorAngle);
                        RT_ee = R3_ee * R2_ee * R1_ee;
                        RT_base = RT_ee * HT;

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

                        eeYawRotorTargetAngle = eeYawRotorAngle;
                        eePitchHingeTargetAngle = eePitchHingeAngle;
                        eeRollRotorTargetAngle = eeRollRotorAngle;

                        seg1RotorTargetAngle = seg1RotorAngle;
                        seg2RotorTargetAngle = seg2RotorAngle;

                        OOB = false;

                    }

                    float seg1RotorAngleError = seg1RotorTargetAngle - seg1RotorAngle;
                    seg1RotorAngleError = Math.Abs(seg1RotorAngleError) > (float)Math.PI ? (2 * (float)Math.PI - Math.Abs(seg1RotorAngleError)) * -Math.Sign(seg1RotorAngleError) : seg1RotorAngleError;

                    float seg2RotorAngleError = seg2RotorTargetAngle - seg2RotorAngle;
                    seg2RotorAngleError = Math.Abs(seg2RotorAngleError) > (float)Math.PI ? (2 * (float)Math.PI - Math.Abs(seg2RotorAngleError)) * -Math.Sign(seg2RotorAngleError) : seg2RotorAngleError;

                    float eeYawRotorAngleError = eeYawRotorTargetAngle - eeYawRotorAngle;
                    eeYawRotorAngleError = Math.Abs(eeYawRotorAngleError) > (float)Math.PI ? (2 * (float)Math.PI - Math.Abs(eeYawRotorAngleError)) * -Math.Sign(eeYawRotorAngleError) : eeYawRotorAngleError;

                    float eePitchHingeAngleError = eePitchHingeTargetAngle - eePitchHingeAngle;
                    eePitchHingeAngleError = Math.Abs(eePitchHingeAngleError) > (float)Math.PI ? (2 * (float)Math.PI - Math.Abs(eePitchHingeAngleError)) * -Math.Sign(eePitchHingeAngleError) : eePitchHingeAngleError;

                    float eeRollRotorAngleError = eeRollRotorTargetAngle - eeRollRotorAngle;
                    eeRollRotorAngleError = Math.Abs(eeRollRotorAngleError) > (float)Math.PI ? (2 * (float)Math.PI - Math.Abs(eeRollRotorAngleError)) * -Math.Sign(eeRollRotorAngleError) : eeRollRotorAngleError;

                    if (!float.IsNaN(seg1RotorAngleError) && !float.IsNaN(seg2RotorAngleError))
                    {
                        seg1Rotor.TargetVelocityRad = seg1RotorInv ? -(speed * seg1RotorAngleError) : (speed * seg1RotorAngleError);

                        seg2Rotor.TargetVelocityRad = seg2RotorInv ? -(speed * seg2RotorAngleError) : (speed * seg2RotorAngleError);

                        eeYawRotor.TargetVelocityRad = eeYawRotorInv ? -(speed * eeYawRotorAngleError) : (speed * eeYawRotorAngleError);

                        eePitchHinge.TargetVelocityRad = eePitchHingeInv ? -(speed * eePitchHingeAngleError) : (speed * eePitchHingeAngleError);

                        eeRollRotor.TargetVelocityRad = eeRollRotorInv ? -(speed * eeRollRotorAngleError) : (speed * eeRollRotorAngleError);

                    }
                    else
                    {
                        seg1Rotor.TargetVelocityRad = 0;

                        seg2Rotor.TargetVelocityRad = 0;

                        eeYawRotor.TargetVelocityRad = 0;

                        eePitchHinge.TargetVelocityRad = 0;

                        eeRollRotor.TargetVelocityRad = 0;
                    }
                }
            }
        }
    }
}
