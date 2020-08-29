#include <FastLED.h>
#include "holztools.h"
#include <EEPROM.h>

byte LEDItem::ItemCount = 0;
LEDItem* LEDItem::ItemList[3];

#ifdef ESP32
bool LEDItem::setupPWMChannels[16] = {false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false};
bool LEDItem::setupFastLEDPins[37] = {false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
    false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false};
#endif

//function to setup the item 
LEDItem::LEDItem(byte _id)
{
	id = _id;
	
	//add this class to all item list and increase the item count
	ItemList[ItemCount] = this;
	ItemCount++;
}

void LEDItem::SetupItem(byte _type, byte _ledCount, byte _dPin, byte _rPin, byte _gPin, byte _bPin)
{
	type = _type;
	ledCount = _ledCount;
	dPin = _dPin;
	rPin = _rPin;
	gPin = _gPin;
	bPin = _bPin;

	//setup the pins
	SetupPins();
}

void LEDItem::ChangeMode(byte _mode, byte _arg1, byte _arg2, byte _arg3, byte _arg4, byte _arg5, byte _arg6, byte _arg7, byte _arg8, byte _arg9, byte _music)
{
    useMultiColor = false;
  
	curMode = _mode;
  
    ARG_PRED = _arg1;
    ARG_PGREEN = _arg2;
    ARG_PBLUE = _arg3;
    
    ARG_SRED = _arg4;
    ARG_SGREEN = _arg5;
    ARG_SBLUE = _arg6;
    
    ARG_SPEED = _arg7;
    ARG_DIRECTION = _arg8;
    ARG_BRIGHTNESS = _arg9;
    
    passedMS = 0;
	
	lightningStep = 0;

    rnbwIsSetup = false;

	if(_music == 0)
		music = false;
	else
		music = true;
	
	//set the current led 
	if(ARG_DIRECTION == 0)
		modeCurLed = 0;   
	else
		modeCurLed = ledCount - 1;
	
	if(_mode == MODE_CYCLE)
	{
        //reset the colors for the cycle mode to succesfully cycle
		ARG_PRED = 255;
		ARG_PGREEN = 0;
		ARG_PBLUE = 0;
	}
    else if (_mode == MODE_LIGHTNING)
    {
        //set the correct speed for lightning and static
        ARG_SPEED = 20;
    }
	else if (_mode == MODE_STATIC)
	{
		ARG_SPEED = 10;
	}

    //check if this ledItem is the syncParent of any other led
    for(byte x = 0; x < LEDItem::ItemCount; x++)
    {
        if(LEDItem::ItemList[x]->GetSyncParent() == id)
        {
            LEDItem::ItemList[x]->ChangeMode(curMode, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, music);
            Serial.println(F("Found synced ledItem")); 
        }
    }
}

void LEDItem::SetSoundIntensity(byte _intensity)
{
	soundIntensity = _intensity;
}

void LEDItem::DisplayMode()
{
	if (passedMS == ARG_SPEED)
	{      
		switch (curMode)
		{
			case MODE_STATIC:
				modeStatic();
				break;
			case MODE_RAINBOW:
				modeRainbow();
				break;
			case MODE_CYCLE:
				modeCycle();
				break;
			case MODE_LIGHTNING:
				modeLightning();
				break;
            case MODE_OVERLAY:
                modeOverlay();
                break;
            case MODE_SPINNER:
                modeSpinner();
                break;
			case MODE_OFF:
				modeOff();
				break;
		}

		passedMS = 0;
	}
	else
	{
		passedMS++;
	}
}

