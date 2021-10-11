using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

namespace Script
{
    // 스킬 쿨타임 계산 스크립트
    public class SkillManager : MonoSingleton<SkillManager>
    {
        private readonly List<Skill> m_Skills = new List<Skill>();

        public class Skill
        {
            public readonly Type name;
            public float coolTime;
            public readonly float maxTime;
            public bool isActive = true;

            public Skill(Type type, float time)
            {
                this.name = type;
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
            m_Skills.Add(new Skill(type, time));
        }

        public bool IsActive(Type type)
        {
            return (from skill in m_Skills where skill.name == type select skill.isActive).FirstOrDefault();
        }

        public Skill FindSkill(Type type)
        {
            return m_Skills.FirstOrDefault(value => value.name == type);
        }

        private void CheckCoolDown()
        {
            foreach (var skill in m_Skills)
            {
                if (skill.isActive == false)
                {
                    skill.coolTime -= Time.deltaTime;
                    if (skill.coolTime <= 0f)
                    {
                        skill.coolTime = skill.maxTime;
                        skill.isActive = true;
                    }
                }
            }
            
            
            foreach (var skill in m_Skills.Where(skill => skill.isActive == false))
            {
                skill.coolTime -= Time.deltaTime;
                if (skill.coolTime <= 0f)
                {
                    skill.coolTime = skill.maxTime;
                    skill.isActive = true;
                }
            }
        }
    }
}