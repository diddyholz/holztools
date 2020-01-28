#ifndef HOLZTOOLS_H
#define HOLZTOOLS_H

#include <FastLED.h>

//define the modes with readable strings
#define MODE_STATIC 	 0
#define MODE_RAINBOW 	 1
#define MODE_CYCLE 		 2
#define MODE_LIGHTNING	 3
#define MODE_TEMPERATURE 4
#define MODE_MUSIC		 5
#define MODE_OVERLAY	 6
#define MODE_OFF		 69

//define the types with readable strings
#define TYPE_ARGB		 0
#define TYPE_4RGB		 1

//define arguments
#define brightness 			direction

class LEDItem
{
	public:
		static LEDItem* ItemList[3];
		static byte ItemCount;
		LEDItem(byte _id);
		int ID();
		void DisplayMode();
		void ChangeMode(byte _mode, byte _arg1, byte _arg2, byte _arg3, int _music);
		void SetupItem(byte _type, byte _ledCount, byte _dPin, byte _rPin, byte _gPin, byte _bPin);
		void SetSoundIntensity(byte _intensity);
	private:
		byte type = 0;
		byte ledCount = 10;
		byte dPin = 0;
		byte rPin = 0;
		byte gPin = 0;
		byte bPin = 0;
		byte id = 0;
		
		byte alreadySetupPinsCount = 0;
		byte alreadySetupPins[7];
		
		byte curMode = 0;

		byte hue[90];

		CRGB ledColors[90];

		//for modes
		bool rnbwIsSetup = false;

		bool redGoingDown = true;
		bool blueGoingDown = false;
		bool greenGoingDown = false;

		bool music = false;

		byte arg1 = 0;
		byte arg2 = 0;
		byte arg3 = 0;

		byte modeCurLed = 0;

		byte direction = 0;

		byte speed = 0;
		byte passedMS = 0;				//the passed miliseconds after a mode was updated

		byte lightningStep = 0;
		
		byte curColor = 0;

		byte soundIntensity = 0;

		byte shownRed = 0;
		byte shownGreen = 0;
		byte shownBlue = 0;

		//declare functions
		void setupPins();
		void modeCycle();
		void modeStatic();
		void modeLightning();
		void modeRainbow();
		void modeOverlay();
		void setOverlayColor();
		void modeOff();
};

#endif