void LEDItem::SetupPins()
{
	if(type == TYPE_ARGB)
	{		
		pinMode(dPin, OUTPUT);
		Serial.print(F("Setup DPin "));
		Serial.println(dPin);
		Serial.flush();
		
        #ifdef ESP32
        if(!setupFastLEDPins[dPin])
        {
            switch(dPin)
            {			
                case 1: 
                    FastLED.addLeds<WS2812, 1, GRB>(ledColors, ledCount);
                    break;

                case 2: 
                    FastLED.addLeds<WS2812, 2, GRB>(ledColors, ledCount);
                    break;

                case 3: 
                    FastLED.addLeds<WS2812, 3, GRB>(ledColors, ledCount);
                    break;
                    
                case 4: 
                    FastLED.addLeds<WS2812, 4, GRB>(ledColors, ledCount);
                    break;
                    
                case 5: 
                    FastLED.addLeds<WS2812, 5, GRB>(ledColors, ledCount);
                    break;
                    
                case 12: 
                    FastLED.addLeds<WS2812, 12, GRB>(ledColors, ledCount);
                    break;
                    
                case 13: 
                    FastLED.addLeds<WS2812, 13, GRB>(ledColors, ledCount);
                    break;
                    
                case 14: 
                    FastLED.addLeds<WS2812, 14, GRB>(ledColors, ledCount);
                    break;
                    
                case 15: 
                    FastLED.addLeds<WS2812, 15, GRB>(ledColors, ledCount);
                    break;
                    
                case 16: 
                    FastLED.addLeds<WS2812, 16, GRB>(ledColors, ledCount);
                    break;
                    
                case 17: 
                    FastLED.addLeds<WS2812, 17, GRB>(ledColors, ledCount);
                    break;
                    
                case 18: 
                    FastLED.addLeds<WS2812, 18, GRB>(ledColors, ledCount);
                    break;
                case 19: 
                    FastLED.addLeds<WS2812, 19, GRB>(ledColors, ledCount);
                    break;

                case 21: 
                    FastLED.addLeds<WS2812, 21, GRB>(ledColors, ledCount);
                    break;
                    
                case 22: 
                    FastLED.addLeds<WS2812, 22, GRB>(ledColors, ledCount);
                    break;
                    
                case 23: 
                    FastLED.addLeds<WS2812, 23, GRB>(ledColors, ledCount);
                    break;
                    
                case 25: 
                    FastLED.addLeds<WS2812, 25, GRB>(ledColors, ledCount);
                    break;
                    
                case 26: 
                    FastLED.addLeds<WS2812, 26, GRB>(ledColors, ledCount);
                    break;
                    
                case 27: 
                    FastLED.addLeds<WS2812, 27, GRB>(ledColors, ledCount);
                    break;
                    
                case 32: 
                    FastLED.addLeds<WS2812, 32, GRB>(ledColors, ledCount);
                    break;
                    
                case 33: 
                    FastLED.addLeds<WS2812, 33, GRB>(ledColors, ledCount);
                    break;

                default:
                    Serial.print(dPin);
                    Serial.println(F(" is an unsupported PIN"));
                    return;
            }

            setupFastLEDPins[dPin] = true;
        }
        #endif
		
        #ifndef ESP32
		switch(dPin)
		{			
			case 3: 
				FastLED.addLeds<WS2812, 3, GRB>(ledColors, ledCount);
				break;
				
			case 5: 
				FastLED.addLeds<WS2812, 5, GRB>(ledColors, ledCount);
				break;
				
			case 6: 
				FastLED.addLeds<WS2812, 6, GRB>(ledColors, ledCount);
				break;
		}
        #endif
		
		Serial.println(F("Created FastLED"));
		Serial.flush();
	}
	else if(type == TYPE_4RGB)
	{		
        #ifdef ESP32
        byte ledcChannels[3] = {255, 255, 255};
        byte foundChannels = 0;

        if(rPinChannel != 255 && gPinChannel != 255 && bPinChannel != 255)
            goto initPins;

        for(byte x = 0; x < 16; x++)
        {
            if(setupPWMChannels[x] == false)
            {
                ledcChannels[foundChannels] = x;
                setupPWMChannels[x] = true;
                foundChannels++;

                if(foundChannels == 3)
                    goto initPins;
            }
        }

        Serial.println(F("Unable to get an unused ledc channel"));
        return;

        initPins:

        rPinChannel = ledcChannels[0];
        gPinChannel = ledcChannels[1];
        bPinChannel = ledcChannels[2];

        ledcSetup(rPinChannel, 5000, 8);
        ledcAttachPin(rPin, rPinChannel);
        Serial.print(F("Setup RPin "));
        Serial.print(rPin);
        Serial.print(F(" to use channel "));
        Serial.println(rPinChannel);

        ledcSetup(gPinChannel, 5000, 8);
        ledcAttachPin(gPin, gPinChannel);
        Serial.print(F("Setup GPin "));
        Serial.print(gPin);
        Serial.print(F(" to use channel "));
        Serial.println(gPinChannel);

        ledcSetup(bPinChannel, 5000, 8);
        ledcAttachPin(bPin, bPinChannel);
        Serial.print(F("Setup BPin "));
        Serial.print(bPin);
        Serial.print(F(" to use channel "));
        Serial.println(bPinChannel);
        #endif

        #ifndef ESP32
		pinMode(rPin, OUTPUT);
		Serial.print(F("Setup RPin "));
		Serial.println(rPin);
		Serial.flush();
		
		pinMode(gPin, OUTPUT);
		Serial.print(F("Setup GPin "));
		Serial.println(gPin);
		Serial.flush();
		
		pinMode(bPin, OUTPUT);
		Serial.print(F("Setup BPin "));
		Serial.println(bPin);
		Serial.flush();
        #endif
		
		return;
	}
}

