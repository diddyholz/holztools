// #include <MemoryFree.h>
#include "holztools.h"
#include <EEPROM.h>

#define BAUDRATE 4800
#define PORT 39769
#define ESP32
#define ESP_LOW_POWER_FREQ 80
#define HOSTNAME_LENGTH 13

#ifdef ESP32
#include <BluetoothSerial.h>
#include <WiFi.h>
#include "OTA.h"
#endif

const char* binaryVer = "1.07";

#ifdef ESP32
const char* controllerModel = "ESP32";
#else
const char* controllerModel = "NanoR3";
#endif

String usbMessage = "";

bool stringComplete = false;
bool backslash = false;
bool isMusicData = false;
bool isCommand = false;
bool isMultiColor = false;

//declare functions
void decodeMessage(String message);

#ifdef ESP32
TaskHandle_t taskHandle;
BluetoothSerial btConnection;
WiFiServer server(PORT);

const char* TCPGETINFO = "GETINFO";
const char* TCPOK = "200";
const char* TCPINVALIDCOMMAND = "400";

char hostname[HOSTNAME_LENGTH] = "";
char ssid[65] = "";
char password[65] = ""; 
char command[65] = "";

String httpHeader = "";

bool connectingWiFi = false;

unsigned char h2int(char c);
void urldecode();
void serialTaskFunc(void* parameter);
void networkEvent();
void connectToWiFiTask(void* parameter);
void serialEvent();
void btEvent();
void loadConfig();
void saveConfig();
void generateHostname();
void resetConfig();
bool setupWiFiConnection(const char* ssid, const char* password);
#endif

