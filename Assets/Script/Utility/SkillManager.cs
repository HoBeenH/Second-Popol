using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Script
{
    // 스킬 쿨타임 계산 스크립트
    public class SkillManager : MonoSingleton<SkillManager>
    {
        private readonly List<CoolDown> m_Skills = new List<CoolDown>();

        public class CoolDown
        {
            public readonly Type skill;
            public bool BIsActive = true;
            public float coolTime;
            public readonly float maxTime;

            public CoolDown(Type type, float time)
            {
                this.skill = type;
                this.coolTime = time;
                this.maxTime = time;
            }
        }

        private void Update()
        {
            CheckCoolDown();
        }

        public void AddSkill(Type type, float time)
        {
            m_Skills.Add(new CoolDown(type, time));
        }

        public CoolDown FindSkill(Type type)
        {
            return m_Skills.FirstOrDefault(value => value.skill == type);
        }

        private void CheckCoolDown()
        {
            foreach (var skill in m_Skills.Where(skill => skill.BIsActive == false))
            {
                skill.coolTime -= Time.deltaTime;
                if (skill.coolTime <= 0f)
                {
                    skill.coolTime = skill.maxTime;
                    skill.BIsActive = true;
                }
            }
        }
    }
}