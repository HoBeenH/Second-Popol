using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Script
{
    // 스킬 쿨타임 계산 스크립트
    public class SkillManager : MonoSingleton<SkillManager>
    {
        private readonly List<Skill> m_SkillList = new List<Skill>();
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
        private void Update() => CheckCoolDown();

        // 현재 활성화 되지 않은 스킬만 쿨타임을 계산
        private void CheckCoolDown()
        {
            foreach (var skill in m_SkillList.Where(skill => skill.isActive == false))
            {
                skill.coolTime -= Time.deltaTime;
                if (skill.coolTime <= 0f)
                {
                    skill.coolTime = skill.maxTime;
                    skill.isActive = true;
                }
            }
        }

        // 스킬 추가
        public void AddSkill(Type type, float time) => m_SkillList.Add(new Skill(type, time));
        // 스킬 사용
        public Skill FindSkill(Type type) => m_SkillList.FirstOrDefault(value => value.name == type);
    }
}