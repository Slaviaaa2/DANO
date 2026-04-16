namespace DANO.Events
{
    /// <summary>プレイヤーがスプリントを開始/終了したときのイベント</summary>
    public class PlayerSprintChangedEvent
    {
        public API.Player Player { get; }
        /// <summary>スプリント中かどうか（true = 開始, false = 終了）</summary>
        public bool IsSprinting { get; }

        internal PlayerSprintChangedEvent(API.Player player, bool isSprinting)
        {
            Player = player;
            IsSprinting = isSprinting;
        }
    }

    /// <summary>プレイヤーがしゃがみを開始/終了したときのイベント</summary>
    public class PlayerCrouchChangedEvent
    {
        public API.Player Player { get; }
        /// <summary>しゃがみ中かどうか（true = 開始, false = 終了）</summary>
        public bool IsCrouching { get; }

        internal PlayerCrouchChangedEvent(API.Player player, bool isCrouching)
        {
            Player = player;
            IsCrouching = isCrouching;
        }
    }

    /// <summary>プレイヤーがスライディングを開始/終了したときのイベント</summary>
    public class PlayerSlideChangedEvent
    {
        public API.Player Player { get; }
        /// <summary>スライディング中かどうか（true = 開始, false = 終了）</summary>
        public bool IsSliding { get; }

        internal PlayerSlideChangedEvent(API.Player player, bool isSliding)
        {
            Player = player;
            IsSliding = isSliding;
        }
    }

    /// <summary>プレイヤーの接地状態が変化したときのイベント</summary>
    public class PlayerGroundedChangedEvent
    {
        public API.Player Player { get; }
        /// <summary>接地しているかどうか（true = 着地, false = 離陸）</summary>
        public bool IsGrounded { get; }

        internal PlayerGroundedChangedEvent(API.Player player, bool isGrounded)
        {
            Player = player;
            IsGrounded = isGrounded;
        }
    }

    /// <summary>プレイヤーがリーンを開始/終了したときのイベント</summary>
    public class PlayerLeanChangedEvent
    {
        public API.Player Player { get; }
        /// <summary>リーン中かどうか（true = 開始, false = 終了）</summary>
        public bool IsLeaning { get; }

        internal PlayerLeanChangedEvent(API.Player player, bool isLeaning)
        {
            Player = player;
            IsLeaning = isLeaning;
        }
    }

    /// <summary>プレイヤーがエイム（ADS）を開始/終了したときのイベント</summary>
    public class PlayerAimChangedEvent
    {
        public API.Player Player { get; }
        /// <summary>エイム中かどうか（true = 開始, false = 終了）</summary>
        public bool IsAiming { get; }

        internal PlayerAimChangedEvent(API.Player player, bool isAiming)
        {
            Player = player;
            IsAiming = isAiming;
        }
    }
}
