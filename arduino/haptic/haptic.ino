#define  LED 13 
#define  VIVE 0
void setup() {
  // put your setup code here, to run once:
  Serial.begin(9600);
    pinMode(LED, OUTPUT);  
  pinMode(VIVE, OUTPUT);
}

void loop() {
  // put your main code here, to run repeatedly:
  if(Serial.available()){
     int key=Serial.read();
     if('a'<=key&& key<='z'){
      int time=key-'a'+1;
      digitalWrite(VIVE,HIGH);
      digitalWrite(LED,HIGH);
      delay(10*time);
     }
  }
  digitalWrite(VIVE,LOW);
  digitalWrite(LED,LOW);
}