int LEDItem::SaveData(int offset)
{
    // save everything
    EEPROM.put(offset, id);
    EEPROM.put(offset + sizeof(byte), type);
    EEPROM.put(offset + (2 * sizeof(byte)), ledCount);
    EEPROM.put(offset + (3 * sizeof(byte)), dPin);
    EEPROM.put(offset + (4 * sizeof(byte)), rPin);
    EEPROM.put(offset + (5 * sizeof(byte)), gPin);
    EEPROM.put(offset + (6 * sizeof(byte)), bPin);
    EEPROM.put(offset + (7 * sizeof(byte)), syncParent);
    EEPROM.put(offset + (8 * sizeof(byte)), curMode);
    EEPROM.put(offset + (9 * sizeof(byte)), arg1);
    EEPROM.put(offset + (10 * sizeof(byte)), arg2);
    EEPROM.put(offset + (11 * sizeof(byte)), arg3);
    EEPROM.put(offset + (12 * sizeof(byte)), arg4);
    EEPROM.put(offset + (13 * sizeof(byte)), arg5);
    EEPROM.put(offset + (14 * sizeof(byte)), arg6);
    EEPROM.put(offset + (15 * sizeof(byte)), arg7);
    EEPROM.put(offset + (16 * sizeof(byte)), arg8);
    EEPROM.put(offset + (17 * sizeof(byte)), arg9);

    offset += (18 * sizeof(byte));
    EEPROM.put(offset, useMultiColor);
    EEPROM.put(offset + sizeof(bool), music);

    offset += (2 * sizeof(bool));
    EEPROM.put(offset, ledColors);

    return offset + sizeof(ledColors);
}

int LEDItem::LoadData(int offset)
{
    // load everything
    EEPROM.get(offset, id);
    EEPROM.get(offset + sizeof(byte), type);
    EEPROM.get(offset + (2 * sizeof(byte)), ledCount);
    EEPROM.get(offset + (3 * sizeof(byte)), dPin);
    EEPROM.get(offset + (4 * sizeof(byte)), rPin);
    EEPROM.get(offset + (5 * sizeof(byte)), gPin);
    EEPROM.get(offset + (6 * sizeof(byte)), bPin);
    EEPROM.get(offset + (7 * sizeof(byte)), syncParent);
    EEPROM.get(offset + (8 * sizeof(byte)), curMode);
    EEPROM.get(offset + (9 * sizeof(byte)), arg1);
    EEPROM.get(offset + (10 * sizeof(byte)), arg2);
    EEPROM.get(offset + (11 * sizeof(byte)), arg3);
    EEPROM.get(offset + (12 * sizeof(byte)), arg4);
    EEPROM.get(offset + (13 * sizeof(byte)), arg5);
    EEPROM.get(offset + (14 * sizeof(byte)), arg6);
    EEPROM.get(offset + (15 * sizeof(byte)), arg7);
    EEPROM.get(offset + (16 * sizeof(byte)), arg8);
    EEPROM.get(offset + (17 * sizeof(byte)), arg9);

    offset += (18 * sizeof(byte));
    EEPROM.get(offset, useMultiColor);
    EEPROM.get(offset + sizeof(bool), music);

    offset += (2 * sizeof(bool));
    EEPROM.get(offset, ledColors);

    return offset + sizeof(ledColors);
}

