using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICommandSender
{
    void SendServo(int servoIndex, int angle);
}
