#include <FastLED.h>
#include "holztools.h"

byte LEDItem::ItemCount = 0;
LEDItem* LEDItem::ItemList[3];

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
	setupPins();
}

void LEDItem::ChangeMode(byte _mode, byte _arg1, byte _arg2, byte _arg3, byte _arg4, byte _arg5, byte _arg6, byte _arg7, byte _arg8, byte _arg9, byte _music)
{
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

void LEDItem::setupPins()
{
	if(type == TYPE_ARGB)
	{		
		pinMode(dPin, OUTPUT);
		Serial.print(F("Setup DPin "));
		Serial.println(dPin);
		Serial.flush();
		
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
				
			case 9: 
				FastLED.addLeds<WS2812, 9, GRB>(ledColors, ledCount);
				break;
				
			case 10: 
				FastLED.addLeds<WS2812, 10, GRB>(ledColors, ledCount);
				break;
				
			case 11: 
				FastLED.addLeds<WS2812, 11, GRB>(ledColors, ledCount);
				break;
		}
		
		Serial.println(F("Created FastLED"));
		Serial.flush();
	}
	else if(type == TYPE_4RGB)
	{		
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
		
		return;
	}
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
		analogWrite(rPin, shownRed);
		analogWrite(gPin, shownGreen);
		analogWrite(bPin, shownBlue);
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
			analogWrite(rPin, ARG_PRED);
			analogWrite(gPin, ARG_PGREEN);
			analogWrite(bPin, ARG_PBLUE);
		}
		else if (lightningStep == 40)
		{
			analogWrite(rPin, 0);
			analogWrite(gPin, 0);
			analogWrite(bPin, 0);
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
        ledColors[x] = CRGB(ARG_PRED * 0.30, ARG_PGREEN * 0.30, ARG_PBLUE * 0.30);

        if(x + 1 > ledCount)
        {
          for(byte c = 0; c < (x + 1) - ledCount; c++)
          {
            ledColors[c] = CRGB(ARG_PRED * 0.30, ARG_PGREEN * 0.30, ARG_PBLUE * 0.30);
          }
        }
      }
      else if(ledCount + (modeCurLed - ARG_LENGTH) <= x)
      {
        //light up the LED's on the end of the LED-strip
        ledColors[x] = CRGB(ARG_PRED * 0.30, ARG_PGREEN * 0.30, ARG_PBLUE * 0.30);
      }
      else
      {
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
      analogWrite(rPin, ARG_PRED);
      analogWrite(gPin, ARG_PGREEN);
      analogWrite(bPin, ARG_PBLUE);
    }
    else if (lightningStep == 40)
    {
      analogWrite(rPin, 0);
      analogWrite(gPin, 0);
      analogWrite(bPin, 0);
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
		analogWrite(rPin, shownRed);
		analogWrite(gPin, shownGreen);
		analogWrite(bPin, shownBlue);
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
		for(byte x = 0; x < ledCount; x++)
		{
			ledColors[x] = CRGB(shownRed, shownGreen, shownBlue);
		}
		
		FastLED.show();
	}
	else if(type == TYPE_4RGB)
	{
		analogWrite(rPin, shownRed);
		analogWrite(gPin, shownGreen);
		analogWrite(bPin, shownBlue);
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
		analogWrite(rPin, 0);
		analogWrite(gPin, 0);
		analogWrite(bPin, 0);
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

byte LEDItem::ID()
{
	return id;
}
