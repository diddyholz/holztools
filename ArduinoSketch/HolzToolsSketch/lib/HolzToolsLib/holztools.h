#ifndef HOLZTOOLS_H
#define HOLZTOOLS_H

#include <FastLED.h>

#define MAX_LEDS 80
#define MAX_LEDITEMS 3

//define the modes with readable strings
#define MODE_STATIC 	 0
#define MODE_RAINBOW 	 1
#define MODE_CYCLE 		 2
#define MODE_LIGHTNING 3
#define MODE_MUSIC		 5
#define MODE_OVERLAY   6
#define MODE_SPINNER   7
#define MODE_OFF		   69

//define the types with readable strings
#define TYPE_ARGB		 0
#define TYPE_4RGB		 1

//define arguments
#define ARG_PRED        arg1
#define ARG_PGREEN      arg2
#define ARG_PBLUE       arg3
#define ARG_SRED        arg4
#define ARG_SGREEN      arg5
#define ARG_SBLUE       arg6
#define ARG_SPEED       arg7
#define ARG_DIRECTION   arg8
#define ARG_BRIGHTNESS  arg9
#define ARG_LENGTH      arg9

class LEDItem
{
	public:
		static LEDItem* ItemList[MAX_LEDITEMS];
		static byte ItemCount;
    
		LEDItem(byte _id);

		void SetLed(byte led, CRGB color);
		void SetUseMultiColor(bool value);
		void SetSyncParent(byte parent);
		byte GetSyncParent();
		byte GetID();
		void DisplayMode();
    	void Refresh();
		void ChangeMode(byte _mode, byte _arg1, byte _arg2, byte _arg3, byte _arg4, byte _arg5, byte _arg6,  byte _arg7, byte _arg8, byte _arg9, byte _music);
		void SetupItem(byte _type, byte _ledCount, byte _dPin, byte _rPin, byte _gPin, byte _bPin);
		void SetSoundIntensity(byte _intensity);
        //void PrintInfo();
        void SetupPins();
        int SaveData(int offset);
        int LoadData(int offset);
    
	private:
        #ifdef ESP32
        static bool setupPWMChannels[16];
        static bool setupFastLEDPins[34];

        bool rPinChanged = true;
        bool gPinChanged = true;
        bool bPinChanged = true;

        byte rPinChannel = 255;
        byte gPinChannel = 255;
        byte bPinChannel = 255;
        #endif
		byte type = 0;
		byte ledCount = 10;
		byte dPin = 0;
		byte rPin = 0;
		byte gPin = 0;
		byte bPin = 0;
		byte id = 0;
        byte syncParent = 255;
            
        byte curMode = 0;

        byte hue[MAX_LEDS];

        CRGB ledColors[MAX_LEDS];

        //for modes
        bool rnbwIsSetup = false;

        bool redGoingDown = true;
        bool blueGoingDown = false;
        bool greenGoingDown = false;

        bool useMultiColor = false;

        bool music = false;

        byte arg1 = 0;            //primary red
        byte arg2 = 0;            //primary green
        byte arg3 = 0;            //primary blue
        
        byte arg4 = 0;            //secondary red
        byte arg5 = 0;            //secondary green
        byte arg6 = 0;            //secondary blue
        
        byte arg7 = 0;            //speed
        byte arg8 = 0;            //direction
        byte arg9 = 0;            //brightness

        byte modeCurLed = 0;

        byte passedMS = 0;				//the passed miliseconds after a mode was updated

        byte lightningStep = 0;
        
        byte curColor = 0;

        byte soundIntensity = 0;

        byte shownRed = 0;
        byte shownGreen = 0;
        byte shownBlue = 0;

        //declare functions
        void modeCycle();
        void modeStatic();
        void modeLightning();
        void modeRainbow();
        void modeOverlay();
        void setOverlayColor();
        void modeSpinner();
        void modeOff();
};

#endif
