//#include <MemoryFree.h>
#include "holztools.h"

#define BAUDRATE 4800

const String binaryVer = "1.07";
const String arduinoModel = "NanoR3";

String usbMessage = "";

bool stringComplete = false;
bool backslash = false;
bool isMusicData = false;
bool isSysInfoRequest = false;

//declare functions
void decodeMessage(String message);

void setup() 
{
  Serial.begin(BAUDRATE);
}

void loop() 
{
  if(stringComplete)
  {
    stringComplete = false;
    
    Serial.print(F("Arduino received: ")); 
    Serial.print(usbMessage);
    Serial.println();
    Serial.flush();
    
    decodeMessage(usbMessage);
    usbMessage = "";
  }

  //display the active mode on each leditem
  for(byte x = 0; x < LEDItem::ItemCount; x++)
  {
    LEDItem::ItemList[x]->DisplayMode();
  }

  delay(1);
}

void decodeMessage(String message)
{ 
  String temp = ""; 
  
  byte mode = 0;
  byte type = 0;
  byte ledCount = 0;
  byte dPin = 0;
  byte rPin = 0;
  byte gPin = 0;
  byte bPin = 0;
  byte arg1 = 0;
  byte arg2 = 0;
  byte arg3 = 0;
  byte arg4 = 0;
  byte arg5 = 0;
  byte arg6 = 0;
  byte arg7 = 0;
  byte arg8 = 0;
  byte arg9 = 0;
  byte overlapedMode = 0;
  byte id = 0;
  byte isMusic = false;
  
  //get the mode
  temp = message.substring(1,5);

  if(temp == "STTC")
    mode = MODE_STATIC;
  else if (temp == "RNBW")
    mode = MODE_RAINBOW;
  else if (temp == "CYCL")
    mode = MODE_CYCLE;
  else if (temp == "LING")
    mode = MODE_LIGHTNING;
  else if (temp == "OVRL")
    mode = MODE_OVERLAY;
  else if (temp == "SPIN")
    mode = MODE_SPINNER;
  else if (temp == "TOFF")
    mode = MODE_OFF;

  //check if item should use music mode
  isMusic = message.substring(5,6).toInt();

  //get the type
  temp = message.substring(6,7);
  type = temp.toInt();

  //get the pins
  temp = message.substring(7,9);
  
  if(type == TYPE_ARGB)
  {
    dPin = temp.toInt();
    temp = message.substring(9,13);
    ledCount = temp.toInt();
  }
  else if(type == TYPE_4RGB)
  {
    rPin = temp.toInt();
    temp = message.substring(9,11);
    gPin = temp.toInt();
    temp = message.substring(11, 13);
    bPin = temp.toInt();
  }

  //get the args
  temp = message.substring(13, 16);
  arg1 = temp.toInt();
  temp = message.substring(16, 19);
  arg2 = temp.toInt();
  temp = message.substring(19, 22);
  arg3 = temp.toInt();
  temp = message.substring(22, 25);
  arg4 = temp.toInt();
  temp = message.substring(25, 28);
  arg5 = temp.toInt();
  temp = message.substring(28, 31);
  arg6 = temp.toInt();
  temp = message.substring(31, 34);
  arg7 = temp.toInt();
  temp = message.substring(34, 37);
  arg8 = temp.toInt();
  temp = message.substring(37, 40);
  arg9 = temp.toInt();
  temp = message.substring(40,42);
  id = temp.toInt();
 
  bool idExists = false;

  LEDItem* ledItem;
  
  //check if item with id exists
  for(byte x = 0; x < LEDItem::ItemCount; x++)
  {
    if(LEDItem::ItemList[x]->GetID() == id)
    {
      idExists = true;
      ledItem = LEDItem::ItemList[x];
      Serial.print(F("Found existing Item with ID: "));
      Serial.println(id);
    }
  }

  if(!idExists)
  {
    ledItem = new LEDItem(id);
    Serial.print(F("Creating new Item with ID: "));
    Serial.println(id);
  }
    
  ledItem->SetupItem(type, ledCount, dPin, rPin, gPin, bPin);

  //set the syncParent if LED should be synced
  if(message.substring(1,5) == "SYNC")
  {
    Serial.print(F("Set SyncParent to: "));
    Serial.println(arg1);
    
    ledItem->SetSyncParent(arg1);
  }
  else
  {
    //reset syncparent
    ledItem->SetSyncParent(255);
    
    ledItem->ChangeMode(mode, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, isMusic);
    
    Serial.print(F("Set item to mode: "));
    Serial.println(mode);
  
    if(isMusic == 1)
    {
      Serial.println(F("Music Mode is turned on"));
    }
  }
  
}

//fetch data from usb
void serialEvent()
{    
  while(Serial.available())
  {
    char c = (char)Serial.read();
    
    usbMessage += c;

    if(c == '#')
    {
      usbMessage = "#";
    }
    else if(c == '\\')
    {
      backslash = true;
    }
    else if(c == '+')
    {
      isMusicData = true;
    }
    else if(c == '_')
    {
      isSysInfoRequest = true;
    }
    else if(backslash && (c == 'n'))
    {
      backslash = false;

      if(isMusicData)
      {
        byte intensity = usbMessage.substring(1, 5).toInt();
        
        for(byte x = 0; x < LEDItem::ItemCount; x++)
        {
          LEDItem::ItemList[x]->SetSoundIntensity(intensity);
        }
        
        stringComplete = false;
        usbMessage = "";
        isMusicData = false;
      }
      else if(isSysInfoRequest)
      {
        delay(100);
        Serial.print("_" + binaryVer + "_" + arduinoModel);
        
        stringComplete = false;
        usbMessage = "";
        isSysInfoRequest = false;
      }
      else
      {
        stringComplete = true;
      }
    }
  }
}
