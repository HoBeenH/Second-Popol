using Script.Player;

namespace Script
{
    #region DefaultStatus

    
    public class Status
    {
        public float rotSpeed;
        public int health;
        public int damage;
        public int skillDamage;
        public float moveSpeed;
        public float moveAnimDamp;
    }

    #endregion

    #region PlayerStatus

    public class PlayerStatus : Status
    {
        public PlayerStatus()
        {
            this.rotSpeed = 10f;
            this.skillDamage = 10;
            this.damage = 10;
            this.moveSpeed = 2f;
            this.health = 100;
        }
    }

    #endregion

    #region DragonStatus

    public class DragonStatus : Status
    {
        public int magicDefence = 3;
        public int defence = 3;

        public DragonStatus()
        {
            this.rotSpeed = 2f;
            this.health = 200;
            this.damage = 5;
            this.moveSpeed = 3.5f;
        }
    }
    #endregion
    
}