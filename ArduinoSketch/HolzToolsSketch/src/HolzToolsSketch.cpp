// #include <MemoryFree.h>
#include "holztools.h"
#include "EEPROM.h"

#define BAUDRATE 4800
#define EEPROM_OFFSET 
#define ESP32

const String binaryVer = "1.07";
const String arduinoModel = "NanoR3";

String usbMessage = "";

bool stringComplete = false;
bool backslash = false;
bool isMusicData = false;
bool isCommand = false;
bool isMultiColor = false;

//declare functions
void decodeMessage(String message);

#ifdef ESP32
TaskHandle_t serialTaskHandle;
void serialTaskFunc(void* parameter);
#endif

void setup() 
{
    Serial.begin(BAUDRATE);

    // start the serial task on the first core of the esp32
    #ifdef ESP32
    xTaskCreatePinnedToCore(serialTaskFunc, "SerialTask", uxTaskGetStackHighWaterMark(NULL), NULL, 0, &serialTaskHandle, 0);
    Serial.println("Started SerialTask on core =_|!- 0");
    #endif

    // load the information of all previous ledItems
    byte ledCount = 0;
    EEPROM.get(0, ledCount);
    
    Serial.print(F("Ledcount: "));
    Serial.println(ledCount);

    // stop loading if ledCount is 255 (default value)
    if(ledCount == 255 || ledCount == 0) return;

    int offset = sizeof(byte);

    for(byte x = 0; x < ledCount; x++)
    {
        Serial.print("Reading at address: ");
        Serial.println(offset);

        LEDItem* item = new LEDItem(x);
        offset = item->LoadData(offset);

        LEDItem::ItemList[x] = item;
        
        item->SetupPins();
        
        Serial.print(F("Loaded led with ID ")); 
        Serial.print(item->GetID());
        Serial.println();
    }

    LEDItem::ItemCount = ledCount;
}

void loop() 
{
    if(stringComplete)
    {
        stringComplete = false;
        
        Serial.print(F("Received: ")); 
        Serial.print(usbMessage);
        Serial.println();
        
        decodeMessage(usbMessage);
        usbMessage = "";

        // save the information of all current ledItems to the EEPROM
        EEPROM.put(0, LEDItem::ItemCount);

        int offset = sizeof(byte);

        for(byte x = 0; x < LEDItem::ItemCount; x++)
        {
            // EEPROM.put(sizeof(byte) + (x * sizeof(LEDItem)), LEDItem::ItemList[x]);
            Serial.print("Saving at address: ");
            Serial.println(offset);

            offset = LEDItem::ItemList[x]->SaveData(offset);

            Serial.print("Saved item with id ");
            Serial.println(LEDItem::ItemList[x]->GetID());
        }
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
    byte id = 0;
    byte isMusic = false;
    
    //get the mode
    if(message.substring(1,5) == "STTC")
        mode = MODE_STATIC;
    else if (message.substring(1,5) == "RNBW")
        mode = MODE_RAINBOW;
    else if (message.substring(1,5) == "CYCL")
        mode = MODE_CYCLE;
    else if (message.substring(1,5) == "LING")
        mode = MODE_LIGHTNING;
    else if (message.substring(1,5) == "OVRL")
        mode = MODE_OVERLAY;
    else if (message.substring(1,5) == "SPIN")
        mode = MODE_SPINNER;
    else if (message.substring(1,5) == "TOFF")
        mode = MODE_OFF;

    //check if item should use music mode
    isMusic = message.substring(5,6).toInt();

    //get the type
    type = message.substring(6,7).toInt();

    //get the pins    
    if(type == TYPE_ARGB)
    {
        dPin = message.substring(7,9).toInt();
        ledCount = message.substring(9,13).toInt();
    }
    else if(type == TYPE_4RGB)
    {
        rPin = message.substring(7,9).toInt();
        gPin = message.substring(9,11).toInt();
        bPin = message.substring(11, 13).toInt();
    }

    //get the args
    arg1 = message.substring(13, 16).toInt();
    arg2 = message.substring(16, 19).toInt();
    arg3 = message.substring(19, 22).toInt();
    arg4 = message.substring(22, 25).toInt();
    arg5 = message.substring(25, 28).toInt();
    arg6 = message.substring(28, 31).toInt();
    arg7 = message.substring(31, 34).toInt();
    arg8 = message.substring(34, 37).toInt();
    arg9 = message.substring(37, 40).toInt();
    id = message.substring(40,42).toInt();

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
      usbMessage = "+";
      isMusicData = true;
    }
    else if(c == '_')
    {
      usbMessage = "_";
      isCommand = true;
    }
    else if(c == '&')
    {
      usbMessage = "&";
      isMultiColor = true;
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
      else if(isCommand)
      {
        delay(100);
        
        if(usbMessage == "_\\n")
        {
            Serial.print("_" + binaryVer + "_" + arduinoModel);
        }
        else if(usbMessage == "_CLRROM\\n")
        {
            // reset saved leditemcount to 0
            EEPROM.put(0, (byte)0);

            Serial.println(F("Cleared savedata"));
        }
        else if(usbMessage == "_RST\\n")
        {
            Serial.println(F("Resetting program..."));
            Serial.flush();
            #ifdef ESP32
            ESP.restart();
            #endif
            #ifndef ESP32
            asm ("jmp 0");
            #endif
        }

        stringComplete = false;
        usbMessage = "";
        isCommand = false;
      }
      else if(isMultiColor)
      {
            byte dPin = 0;
            byte id = 0;
            byte ledCount = 0;
            byte led = 0;
            CRGB color = CRGB(0,0,0);

            id = usbMessage.substring(1,3).toInt();
        
            dPin = usbMessage.substring(3,9).substring(0,2).toInt();
            ledCount = usbMessage.substring(3,9).substring(2, 6).toInt();

            led = usbMessage.substring(9, 12).toInt();

            color = CRGB(usbMessage.substring(12, 21).substring(0, 3).toInt(), usbMessage.substring(12, 21).substring(3, 6).toInt(), usbMessage.substring(12, 21).substring(6, 9).toInt());
            
            bool idExists = false;
    
            LEDItem* ledItem;
            
            //check if item with id exists
            for(byte x = 0; x < LEDItem::ItemCount; x++)
            {
                if(LEDItem::ItemList[x]->GetID() == id)
                {
                    idExists = true;
                    ledItem = LEDItem::ItemList[x];
                }
            }
        
            if(!idExists)
            {
                ledItem = new LEDItem(id);
            }
        
            ledItem->SetupItem(TYPE_ARGB, ledCount, dPin, 0, 0, 0);
            
            //reset syncparent
            ledItem->SetSyncParent(255);
            ledItem->ChangeMode(MODE_STATIC, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            ledItem->SetUseMultiColor(true);
            ledItem->SetLed(led, color);
            
            stringComplete = false;
            usbMessage = "";
            isMultiColor = false;

            Serial.println("^");
      }
      else
      {
        stringComplete = true;
      }
    }
  }
}

#ifdef ESP32
void serialTaskFunc(void* parameter)
{
    while (true)
    {
        serialEvent();
        delay(1);
    }
}
#endif

