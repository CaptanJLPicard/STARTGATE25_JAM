using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    public Vector3 direction;
    public Vector2 mouseDelta;
    public NetworkBool isJumping;
    public NetworkBool isSprinting;
    public NetworkBool isFreeze;
    public NetworkBool isPunching;
}
