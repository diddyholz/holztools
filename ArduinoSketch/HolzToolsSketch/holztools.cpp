#include <FastLED.h>
#include <ArduinoTrace.h>
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

void LEDItem::ChangeMode(byte _mode, byte _arg1, byte _arg2, byte _arg3, int _music)
{
	curMode = _mode;
	
	arg1 = _arg1;
	speed = _arg1;
	passedMS = 0;
	arg2 = _arg2;
	direction = _arg2;
	arg3 = _arg3;
	
	lightningStep = 0;

	if(_music == 0)
		music = false;
	else
		music = true;
	
	//set the current led in lightning mode for right direction
	if(_arg2 == 0)
		modeCurLed = 0;   
	else
		modeCurLed = ledCount - 1;
	
	//reset the colors for the cycle mode to succesfully cycle
	if(_mode == MODE_CYCLE)
	{
		arg1 = 255;
		arg2 = 0;
		arg3 = 0;
	}
	//set the correct speed for lightning and static
	else if (_mode == MODE_LIGHTNING)
	{
		speed = 25;
	}
	else if (_mode == MODE_STATIC)
	{
		speed = 10;
	}
}

void LEDItem::SetSoundIntensity(byte _intensity)
{
	soundIntensity = _intensity;
}

void LEDItem::DisplayMode()
{
	if (passedMS == speed)
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
			case MODE_TEMPERATURE:
				Serial.println(F("MissingMo"));
				break;
			case MODE_OVERLAY:
				modeOverlay();
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
		for(int x = 0; x < alreadySetupPinsCount; x++)
		{
			if(alreadySetupPins[x] == dPin)
				return;
		}
		
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
		
		alreadySetupPins[alreadySetupPinsCount] = dPin;
		alreadySetupPinsCount++;
		
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
		
		skipB:
		return;
	}
}

//all the modes
void LEDItem::modeOverlay()
{
	setOverlayColor();

	if (music)
	{
		shownRed = arg1 * (float)((float)soundIntensity / 255.00);
		shownGreen = arg2 * (float)((float)soundIntensity / 255.00);
		shownBlue = arg3 * (float)((float)soundIntensity / 255.00);
	}
	else
	{
		shownRed = arg1;
		shownGreen = arg2;
		shownBlue = arg3;
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
	if (direction == 0)
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

		if (direction == 0)
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
			for (int x = 0; x < ledCount; x++)
			{
				ledColors[x] = CHSV(hue[x]++, 255, soundIntensity);
			}
		}
		else
		{
			//higher the hue
			for (int x = 0; x < ledCount; x++)
			{
				ledColors[x] = CHSV(hue[x]++, 255, arg2);
			}
		}

		FastLED.show();
	}
	else
	{
		//set the hue for each led
		for (int x = 0; x < ledCount; x++)
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
		if(modeCurLed != ledCount)
		{
			for(int x = 0; x <= ledCount; x++)
			{
				//check if the Led is the current displayed led
				if(x == modeCurLed)
				{
					ledColors[x] = CRGB(arg1, arg2, arg3);
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
				modeCurLed = 0;

				lightningStep = 0;
			}
		}
	}
	else if(type == TYPE_4RGB)
	{
		if (lightningStep == 0)
		{
			analogWrite(rPin, arg1);
			analogWrite(gPin, arg2);
			analogWrite(bPin, arg3);
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
		if(arg1 != 0)
		{
			arg1--;
			arg2++;
		}
		else 
		{
			redGoingDown = false;
			greenGoingDown = true;
		}
	} 
	else if(greenGoingDown)
	{
		if(arg2 != 0)
		{
			arg2--;
			arg3++;
		}
		else 
		{
		  greenGoingDown = false;
		  blueGoingDown = true;
		}
	}
	else if(blueGoingDown)
	{
		if(arg3 != 0)
		{
			arg3--;
			arg1++;
		}
		else 
		{
			blueGoingDown = false;
			redGoingDown = true;
		}
	}
  
	if(music)
	{
		shownRed = arg1 * (float)((float)soundIntensity / 255.00);
		shownGreen = arg2 * (float)((float)soundIntensity / 255.00);
		shownBlue = arg3 * (float)((float)soundIntensity / 255.00);
	}
	else
	{
		shownRed = arg1 * (float)((float)brightness / 255.00);
		shownGreen = arg2 * (float)((float)brightness / 255.00);
		shownBlue = arg3 * (float)((float)brightness / 255.00);
	}	
  
	if(type == TYPE_ARGB)
	{
		for(int x = 0; x < ledCount; x++)
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
		shownRed = arg1 * (float)((float)soundIntensity / 255.00);
		shownGreen = arg2 * (float)((float)soundIntensity / 255.00);
		shownBlue = arg3 * (float)((float)soundIntensity / 255.00);
	}
	else
	{
		shownRed = arg1;
		shownGreen = arg2;
		shownBlue = arg3;
	}
	
	if(type == TYPE_ARGB)
	{
		for(int x = 0; x < ledCount; x++)
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
		for(int x = 0; x < ledCount; x++)
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
			arg1 = 255;
			arg2 = 0;
			arg3 = 0;
			break;
			
		  case 1:
			arg1 = 255;
			arg2 = 127;
			arg3 = 0;
			break;
			
		  case 2:
			arg1 = 255;
			arg2 = 255;
			arg3 = 0;
			break;

		  case 3:
			arg1 = 127;
			arg2 = 255;
			arg3 = 0;
			break;

		  case 4:
			arg1 = 0;
			arg2 = 255;
			arg3 = 0;
			break;
			
		  case 5:
			arg1 = 0;
			arg2 = 255;
			arg3 = 127;
			break;

		  case 6:
			arg1 = 0;
			arg2 = 255;
			arg3 = 255;
			break;
			
		  case 7:
			arg1 = 0;
			arg2 = 127;
			arg3 = 255;
			break;

		  case 8:
			arg1 = 0;
			arg2 = 0;
			arg3 = 255;
			break;

		  case 9:
			arg1 = 127;
			arg2 = 0;
			arg3 = 255;
			break;
			
		  case 10:
			arg1 = 255;
			arg2 = 0;
			arg3 = 255;
			break;
			
		  case 11:
			arg1 = 255;
			arg2 = 0;
			arg3 = 127;
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

		if(direction == 0)
		{
			modeCurLed = 0;
		}
		else
		{
			modeCurLed = ledCount;  
		}
	}
}

int LEDItem::ID()
{
	return id;
}
