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
        public class PIDControl
        {
            public float Kp { get; }
            public float Ki { get; }
            public float Kd { get; }

            public float integralValue = 0;
            public float priorValue = 0;

            public PIDControl(float Kp, float Ki, float Kd)
            {
                this.Kp = Kp;
                this.Ki = Ki;
                this.Kd = Kd;
            }

            public float Run(float input, float timeDelta)
            {
                if (timeDelta == 0 || input == 0)
                {
                    return input;
                }

                float differencial = (input - priorValue) / timeDelta;
                priorValue = input;
                float result = Kp * input + Ki * integralValue + Kd * differencial;

                return result;
            }

            public virtual void GetIntegral(float input, float timeDelta)
            {
                integralValue += (input * timeDelta);
            }

        }

        public class ClampedIntegralPIDControl : PIDControl
        {
            float lowerLimit;
            float upperLimit;
            public ClampedIntegralPIDControl(float Kp, float Ki, float Kd, float lowerLimit, float upperLimit) : base(Kp, Ki, Kd)
            {
                this.lowerLimit = lowerLimit;
                this.upperLimit = upperLimit;
            }

            public override void GetIntegral(float input, float timeDelta)
            {
                base.GetIntegral(input, timeDelta);
                integralValue = Math.Max(lowerLimit, Math.Min(upperLimit, integralValue));
            }
        }
    }
}
