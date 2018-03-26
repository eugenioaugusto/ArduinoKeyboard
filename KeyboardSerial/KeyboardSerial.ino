/*
  Keyboard test

  For the Arduino Leonardo, Micro or Due

  Reads a byte from the serial port, sends a keystroke back.
  The sent keystroke is one higher than what's received, e.g. if you send a,
  you get b, send A you get B, and so forth.

  The circuit:
  - none

  created 21 Oct 2011
  modified 27 Mar 2012
  by Tom Igoe

  This example code is in the public domain.

  http://www.arduino.cc/en/Tutorial/KeyboardSerial
*/

int i = 0;

bool lastIsZero = false;
void setup() {
  // open the serial port:
  Serial.begin(9600);
  pinMode(2, INPUT);
  pinMode(3, INPUT);
  pinMode(4, INPUT);
  pinMode(5, INPUT);
  pinMode(6, INPUT);
  pinMode(7, INPUT);
  pinMode(8, INPUT);
  pinMode(9, INPUT);
  pinMode(10, INPUT);
  pinMode(11, INPUT);
  pinMode(12, INPUT);
}

void loop() {
  String envio = "#";
  int digiRead = 0;
  bool isZero = false;
  for(int i=2;i<=12;i++)
  {
    digiRead = digitalRead(i);
    envio += digitalRead(i);
    isZero = isZero && digiRead == 0;
  }
  envio += "$";
  if( !isZero || !lastIsZero )
  {
    Serial.println(envio);
  }
  lastIsZero = isZero;
  delay(50);
  i++;
}
