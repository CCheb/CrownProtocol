using Godot;
using System;

public partial interface IEnemy
{
    public void Hit(float damageReceived, long senderId = -1, long receiverId = -1);
}
