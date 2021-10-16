using Script.Player;

namespace Script
{
    #region DefaultStatus

    
    public class Status
    {
        public float rotSpeed;
        public int health;
        public int damage;
        public float moveSpeed;
    }

    #endregion

    #region PlayerStatus

    public class PlayerStatus : Status
    {
        public PlayerStatus()
        {
            this.rotSpeed = 10f;
            this.damage = 10;
            this.moveSpeed = 2f;
            this.health = 100;
        }
    }

    #endregion

    #region DragonStatus

    public class DragonStatus : Status
    {
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