//all the modes
void LEDItem::modeOverlay()
{
	setOverlayColor();

	if (music)
	{
		shownRed = ARG_PRED * (float)((float)soundIntensity / 255.00);
		shownGreen = ARG_PGREEN * (float)((float)soundIntensity / 255.00);
		shownBlue = ARG_PBLUE * (float)((float)soundIntensity / 255.00);
	}
	else
	{
		shownRed = ARG_PRED;
		shownGreen = ARG_PGREEN;
		shownBlue = ARG_PBLUE;
	}

	if (type == TYPE_ARGB)
	{
		//apply for argb
		ledColors[modeCurLed] = CRGB(shownRed, shownGreen, shownBlue);

		FastLED.show();
	}
	else if (type == TYPE_4RGB)
	{
		//apply for 4pin rgb
        #ifdef ESP32
        ledcWrite(rPinChannel, shownRed);
        ledcWrite(gPinChannel, shownGreen);
        ledcWrite(bPinChannel, shownBlue);
        #endif
        #ifndef ESP32
		analogWrite(rPin, shownRed);
		analogWrite(gPin, shownGreen);
		analogWrite(bPin, shownBlue);
        #endif
	}

	//higher the next led
	if (ARG_DIRECTION == 0)
	{
		modeCurLed++;
	}
	else
	{
		modeCurLed--;
	}

	if (modeCurLed == ledCount || modeCurLed == 255)
	{
		if (curColor != 11)
		{
			curColor++;
		}
		else
		{
			curColor = 0;
		}

		if (ARG_DIRECTION == 0)
		{
			modeCurLed = 0;
		}
		else
		{
			modeCurLed = ledCount - 1;
		}
	}
}

void LEDItem::modeRainbow()
{
	if(rnbwIsSetup)
	{ 
		if(music)
		{
			//higher the hue
			for (byte x = 0; x < ledCount; x++)
			{
				ledColors[x] = CHSV(hue[x]++, 255, soundIntensity);
			}
		}
		else
		{
			//higher the hue
			for (byte x = 0; x < ledCount; x++)
			{
				ledColors[x] = CHSV(hue[x]++, 255, ARG_BRIGHTNESS);
			}
		}

		FastLED.show();
	}
	else
	{
		//set the hue for each led
		for (byte x = 0; x < ledCount; x++)
		{
			hue[x] = 255 / ledCount * x;
		}

		rnbwIsSetup = true;
	}
}

void LEDItem::modeLightning()
{
	if(type == TYPE_ARGB)
	{
		if(modeCurLed - 2 != ledCount)
		{ 
			for(byte x = 0; x <= ledCount; x++)
			{
				//check if the Led is the current displayed led
				if(x == modeCurLed)
				{
					ledColors[x] = CRGB(ARG_PRED, ARG_PGREEN, ARG_PBLUE);
				}
        else if((x - 1 == modeCurLed) || (x + 1 == modeCurLed))
        {
          ledColors[x] = CRGB(ARG_PRED * 0.30, ARG_PGREEN * 0.30, ARG_PBLUE * 0.30);
        }
        else if((x - 2 == modeCurLed) || (x + 2 == modeCurLed))
        {
          ledColors[x] = CRGB(ARG_PRED * 0.01, ARG_PGREEN * 0.01, ARG_PBLUE * 0.01);
        }
				else
				{
					ledColors[x] = CRGB(0, 0, 0);
				}
			}

			FastLED.show();

			//higher the next led
			modeCurLed++;
		}
		else
		{
			//set the last LED off
			ledColors[ledCount - 1] = CRGB(0, 0, 0);
			FastLED.show();

			lightningStep++;

			if (lightningStep == 40)
			{
				modeCurLed = 254;

				lightningStep = 0;
			}
		}
	}
	else if(type == TYPE_4RGB)
	{
		if (lightningStep == 0)
		{
            #ifdef ESP32
            ledcWrite(rPinChannel, ARG_PRED);
            ledcWrite(gPinChannel, ARG_PGREEN);
            ledcWrite(bPinChannel, ARG_PBLUE);
            #endif
            
            #ifndef ESP32
			analogWrite(rPin, ARG_PRED);
			analogWrite(gPin, ARG_PGREEN);
			analogWrite(bPin, ARG_PBLUE);
            #endif 
		}
		else if (lightningStep == 40)
		{
            #ifdef ESP32
            ledcWrite(rPinChannel, 0);
            ledcWrite(gPinChannel, 0);
            ledcWrite(bPinChannel, 0);
            #endif

            #ifndef ESP32
			analogWrite(rPin, 0);
			analogWrite(gPin, 0);
			analogWrite(bPin, 0);
            #endif
        }

		lightningStep++;
	}
}

