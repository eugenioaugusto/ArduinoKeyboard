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
  pinMode(2, INPUT_PULLUP);
  pinMode(3, INPUT_PULLUP);
  pinMode(4, INPUT_PULLUP);
  pinMode(5, INPUT_PULLUP);
  pinMode(6, INPUT_PULLUP);
  pinMode(7, INPUT_PULLUP);
  pinMode(8, INPUT_PULLUP);
  pinMode(9, INPUT_PULLUP);
  pinMode(10, INPUT_PULLUP);
  pinMode(11, INPUT_PULLUP);
  pinMode(12, INPUT);
}

void loop() {
  String envio = "#";
  int digiRead = 0;
  bool isZero = true;
  for(int i=2;i<=11;i++)
  {
    digiRead = 1 - digitalRead(i);
    envio += digiRead;
    isZero = isZero && digiRead == 0;
  }
  digiRead = digitalRead(12);
  envio += digiRead;
  isZero = isZero && digiRead == 0;
  envio += "$";
  if( !isZero || !lastIsZero )
  {
    Serial.println(envio);
  }
  lastIsZero = isZero;
  String read = "";
  while( Serial.available() )
  {
    read += Serial.readString();
  }
  if(read.indexOf("ping") >= 0)
  {
    Serial.println("pong");
  }
  delay(50);
  i++;
}
