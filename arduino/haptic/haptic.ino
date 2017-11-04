#include <Adafruit_NeoPixel.h>

#define  LED 13 
#define  VIVE 0
#define SW_IN 19
#define NEO_PIX 12
#define NOF_LED 100
Adafruit_NeoPixel strip = Adafruit_NeoPixel(NOF_LED, NEO_PIX, NEO_GRB + NEO_KHZ800);
void setup() {
  // put your setup code here, to run once:
  Serial.begin(9600);
  pinMode(LED, OUTPUT);  
  pinMode(VIVE, OUTPUT);
  pinMode(SW_IN,INPUT_PULLUP);
  strip.begin();
  strip.show();
}




unsigned long previousTime;
unsigned long viveTime;
unsigned long now;


//Send key input
bool oldKey=HIGH;
void keyPoll(){
  bool k=digitalRead(SW_IN);
  if(k && (!oldKey))Serial.write("U");
  if((!k) && oldKey)Serial.write("D");
  oldKey=k;
}

bool onVive=false;
void startVive(int time){
  digitalWrite(VIVE,HIGH);
  digitalWrite(LED,HIGH);
  viveTime=now+10*time;
  onVive=true;
}
void tickVive(){
  if(!onVive)return;
  if( (long)(now-viveTime)>0){
    onVive=false;
    digitalWrite(VIVE,LOW);
    digitalWrite(LED,LOW);  
  }
}

/////LED strip
int ledReadCount=NOF_LED*3;
byte led[3];
void pushLed(byte v){
      led[ledReadCount%3]=v;
      if(ledReadCount%3==2){
        strip.setPixelColor(ledReadCount/3,led[0],led[1],led[2]);
      }
      ledReadCount++;
      if(ledReadCount==NOF_LED*3)strip.show();
}




void loop() {
  now=millis();

  // put your main code here, to run repeatedly:
  if(Serial.available()){
    int key=Serial.read();
    if(ledReadCount == NOF_LED*3){
      if('a'<=key&& key<='z')startVive(key-'a');
      if('L'==key)ledReadCount=0;
    }else{
      pushLed(Serial.read());
      if(ledReadCount>NOF_LED*3)ledReadCount=NOF_LED*3;
    }
  }
  
  if(now==previousTime)return;
  previousTime=now;
  if(now%15==0)keyPoll();
  tickVive();  
}
