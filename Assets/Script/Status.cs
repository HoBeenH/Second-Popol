using System.Collections;
using System.Runtime.CompilerServices;
using Script.Dragon;
using UnityEngine;

namespace Script
{
    #region DefaultStatus

    public class Status
    {
        public float rotSpeed;
        public int currentHealth;
        public int maxHealth;
        public int damage;
        public int skillDamage;
        public float moveSpeed;
        public float moveAnimDamp;

        public int Health
        {
            get => currentHealth;
            set
            {
                currentHealth = value;
                if (currentHealth > maxHealth)
                {
                    currentHealth = maxHealth;
                }
            }
        }
    }

    #endregion

    #region PlayerStatus

    public class PlayerStatus : Status
    {
        public PlayerStatus()
        {
            this.rotSpeed = 20f;
            this.moveAnimDamp = 0.05f;
            this.skillDamage = 10;
            this.maxHealth = 100;
            this.damage = 10;
            this.moveSpeed = 2f;
            this.Health = maxHealth;
        }
    }

    #endregion

    #region DragonStatus

    public class DragonStatus : Status
    {
        private readonly WaitForSeconds m_RecoverySpeed = new WaitForSeconds(0.5f);
 
        public float animSpeed = 1.0f;
        public int magicDefence = 3;
        public int defence = 3;
        public int recovery = 1;

        public DragonStatus()
        {
            this.rotSpeed = 2f;
            this.moveAnimDamp = 0.1f;
            this.maxHealth = 200;
            this.skillDamage = 5;
            this.damage = 5;
            this.moveSpeed = 3.5f;
            this.currentHealth = maxHealth;
        }

        public IEnumerator DragonRecovery()
        {
            while (DragonController.Instance.currentPhaseFlag != EDragonPhaseFlag.Dead)
            {
                yield return m_RecoverySpeed;
                Health += recovery;
            }
        }
    }
    #endregion
}