void setup() 
{
    Serial.begin(BAUDRATE);

    #ifdef ESP32
    EEPROM.begin(4096);

    loadConfig();

    if (hostname[0] == 0)
    {
        generateHostname();
        saveConfig();
    }

    // set hostnames
    btConnection.begin(hostname);
    ArduinoOTA.setHostname(hostname);
    WiFi.setHostname(hostname);

    xTaskCreatePinnedToCore(connectToWiFiTask, "WiFiTask", 6656, NULL, 0, &taskHandle, 0);

    setCpuFrequencyMhz(ESP_LOW_POWER_FREQ);
    #endif

    // load the information of all previous ledItems
    byte ledCount = 0;
    EEPROM.get(0, ledCount);
    
    Serial.print(F("Ledcount: "));
    Serial.println(ledCount);

    // stop loading if ledCount is 255 (default value)
    if(ledCount == 255 || ledCount == 0 || ledCount > MAX_LEDS + 1) return;

    int offset = sizeof(byte);

    for(byte x = 0; x < ledCount; x++)
    {
        Serial.print(F("Reading at address: "));
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

            #ifdef ESP32
            EEPROM.commit();
            #endif
        }
    }

    //display the active mode on each leditem
    for(byte x = 0; x < LEDItem::ItemCount; x++)
    {
        LEDItem::ItemList[x]->DisplayMode();
    }

    #ifdef ESP32
    ArduinoOTA.handle();
    serialEvent();
    btEvent();
    networkEvent();
    #endif    
    
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
                    char temp[sizeof(binaryVer) + sizeof(controllerModel)] = { 0 };
                    snprintf(temp, sizeof(temp), "_%s_%s", binaryVer, controllerModel);
                    Serial.print(temp);
                }
                else if(usbMessage == "_CLRROM\\n")
                {
                    #ifdef ESP32
                    resetConfig();
                    #else
                    // reset saved leditemcount to 0
                    EEPROM.put(0, (byte)0);
                    #endif            

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
void generateHostname()
{
    strcpy(hostname, "ESP32-");

    for (byte x = 0; x < HOSTNAME_LENGTH - sizeof("ESP32-"); x++)
    {
        // generate random digits and append them to the hostname
        hostname[(sizeof("ESP32-") - 1) + x] = '0' + (esp_random() % 9);
    }

    hostname[12] = 0;

    Serial.print(F("Generated hostname: "));
    Serial.println(hostname);
    Serial.flush();
}

unsigned char h2int(char c)
{
    if (c >= '0' && c <='9'){
        return((unsigned char)c - '0');
    }
    if (c >= 'a' && c <='f'){
        return((unsigned char)c - 'a' + 10);
    }
    if (c >= 'A' && c <='F'){
        return((unsigned char)c - 'A' + 10);
    }
    return(0);
}

void urldecode()
{
    byte index = 0;

    char c;
    char code0;
    char code1;

    for (int i = 0; i < sizeof(command); i++)
    {
        c = command[i];

        if (c == '+')
        {
            command[index] = ' ';  
            index++;
        }
        else if (c == '%') 
        {
            i++;
            code0 = command[i];
            i++;
            code1 = command[i];
            c = (h2int(code0) << 4) | h2int(code1);
            command[index] = c;
            index++;
        } 
        else
        {            
            command[index] = c;  
            index++;
        }
    }
}

bool setupWiFiConnection(const char* ssid, const char* password)
{
    server.end();
    
    Serial.print(F("Connecting to "));
    Serial.print(ssid);
    Serial.print(F(" using password "));
    Serial.println(password);
    
    byte temp = 0;

    retry:

    WiFi.begin(ssid, password);

    unsigned long time = millis();

    for(byte x = 0; x < 100; x++)
    {
        if(WiFi.status() != WL_CONNECTED)
            break;
        
        delay(100);
    }
    
    
    while(WiFi.status() != WL_CONNECTED)
    {
        if(millis() - time > 5000)
        {
            if(temp < 3)
            {
                temp++;
                goto retry;
            }
            else 
            {
                return false;
            }
        }
    }

    server.begin();
    setupOTA(&btConnection, &taskHandle);
    
    return true;
}

void connectToWiFiTask(void* parameter)
{
    // load the network config
    while(true)
    {
        if(WiFi.status() != WL_CONNECTED && ssid[0] != 0 && password[0] != 0 && !connectingWiFi)
        {
            byte temp = 0;

            while(temp < 3)
            {
                if(setupWiFiConnection(ssid, password))
                {
                    Serial.println(F("Successfully connected to saved network"));
                    Serial.print(F("IP-address: "));
                    Serial.println(WiFi.localIP());

                    temp = 3;
                }
                else 
                {
                    temp++;

                    Serial.println(F("Cannot connect to saved network, retrying"));
                }
            }
        }
        
        delay(10000);   
    }
}

void btEvent()
{
    while(btConnection.available())
    {
        char c = btConnection.read();

        if(c == '\\')
        {
            backslash = true;
        }
        else if(backslash && c == 'n')
        {
            if(usbMessage.indexOf("|") > 1)
            {
                strcpy(ssid, usbMessage.substring(0, usbMessage.indexOf("|")).c_str());
                strcpy(password, usbMessage.substring(usbMessage.indexOf("|") + 1, usbMessage.length()).c_str());

                connectingWiFi = true;

                if(setupWiFiConnection(ssid, password))
                {
                    while (WiFi.localIP().toString().startsWith("0"))
                    {
                        delay(10);
                    }

                    btConnection.println("#" + WiFi.localIP().toString() + "\n");          
                    btConnection.println(F("#YESCONNECT\n"));

                    saveConfig();
                }
                else
                {
                    btConnection.println(F("#NOCONNECT\n"));
                }

                connectingWiFi = false;
            }

            usbMessage = "";
        }
        else 
        {
            usbMessage += c;
        }
    }
}

void networkEvent()
{
    WiFiClient client = server.available();

    if(client)
    {
        Serial.println(F("Client connected"));

        String currentLine = "";       

        unsigned long time = millis();

        while (client.connected() && millis() - time <= 5000) 
        {  
            if (client.available()) 
            {             
                char c = client.read();                
                httpHeader += c;
                
                if (c == '\n') 
                {                    
                    if (currentLine.length() == 0) 
                    {
                        client.println("HTTP/1.1 200 OK");
                        client.println("Content-type:text/html");
                        client.println("Connection: close");
                        client.println();

                        byte index = 0;

                        // get the command
                        for(int x = httpHeader.indexOf("Command=") + sizeof("Command=") - 1; x < httpHeader.length(); x++)
                        {
                            if(httpHeader[x] != ' ')
                                command[index] = httpHeader[x];
                            else 
                                break;

                            index++;
                        }

                        urldecode();

                        Serial.println(command);

                        if(strcmp(command, TCPGETINFO) == 0)
                        {
                            char response[65] = { 0 };
                            snprintf(response, sizeof(response), "Hostname=%s&Version=%s&Model=%s", hostname, binaryVer, controllerModel);
                            client.print(response);
                        }
                        else
                        {
                            // check if the command is valid
                            bool containsEnd = false;
                            bool containsStart = false;

                            for(byte x = 0; x < sizeof(command); x++)
                            {
                                if(command[x] == '\\')
                                    backslash = true;
                                else if(command[x] == 'n' && backslash)
                                    containsEnd = true;
                            }

                            if(command[0] == '#')
                                containsStart = true;

                            backslash = false;

                            if(containsStart && containsEnd)
                            {
                                client.print(TCPOK);
                                usbMessage = command;

                                stringComplete = true;
                            }
                            else
                            {
                                client.print(TCPINVALIDCOMMAND);
                            }
                        }

                        for(byte x = 0; x < sizeof(command); x++)
                        {
                            command[x] = 0;
                        }
                        
                        // Break out of the while loop
                        break;
                    } 
                    else 
                    {
                         // if you got a newline, then clear currentLine
                        currentLine = "";
                    }
                } 
                else if (c != '\r') 
                {  // if you got anything else but a carriage return character,
                    currentLine += c;      // add it to the end of the currentLine
                }
            }
        }
    
        // Clear the header variable
        httpHeader = "";
        // Close the connection
        client.stop();
        Serial.println("Client disconnected");
        Serial.println("");
    }
}

void resetConfig()
{
    for(int x = 0; x < 4096; x++)
    {
        EEPROM.put(x, (byte)0);
    }

    EEPROM.commit();
}

void saveConfig()
{
    int16_t pointer = 4095;

    // save networkconfig
    EEPROM.put(pointer, 'n');
    pointer--;
    EEPROM.put(pointer, 'e');
    pointer--;
    EEPROM.put(pointer, '=');
    pointer--;
    EEPROM.put(pointer, ssid[0] != 0);
    pointer--;
    EEPROM.put(pointer, '&');
    pointer--;

    Serial.print(F("Saved ne: "));
    Serial.println(ssid[0] != 0);

    if(ssid[0] != 0)
    {
        EEPROM.put(pointer, 'n');
        pointer--;
        EEPROM.put(pointer, 'n');
        pointer--;
        EEPROM.put(pointer, '=');
        pointer--;
        
        for(byte x = 0; x < sizeof(ssid); x++)
        {
            if(ssid[x] != 0)
            {
                EEPROM.put(pointer, ssid[x]);
                pointer--;
            }
        }

        EEPROM.put(pointer, '&');
        pointer--;

        Serial.print(F("Saved nn: "));
        Serial.println(ssid);
    }

    if(ssid[0] != 0)
    {
        EEPROM.put(pointer, 'n');
        pointer--;
        EEPROM.put(pointer, 'p');
        pointer--;
        EEPROM.put(pointer, '=');
        pointer--;
        
        for(byte x = 0; x < sizeof(password); x++)
        {
            if(password[x] != 0)
            {
                EEPROM.put(pointer, password[x]);
                pointer--;
            }
        }

        EEPROM.put(pointer, '&');
        pointer--;

        Serial.print(F("Saved np: "));
        Serial.println(password);
    }

    EEPROM.put(pointer, 'h');
    pointer--;
    EEPROM.put(pointer, 'n');
    pointer--;
    EEPROM.put(pointer, '=');
    pointer--;

    for (int x = 0; x < sizeof(hostname); x++)
    {
        if(hostname[x] != 0)
        {
            EEPROM.put(pointer, hostname[x]);
            pointer--;
        }
    }

    EEPROM.put(pointer, '&');
    pointer--;

    Serial.print(F("Saved hn: "));
    Serial.println(hostname);

    // put a 0 to symbolize end of config 
    EEPROM.put(pointer, (byte)0);

    EEPROM.commit();
}

void loadConfig()
{
    int16_t pointer = 4095;

    char c = 0;
    char arg[3] = {0, 0}; 

    byte counter = 0;

    bool networkConfigSaved = false;

    EEPROM.get(pointer, c);
    pointer--;

    Serial.println(c);

    while (c != 0)
    {
        if(c == '=')
        {
            Serial.print(F("Read arg: "));
            Serial.println(arg);

            // check which type of argument was read
            if(strncmp(arg, "ne", 2) == 0)
            {
                EEPROM.get(pointer, c);
                pointer--;

                networkConfigSaved = c;

                Serial.print(F("Network config available: "));
                Serial.println((byte)c);
                
                // skip the '&' symbol
                pointer--;
            }
            else if(strncmp(arg, "nn", 2) == 0 && networkConfigSaved)
            {
                char argC = 0;
                
                byte x = 0; 

                EEPROM.get(pointer, argC);
                pointer--;

                while(argC != '&')
                {
                    ssid[x] = argC;

                    EEPROM.get(pointer, argC);
                    pointer--;
                    
                    x++;
                }

                for(; x < sizeof(ssid); x++)
                {
                    ssid[x] = 0;
                }

                Serial.print(F("Loaded ssid: "));
                Serial.println(ssid);
            }
            else if(strncmp(arg, "np", 2) == 0 && networkConfigSaved)
            {

                char argC = 0;
                
                byte x = 0; 

                EEPROM.get(pointer, argC);
                pointer--;

                while(argC != '&')
                {
                    password[x] = argC;

                    EEPROM.get(pointer, argC);
                    pointer--;
                    
                    x++;
                }

                Serial.print(F("3. ssid[0]: "));
                Serial.println((byte)ssid[0]);

                for(; x < sizeof(password); x++)
                {
                    password[x] = 0;
                }

                Serial.print(F("4. ssid[0]: "));
                Serial.println((byte)ssid[0]);

                Serial.print(F("Loaded password: "));
                Serial.println(password);
            }
            else if(strncmp(arg, "hn", 2) == 0)
            {
                char argC = 0;
                
                byte x = 0; 

                EEPROM.get(pointer, argC);
                pointer--;

                while(argC != '&')
                {
                    hostname[x] = argC;

                    EEPROM.get(pointer, argC);
                    pointer--;
                    
                    x++;
                }

                for(; x < sizeof(hostname); x++)
                {
                    hostname[x] = 0;
                }

                Serial.print(F("Loaded hostname: "));
                Serial.println(hostname);
            }

            counter = 0;
        }
        else 
        {
            arg[counter] = c;
            counter++;
        }

        EEPROM.get(pointer, c);
        pointer--;
    }
}

void serialTaskFunc(void* parameter)
{
    while (true)
    {
        serialEvent();
        btEvent();
        networkEvent();
        delay(1);
    }
}
#endif