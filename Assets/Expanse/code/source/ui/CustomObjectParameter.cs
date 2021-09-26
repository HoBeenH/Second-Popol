using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using UnityEngine.Rendering;

namespace Expanse
{

    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public class CustomObjectParameter<T> : VolumeParameter<T>
    {
        const string kEnabledParameter = "enabled";

        public sealed override bool overrideState
        {
            get => true;
            set => m_OverrideState = true;
        }

        public sealed override T value
        {
            get => m_Value;
            set
            {
                m_Value = value;
            }
        }

        public CustomObjectParameter(T value)
        {
            m_OverrideState = true;
            this.value = value;
        }

        public override void Interp(T from, T to, float t)
        {

            if (m_Value == null)
                return;

            FieldInfo[] fields = typeof(T).GetFields();
            foreach (FieldInfo field in fields)
            {
                // TODO: should probably find a way to use actual interpolation
                // function instead of just directly setting.
                VolumeParameter toParam = (VolumeParameter)field.GetValue(to);
                VolumeParameter myParam = (VolumeParameter)field.GetValue(m_Value);
                myParam.SetValue(toParam);
                myParam.overrideState = toParam.overrideState;
                if (field.Name == kEnabledParameter)
                {
                    /* If we have an enabled parameter, don't do anything unless
                     * it is set to true. This gives us a sizeable performance
                     * boost in the majority of cases where only a few things
                     * are enabled. */
                    BoolParameter enabledParam = (BoolParameter)field.GetValue(to);
                    if (!enabledParam.value)
                    {
                        break;
                    }
                }
            }
        }
    };

} // namespace Expanse
