int WORK_SIGNAL = 2;
int YES_SIGNAL = 3;
int NO_SIGNAL = 4;


char TURN = 't';
char OFF = 'o';
char YES = 'y';
char NO = 'n';

void setup() {
  pinMode(WORK_SIGNAL, OUTPUT);
  pinMode(NO_SIGNAL, OUTPUT);
  pinMode(YES_SIGNAL, OUTPUT);

  Serial.begin(250000);
}

void loop() {
  if (Serial.available())
  {
    char c = (char)Serial.read();

    if (c == TURN)
      digitalWrite(WORK_SIGNAL, HIGH);
    else if (c == OFF)
      digitalWrite(WORK_SIGNAL, LOW);
    else if (c == YES)
    {
      digitalWrite(YES_SIGNAL, HIGH);
      delay(1900);
      digitalWrite(YES_SIGNAL, LOW);
    }
    else if (c == NO)
    {
      digitalWrite(NO_SIGNAL, HIGH);
      delay(900);
      digitalWrite(NO_SIGNAL, LOW);
    }
  }
}
