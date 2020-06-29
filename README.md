# HolzTools

### The cheap and open source way to control your LED-strips using an Arduino

![Application Preview](images/applicationPreview.png?raw=true "Application Preview")

## Features
* An easy to use and beginner friendly user interface
* Control 4-Pin RGB and 3-Pin A-RGB WS2812B LED-strips (other 3-Pin LEDs should also work but are not tested)
* Choose from multiple lightning effects (more are being developed)
* Automatic updates of the software on your Arduino
* Connect up to 3 different LED-strips on one Arduino
* Control every LED independent of your other LEDs 
* Up to 85 LEDs on one WS2812B strip are supported (This can be set higher by you)

## How to add your first LED 
* [Download HolzTools](https://github.com/diddyholz/HolzTools/releases)
* Click on the download button that is located in the title bar of the program
* In this window choose your Arduino Model and select the COM-Port where that Arduino is connected to
* Click on 'Upload to Arduino' to download and install the latest binary onto your Arduino
* Back in the main screen add an LED and press the wrench which appears when you hover over the added LED
* In that screen put in the following information
    * The type of your LED (4-Pin or 3-Pin)
    * The COM-Port at which the Arduino that controlls that LED is connected to
    * The pins where the LED is connected to. E.g. if the data input of your WS2812B strip is plugged into pin D3, put '3' into the D-Pin field. Do that for all connections of the LED that are plugged onto the Arduino, except the power connectors
* Click on apply and you are good to go!

## How to connect a 3-Pin addressable RGB LED-strip to an Arduino and make it work with HolzTools
* [This](https://www.youtube.com/watch?v=9hJyyUTflXA) Video explains the wiring very good (Of course you don't need to do the coding part from the video)
* Make sure you use a powerful enough power supply because these LEDs are very power hungry (One WS2812B LED at full brightness with white color draws 60 mA)
* In HolzTools you need to configure the following things for the LED 
    * LED type to 3-Pin
    * Configure on which pin the data connector is connected to
    * Put in the amount of LEDs that are on your LED-strip

![configure 3-Pin sample](images/config3Pin.png?raw=true "How to configure 3-Pin RGB")

## How to connect a 4-Pin LED-strip to an Arduino and make it work with HolzTools
* [This](https://www.youtube.com/watch?v=OR5JpFGJPf4) Video explains the wiring very good (Of course you don't need to copy the code from the video)
* If you want to use a longer LED-strip you need to use mosfets instead of transistors because most of them can't operate at the needed amperage
* Make sure you use a powerful enough power supply (Check the specifications of your LED-strip)
* In HolzTools you need to configure the following things for the LED 
    * LED type to 4-Pin 
    * Configure on which pin the red, green and blue connectors are connected to

![configure 4-Pin sample](images/config4Pin.png?raw=true "How to configure 4-Pin RGB")

## How to higher the ARGB LED amount cap of 85
Due to the low memory of an Arduino the amount of LEDs you can control on one WS2812B strip has to be capped at 85 to ensure stability when using 3 strips simultaneously. If you are planning to not use 3 strips simultaneously on one Arduino, you can set a higher LED limit.
* Open holztools.h in your Arduino IDE
* Search for `#define MAX_LEDS 85`
* Change the 85 to your desired amount (Do not set a too high value because then you will run into memory issues)

## Requirements
* I test everything on Windows 10 but Windows 7 and 8/8.1 should work too (not guaranteed)
* [Microsoft .NET 4.6 or higher](https://dotnet.microsoft.com/download/dotnet-framework)
* A bit of electrical engineering knowledge
* An Arduino
* LED-strips
* A power supply if needed
* Transistors/Mosfets if you want to use 4-Pin RGB Strips
* Resistors (about 1kOhm)

## Download
* [Main builds of HolzTools](https://github.com/diddyholz/HolzTools/releases)
* [Main builds of HolzTools Mobile (Android)](https://github.com/diddyholz/HolzTools/releases)

## Before you read the code
Sometimes Visual Studio destroyed the formatting of the XAML text and it looks extremly bad in some places.

I am very sorry for any grammar/spelling mistakes I made in the code. English is not my main language but I try to learn it every day. Also, I wrote most of the program very early in the morning or very late in the night so please don't hate me if I sometimes wrote ugly code :)

## About me
I am just a regular student living in a small village in germany who loves everything about tech. I started learning C++ when I was 12 years old but I never wrote anything useful in that language. After some time I saw another programming language which is called C# in a youtube video that got recommended to me. I quickly started to like that language because it allows you to develop pretty Windows applications. In the beggining I only used WinForms because I never knew that other UI frameworks existed. In summer 2019 I thought there must be something I was missing out. I was able to develop Windows Forms applications but they always felt very sluggish and didn't have any animations in it. The first version of HolzTools was for example a Forms application. But when I was finished with it I didn't really like the outcome because it did not feel like a "real" application. So I started googling and found out about WPF. After some time I got used to it and rewrote the whole HolzTools in WPF and added a lot of new things that where impossible to do with Windows Forms. Today I am sitting here and finally get to publish this program on which I have been working on next to school for many months.