void LEDItem::modeSpinner()
{
  if(type == TYPE_ARGB)
  {
    if(modeCurLed == ledCount)
      modeCurLed = 0;
    
    for(byte x = 0; x < ledCount + ARG_LENGTH; x++)
    {
      //check if the Led is the current displayed led
      if(x == modeCurLed)
      {
        ledColors[x] = CRGB(ARG_PRED, ARG_PGREEN, ARG_PBLUE);
      }
      else if(((x > modeCurLed) && (x <= modeCurLed + ARG_LENGTH)) || ((x < modeCurLed) && (x >= modeCurLed - ARG_LENGTH)))
      {
        //light up all leds around the main led
        if(x < ledCount)
          ledColors[x] = CRGB(ARG_PRED, ARG_PGREEN, ARG_PBLUE);
          
        if(x + 1 > ledCount)
        {
          //light up the leds at the beginning of the strip when the current led is at the end
          for(byte c = 0; c < (x + 1) - ledCount; c++)
          {
            ledColors[c] = CRGB(ARG_PRED, ARG_PGREEN, ARG_PBLUE);

            //dim the last led
            if(c + 1 == (x + 1) - ledCount)
              ledColors[c] = CRGB(ARG_PRED * 0.30, ARG_PGREEN * 0.30, ARG_PBLUE * 0.30);
          }
        }

        //dim the last led
        if(((x == modeCurLed + ARG_LENGTH) || x == modeCurLed - ARG_LENGTH) && x < ledCount)
          ledColors[x] = CRGB(ARG_PRED * 0.30, ARG_PGREEN * 0.30, ARG_PBLUE * 0.30);
      }
      else if(ledCount + (modeCurLed - ARG_LENGTH) <= x)
      {
        if(x > (ledCount - 1))
          continue;
          
        //light up the LED's on the end of the LED-strip
        ledColors[x] = CRGB(ARG_PRED, ARG_PGREEN, ARG_PBLUE);

        //dim the last led
        if(x == ledCount + (modeCurLed - ARG_LENGTH))
          ledColors[x] = CRGB(ARG_PRED * 0.30, ARG_PGREEN * 0.30, ARG_PBLUE * 0.30);
      }
      else
      {
        if(x > (ledCount - 1))
          continue;
          
        ledColors[x] = CRGB(ARG_SRED, ARG_SGREEN, ARG_SBLUE);
      }
    }

    FastLED.show();

    //higher the next led
    modeCurLed++;
  }
  else if(type == TYPE_4RGB)
  {
    if (lightningStep == 0)
    {
        #ifdef ESP32
        ledcWrite(rPinChannel, ARG_PRED);
        ledcWrite(gPinChannel, ARG_PGREEN);
        ledcWrite(bPinChannel, ARG_PBLUE);
        #endif
        
        #ifndef ESP32
        analogWrite(rPin, ARG_PRED);
        analogWrite(gPin, ARG_PGREEN);
        analogWrite(bPin, ARG_PBLUE);
        #endif
    }
    else if (lightningStep == 40)
    {
        #ifdef ESP32
        ledcWrite(rPinChannel, 0);
        ledcWrite(gPinChannel, 0);
        ledcWrite(bPinChannel, 0);
        #endif

        #ifndef ESP32
        analogWrite(rPin, 0);
        analogWrite(gPin, 0);
        analogWrite(bPin, 0);
        #endif
    }

    lightningStep++;
  }
}

void LEDItem::modeCycle()
{
	if(redGoingDown)
	{
		if(ARG_PRED != 0)
		{
			ARG_PRED--;
			ARG_PGREEN++;
		}
		else 
		{
			redGoingDown = false;
			greenGoingDown = true;
		}
	} 
	else if(greenGoingDown)
	{
		if(ARG_PGREEN != 0)
		{
			ARG_PGREEN--;
			ARG_PBLUE++;
		}
		else 
		{
		  greenGoingDown = false;
		  blueGoingDown = true;
		}
	}
	else if(blueGoingDown)
	{
		if(ARG_PBLUE != 0)
		{
			ARG_PBLUE--;
			ARG_PRED++;
		}
		else 
		{
			blueGoingDown = false;
			redGoingDown = true;
		}
	}
  
	if(music)
	{
		shownRed = ARG_PRED * (float)((float)soundIntensity / 255.00);
		shownGreen = ARG_PGREEN * (float)((float)soundIntensity / 255.00);
		shownBlue = ARG_PBLUE * (float)((float)soundIntensity / 255.00);
	}
	else
	{
		shownRed = ARG_PRED * (float)((float)ARG_BRIGHTNESS / 255.00);
		shownGreen = ARG_PGREEN * (float)((float)ARG_BRIGHTNESS / 255.00);
		shownBlue = ARG_PBLUE * (float)((float)ARG_BRIGHTNESS / 255.00);
	}	
  
	if(type == TYPE_ARGB)
	{
		for(byte x = 0; x < ledCount; x++)
		{
			ledColors[x] = CRGB(shownRed, shownGreen, shownBlue);
		}
	  
		FastLED.show();
	}
	else if(type == TYPE_4RGB)
	{
        #ifdef ESP32
        ledcWrite(rPinChannel, shownRed);
        ledcWrite(gPinChannel, shownGreen);
        ledcWrite(bPinChannel, shownBlue);
        #endif
        
        #ifndef ESP32
		analogWrite(rPin, shownRed);
		analogWrite(gPin, shownGreen);
		analogWrite(bPin, shownBlue);
        #endif
	}
}

