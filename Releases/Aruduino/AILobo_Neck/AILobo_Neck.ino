#include <Servo.h>
#include <SoftwareSerial.h>

SoftwareSerial BT(10, 11); // RX, TX

const int NUM_SERVOS = 2;

Servo servos[NUM_SERVOS];

const int SERVO_PINS[NUM_SERVOS] = {
  9, // #0 Yaw
  6  // #1 Pitch
};

String inputString = "";

void setup()
{
  Serial.begin(9600);
  BT.begin(9600);

  servos[0].attach(SERVO_PINS[0]);
  servos[1].attach(SERVO_PINS[1]);

  inputString.reserve(32);

  Serial.println("AILobo_Neck Ready");
  BT.println("AILobo_Neck Ready");
}

void loop()
{
  readBluetooth();
  readSerial();
}

void readBluetooth()
{
  while (BT.available())
  {
    char c = (char)BT.read();
    handleChar(c);
  }
}

void readSerial()
{
  while (Serial.available())
  {
    char c = (char)Serial.read();
    handleChar(c);
  }
}

void handleChar(char c)
{
  if (c == '\n')
  {
    parseAndExecute(inputString);
    inputString = "";
  }
  else if (c != '\r')
  {
    inputString += c;
  }
}

void parseAndExecute(String command)
{
  command.trim();

  if (!command.startsWith("#"))
    return;

  int pIndex = command.indexOf('P');
  if (pIndex < 0)
    return;

  int servoIndex = command.substring(1, pIndex).toInt();
  int angle = command.substring(pIndex + 1).toInt();

  if (servoIndex < 0 || servoIndex >= NUM_SERVOS)
    return;

  angle = constrain(angle, 0, 180);

  servos[servoIndex].write(angle);

  Serial.print("OK #");
  Serial.print(servoIndex);
  Serial.print(" P");
  Serial.println(angle);
}
