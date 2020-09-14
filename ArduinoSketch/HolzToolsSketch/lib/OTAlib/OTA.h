#ifdef ESP32
#include <WiFi.h>
#include <ESPmDNS.h>
#else
#include <ESP8266WiFi.h>
#include <ESP8266mDNS.h>
#endif

#include <ArduinoHttpClient.h>
#include <WiFiUdp.h>
#include <ArduinoOTA.h>

bool hasBlinked = false;
bool progressShown = false;

byte lastProgress = 0;

// const char* ssid = mySSID;
// const char* password = myPASSWORD;

#if defined(ESP32_RTOS) && defined(ESP32)
void taskOne( void * parameter )
{
  ArduinoOTA.handle();
  delay(3500);
}
#endif

void setupOTA(BluetoothSerial* btConnection, TaskHandle_t* taskHandle) {
  const int maxlen = 40;
  char fullhostname[maxlen];
  uint8_t mac[6];
  WiFi.macAddress(mac);

  // Port defaults to 3232
  // ArduinoOTA.setPort(3232);

  // No authentication by default
  // ArduinoOTA.setPassword("admin");

  // Password can be set with it's md5 value as well
  // MD5(admin) = 21232f297a57a5a743894a0e4a801fc3
  // ArduinoOTA.setPasswordHash("21232f297a57a5a743894a0e4a801fc3");

  ArduinoOTA.onStart([=]() {
      // stop network connection task
      vTaskDelete(*taskHandle);

    String type;
    if (ArduinoOTA.getCommand() == U_FLASH)
      type = "sketch";
    else // U_SPIFFS
      type = "filesystem";

    // set the pinmode for the status led
    pinMode(2, OUTPUT);

    setCpuFrequencyMhz(240);

    // NOTE: if updating SPIFFS this would be the place to unmount SPIFFS using SPIFFS.end()
    Serial.println("Start updating " + type);
  });
  ArduinoOTA.onEnd([=]() {
    btConnection->println("+\n");

    digitalWrite(2, HIGH);
    delay(2000);
    digitalWrite(2, LOW);
    delay(200);
    digitalWrite(2, HIGH);
    delay(2000);
    digitalWrite(2, LOW);
  });
  ArduinoOTA.onProgress([=](unsigned int progress, unsigned int total) {

    if(!progressShown)
    {
        lastProgress = progress / (total / 100);
        btConnection->println("#" + String(lastProgress) + "\n");
        btConnection->flush();
        progressShown = true;
    }
    else if(lastProgress != (progress / (total / 100)))
    {
        progressShown = false;
    }
    
    // flash the led every 5 percent
    if((progress / (total / 100)) % 5 == 0 && !hasBlinked)
    {
        digitalWrite(2, HIGH);
        delay(50);
        digitalWrite(2, LOW);

        hasBlinked = true;
    }
    else if((progress / (total / 100)) % 5 != 0)
    {
        hasBlinked = false;
    }
  });
  ArduinoOTA.onError([=](ota_error_t error) {
    Serial.printf("Error[%u]: ", error);
    if (error == OTA_AUTH_ERROR) btConnection->println("-Auth Failed\n");
    else if (error == OTA_BEGIN_ERROR) btConnection->println("-Begin Failed\n");
    else if (error == OTA_CONNECT_ERROR) btConnection->println("-Connect Failed\n");
    else if (error == OTA_RECEIVE_ERROR) btConnection->println("-Receive Failed\n");
    else if (error == OTA_END_ERROR) btConnection->println("-End Failed\n");

    digitalWrite(2, HIGH);
    delay(2000);
    digitalWrite(2, LOW);
    delay(200);
    digitalWrite(2, HIGH);
    delay(2000);
    digitalWrite(2, LOW);
    delay(200);
    digitalWrite(2, HIGH);
    delay(2000);
    digitalWrite(2, LOW);
    delay(200);
    digitalWrite(2, HIGH);
    delay(2000);
    digitalWrite(2, LOW);
  });

  ArduinoOTA.begin();

  Serial.println("OTA Initialized");
  Serial.print("IP address: ");
  Serial.println(WiFi.localIP());

#if defined(ESP32_RTOS) && defined(ESP32)
  xTaskCreate(
    ota_handle,          /* Task function. */
    "OTA_HANDLE",        /* String with name of task. */
    10000,            /* Stack size in bytes. */
    NULL,             /* Parameter passed as input of the task */
    1,                /* Priority of the task. */
    NULL);            /* Task handle. */
#endif
}