void LEDItem::modeStatic()
{	
	if(music)
	{
		shownRed = ARG_PRED * (float)((float)soundIntensity / 255.00);
		shownGreen = ARG_PGREEN * (float)((float)soundIntensity / 255.00);
		shownBlue = ARG_PBLUE * (float)((float)soundIntensity / 255.00);
	}
	else
	{
		shownRed = ARG_PRED;
		shownGreen = ARG_PGREEN;
		shownBlue = ARG_PBLUE;
	}
	
	if(type == TYPE_ARGB)
	{
        if(!useMultiColor)
        {
            for(byte x = 0; x < ledCount; x++)
            {
                ledColors[x] = CRGB(shownRed, shownGreen, shownBlue);
            }
            
            FastLED.show();
        }
	}
	else if(type == TYPE_4RGB)
	{
        #ifdef ESP32
        ledcWrite(rPinChannel, shownRed);
        ledcWrite(gPinChannel, shownGreen);
        ledcWrite(bPinChannel, shownBlue);
        #endif
        
        #ifndef ESP32
		analogWrite(rPin, shownRed);
		analogWrite(gPin, shownGreen);
		analogWrite(bPin, shownBlue);
        #endif
	}		
}

void LEDItem::modeOff()
{
	if(type == TYPE_ARGB)
	{
		for(byte x = 0; x < ledCount; x++)
		{
			ledColors[x] = CRGB(0, 0, 0);
			FastLED.show();			
		}
	}
	else if(type == TYPE_4RGB)
	{
        #ifdef ESP32
        ledcWrite(rPinChannel, 0);
        ledcWrite(gPinChannel, 0);
        ledcWrite(bPinChannel, 0);
        #endif
        
        #ifndef ESP32
		analogWrite(rPin, 0);
		analogWrite(gPin, 0);
		analogWrite(bPin, 0);
        #endif
	}
}

void LEDItem::setOverlayColor()
{
	if(modeCurLed != ledCount && modeCurLed != -1)
	{
		switch(curColor)
		{
		  case 0:
			ARG_PRED = 255;
			ARG_PGREEN = 0;
			ARG_PBLUE = 0;
			break;
			
		  case 1:
			ARG_PRED = 255;
			ARG_PGREEN = 127;
			ARG_PBLUE = 0;
			break;
			
		  case 2:
			ARG_PRED = 255;
			ARG_PGREEN = 255;
			ARG_PBLUE = 0;
			break;

		  case 3:
			ARG_PRED = 127;
			ARG_PGREEN = 255;
			ARG_PBLUE = 0;
			break;

		  case 4:
			ARG_PRED = 0;
			ARG_PGREEN = 255;
			ARG_PBLUE = 0;
			break;
			
		  case 5:
			ARG_PRED = 0;
			ARG_PGREEN = 255;
			ARG_PBLUE = 127;
			break;

		  case 6:
			ARG_PRED = 0;
			ARG_PGREEN = 255;
			ARG_PBLUE = 255;
			break;
			
		  case 7:
			ARG_PRED = 0;
			ARG_PGREEN = 127;
			ARG_PBLUE = 255;
			break;

		  case 8:
			ARG_PRED = 0;
			ARG_PGREEN = 0;
			ARG_PBLUE = 255;
			break;

		  case 9:
			ARG_PRED = 127;
			ARG_PGREEN = 0;
			ARG_PBLUE = 255;
			break;
			
		  case 10:
			ARG_PRED = 255;
			ARG_PGREEN = 0;
			ARG_PBLUE = 255;
			break;
			
		  case 11:
			ARG_PRED = 255;
			ARG_PGREEN = 0;
			ARG_PBLUE = 127;
			break;
		}
	}
	else
	{
		if(curColor != 11)
		{
			curColor++;
		}
		else 
		{
			curColor = 0;
		}

		if(ARG_DIRECTION == 0)
		{
			modeCurLed = 0;
		}
		else
		{
			modeCurLed = ledCount;  
		}
	}
}

// void LEDItem::PrintInfo()
// {
//     Serial.print(F("Values of item with ID "));
//     Serial.println(id);

//     Serial.println();
    
//     Serial.print(F("type "));
//     Serial.println(type);
    
//     Serial.print(F("ledCount "));
//     Serial.println(ledCount);
    
//     Serial.print(F("dPin "));
//     Serial.println(dPin);
    
//     Serial.print(F("rPin "));
//     Serial.println(rPin);
    
//     Serial.print(F("gPin "));
//     Serial.println(gPin);
    
//     Serial.print(F("bPin "));
//     Serial.println(bPin);
    
//     Serial.print(F("id "));
//     Serial.println(id);
    
//     Serial.print(F("syncparent "));
//     Serial.println(syncParent);
    
//     Serial.print(F("curMode "));
//     Serial.println(curMode);
    
//     Serial.print(F("rnbwIsSetup "));
//     Serial.println(rnbwIsSetup);
    
//     Serial.print(F("redGoingDown "));
//     Serial.println(redGoingDown);
    
//     Serial.print(F("blueGoingDown "));
//     Serial.println(blueGoingDown);
    
//     Serial.print(F("greenGoingDown "));
//     Serial.println(greenGoingDown);
    
//     Serial.print(F("useMultiColor "));
//     Serial.println(useMultiColor);
    
//     Serial.print(F("music "));
//     Serial.println(music);
    
//     Serial.print(F("arg1 "));
//     Serial.println(arg1);
    
//     Serial.print(F("arg2 "));
//     Serial.println(arg2);
    
//     Serial.print(F("arg3 "));
//     Serial.println(arg3);
    
//     Serial.print(F("arg4 "));
//     Serial.println(arg4);
    
//     Serial.print(F("arg5 "));
//     Serial.println(arg5);
    
//     Serial.print(F("arg6 "));
//     Serial.println(arg6);
    
//     Serial.print(F("arg7 "));
//     Serial.println(arg7);
    
//     Serial.print(F("arg8 "));
//     Serial.println(arg8);
    
//     Serial.print(F("arg9 "));
//     Serial.println(arg9);
    
//     Serial.print(F("modeCurLed "));
//     Serial.println(modeCurLed);
    
//     Serial.print(F("passedMS "));
//     Serial.println(passedMS);
    
//     Serial.print(F("lightningStep "));
//     Serial.println(lightningStep);
    
//     Serial.print(F("curColor "));
//     Serial.println(curColor);
    
//     Serial.print(F("shownRed "));
//     Serial.println(shownRed);
    
//     Serial.print(F("shownGreen "));
//     Serial.println(shownGreen);
    
//     Serial.print(F("shownBlue "));
//     Serial.println(shownBlue);

//     Serial.println();

//     Serial.println(F("============================"));
// }

void LEDItem::Refresh()
{
  ChangeMode(curMode, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, music);
}

void LEDItem::SetSyncParent(byte parent)
{
  syncParent = parent;

  //refresh the syncParent
  for(byte x = 0; x < LEDItem::ItemCount; x++)
  {
    if(LEDItem::ItemList[x]->GetID() == syncParent)
    {
      LEDItem::ItemList[x]->Refresh(); 
      Serial.println(F("Found and refreshed SyncParent"));
    }
  }
}

void LEDItem::SetLed(byte led, CRGB color)
{
  ledColors[led] = color;
  FastLED.show();

  //check if this ledItem is the syncParent of any other led
  for(byte x = 0; x < LEDItem::ItemCount; x++)
  {
    if(LEDItem::ItemList[x]->GetSyncParent() == id)
    {
       LEDItem::ItemList[x]->SetLed(led, color);
       Serial.println(F("Found synced ledItem")); 
    }
  }
}

void LEDItem::SetUseMultiColor(bool value)
{
  useMultiColor = value;
}

byte LEDItem::GetID()
{
  return id;
}

byte LEDItem::GetSyncParent()
{
  return syncParent;
}
