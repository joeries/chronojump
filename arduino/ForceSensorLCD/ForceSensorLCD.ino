

/*
  #
  #  This file is part of ChronoJump
  #
  #  ChronoJump is free software; you can redistribute it and/or modify
  #   it under the terms of the GNU General Public License as published by
  #    the Free Software Foundation; either version 2 of the License, or
  #     (at your option) any later version.
  #
  #  ChronoJump is distributed in the hope that it will be useful,
  #   but WITHOUT ANY WARRANTY; without even the implied warranty of
  #    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  #     GNU General Public License for more details.
  #
  #  You should have received a copy of the GNU General Public License
  #   along with this program; if not, write to the Free Software
  #    Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
  #
  #   Copyright (C) 2017     Xavier Padullés <x.padulles@gmail.com>
  #   Copyright (C) 2017     Xavier de Blas <xaviblas@gmail.com>


*/

#include <HX711.h>
#include <EEPROM.h>
#include <LiquidCrystal.h>

#define DOUT  5
#define CLK  4

//Version number //it always need to start with: "Force_Sensor-"
String version = "Force_Sensor-0.4";


int tareAddress = 0;
int calibrationAddress = 4;

HX711 scale(DOUT, CLK);

//Data comming from the cell after resting the offset weight
float offsetted_data = 0;

//Data resulting of appying the calibration_factor to the offsetted_data
float scaled_data = 0;

//The weight used to calibrate the cell
float weight = 0.0;

//Wether the sensor has to capture or not
boolean capturing = false;

//wether the tranmission is in binary format or not
boolean binaryFormat = false;

unsigned long lastTime = 0;

//RFD variables
//for RFD cannot used lastTime, can have overflow problems. Better use elapsedTime
unsigned long rfdTimePre = 0;
unsigned long rfdTimePre2 = 0;
float rfdMeasuredPre = 0;
float rfdMeasuredPre2 = 0;
bool rfdDataPreOk = false;
bool rfdDataPre2Ok = false;
bool rfdCalculed = false;
float rfdValueMax = 0;


unsigned long currentTime = 0;
unsigned long elapsedTime = 0;
unsigned long totalTime = 0;
unsigned int samples = 0;

const int buttonPin = 7;
int buttonState;
float voltage;


unsigned int lcdDelay = 25; //to be able to see the screen. Seconds are also printed in delay but 25 values are less than one second
unsigned int lcdCount = 0;
float measuredLcdDelayMax = 0; //The max in the lcdDelay period
float measuredMax = 0; // The max since starting capture
float measured = scale.get_units();


LiquidCrystal lcd(12, 11, 10, 8, 3, 2);

void setup() {
  pinMode(buttonPin, INPUT);
  analogWrite(6, 20);
  lcd.begin(16, 2);
  Serial.begin(115200);

  if (buttonState == 0) {
    lcd.setCursor(2, 0);
    lcd.print("CHRONOJUMP");
    lcd.setCursor(2, 1);
    lcd.print("Boscosystem");
    kangaroo();
  }



  long tare = 0;
  EEPROM.get(tareAddress, tare);
  if (tare == -151) {
    scale.set_offset(10000);// Usual value  in Chronojump strength gauge
    EEPROM.put(tareAddress, 10000);
  } else {
    scale.set_offset(tare);
  }


  //The factor to convert the units coming from the cell to the units used in the calibration
  float calibration_factor = 0.0f;
  EEPROM.get(calibrationAddress, calibration_factor);
  if (isnan(calibration_factor)) {
    scale.set_scale(915.0);// Usual value  in Chronojump strength gauge
    EEPROM.put(calibrationAddress, 915.0);
  } else {
    scale.set_scale(calibration_factor);
  }
}

void loop()
{
  buttonState = digitalRead(buttonPin);
  if (buttonState == 1) {
    start_capture();
    delay(1000);
  }

  if (capturing)
  {
    currentTime = micros();

    //Managing the timer overflow
    if (currentTime > lastTime)     //No overflow
      elapsedTime = currentTime - lastTime;
    else  if (currentTime <= lastTime) //Overflow
      elapsedTime = (4294967295 - lastTime) + currentTime; //Time from the last measure to the overflow event plus the currentTime

    //calculations
    totalTime += elapsedTime;
    lastTime = currentTime;
    float measured = scale.get_units();

    //RFD stuff start ------>
    if (rfdDataPre2Ok) {
      float rfdValue =  (measured - rfdMeasuredPre2) / ((elapsedTime + rfdTimePre) / 1000000.0);
      rfdCalculed = true;
      if (rfdValue > rfdValueMax) {
        rfdValueMax = rfdValue;
      }
    }

    if (rfdDataPreOk) {
      rfdTimePre2 = rfdTimePre;
      rfdMeasuredPre2 = rfdMeasuredPre;
      rfdDataPre2Ok = true;
    }

    rfdTimePre = elapsedTime;
    rfdMeasuredPre = measured;
    rfdDataPreOk = true;
    //<------- RFD stuff end

    if (measured > measuredLcdDelayMax) {
      measuredLcdDelayMax = measured;
    }
    if (measured > measuredMax) {
      measuredMax = measured;
    }

    Serial.print(totalTime); Serial.print(";");
    Serial.println(measured, 2); //scale.get_units() returns a float

    printOnLcd();
  }
}
void printOnLcd() {
  lcdCount = lcdCount + 1;
  if (lcdCount >= lcdDelay)
  {
    lcd.clear();
    //print Battery level
    float sensorValue = analogRead(A0);
    voltage = sensorValue * (5.00 / 1023.00) * 3;
    Serial.println(voltage);
    float percent = (voltage - 6.35) / 0.0635;

    if (voltage < 4.5) {
      lcd.setCursor(13, 0);
      lcd.print("USB");
    }
    if (voltage > 4.5 && voltage < 12.5) {
      printLcdMeu(percent, 14, 0, 0);
      lcd.print("%");
    }
    if (voltage > 12.5) {
      lcd.setCursor(13, 0);
      lcd.print(percent, 0);
      lcd.print("%");
    }
    printLcdMeu (measuredLcdDelayMax, 3, 0, 1);
    printLcdMeu (measuredMax, 3, 1, 1);
    int totalTimeInSec = totalTime / 1000000;
    printLcdMeu (totalTimeInSec, 10, 0, 0);

    if (rfdCalculed) {
      printLcdMeu (rfdValueMax, 13, 1, 1);

      measuredLcdDelayMax = 0;
      lcdCount = 0;
    }

  }
}


void printLcdMeu (float val, int xStart, int y, int decimal) {

  if (val < 10) {
    lcd.setCursor(xStart  , y);
    lcd.print(val, decimal);
  }
  if (val >= 10 && val < 100) {
    lcd.setCursor(xStart - 1, y);
    lcd.print(val, decimal);
  }
  if (val >= 100 && val < 1000) {
    lcd.setCursor(xStart - 2, y);
    lcd.print(val, decimal);
  }
  if (val >= 1000 && val < 10000) {
    lcd.setCursor(xStart - 3, y);
    lcd.print(val, decimal);
  }
  if (val > 10000) {
    lcd.setCursor(xStart - 4, y);
    lcd.print(val, decimal);
  }
}

void kangaroo() {
  byte kangaroo1[] = {
    B00000,
    B00000,
    B00000,
    B10001,
    B11011,
    B01110,
    B00100,
    B00000
  };
  byte kangaroo2[] = {
    B00110,
    B01111,
    B11111,
    B11111,
    B01000,
    B01100,
    B00100,
    B00110
  };
  byte kangaroo3[] = {
    B01000,
    B00100,
    B11110,
    B11111,
    B11000,
    B01000,
    B10000,
    B00000
  };
  lcd.createChar(0, kangaroo1);
  lcd.setCursor(13, 0);
  lcd.write(byte (0));
  lcd.createChar(1, kangaroo2);
  lcd.setCursor(14, 0);
  lcd.write(byte(1));
  lcd.createChar(2, kangaroo3);
  lcd.setCursor(15, 0);
  lcd.write(byte(2));
}


void serialEvent() {
  String inputString = Serial.readString();
  String commandString = inputString.substring(0, inputString.lastIndexOf(":"));
  //  while (Serial.available())
  //  {
  //    char inChar = (char)Serial.read();
  //    inputString += inChar;
  //    if (inChar == '\n') {
  //       commandString = inputString.substring(0, inputString.lastIndexOf(":"));
  //    }




  if (commandString == "start_capture") {
    start_capture();
  } else if (commandString == "end_capture") {
    end_capture();
  } else if (commandString == "get_version") {
    get_version();
  } else if (commandString == "get_calibration_factor") {
    get_calibration_factor();
  } else if (commandString == "set_calibration_factor") {
    set_calibration_factor(inputString);
  } else if (commandString == "calibrate") {
    calibrate(inputString);
  } else if (commandString == "get_tare") {
    get_tare();
  } else if (commandString == "set_tare") {
    set_tare(inputString);
  } else if (commandString == "tare") {
    tare();
  } else if (commandString == "get_transmission_format") {
    get_transmission_format();
  } else {
    Serial.println("Not a valid command");
  }
  inputString = "";

}

void start_capture()
{
  lcd.clear();
  Serial.println("Starting capture...");
  totalTime = 0;
  lastTime = micros();
  measuredMax = 0;
  //samples = 0;
  rfdDataPreOk = false;
  rfdDataPre2Ok = false;
  rfdCalculed = false;
  rfdValueMax = 0;
  capturing = true;

}

void end_capture()
{
  capturing = false;
  Serial.print("Capture ended:");
  Serial.println(scale.get_offset());
}
void get_version()
{
  Serial.println(version);
}

void get_calibration_factor()
{
  Serial.println(scale.get_scale());
}

void set_calibration_factor(String inputString)
{
  //Reading the argument of the command. Located within the ":" and the ";"
  String calibration_factor = get_command_argument(inputString);
  //Serial.println(calibration_factor.toFloat());
  scale.set_scale(calibration_factor.toFloat());
  float stored_calibration = 0.0f;
  EEPROM.get(calibrationAddress, stored_calibration);
  if (stored_calibration != calibration_factor.toFloat()) {
    EEPROM.put(calibrationAddress, calibration_factor.toFloat());
  }
  Serial.println("Calibration factor set");
}

void calibrate(String inputString)
{
  //Reading the argument of the command. Located within the ":" and the ";"
  String weightString = get_command_argument(inputString);
  float weight = weightString.toFloat();
  //mean of 255 values comming from the cell after resting the offset.
  double offsetted_data = scale.get_value(50);

  //offsetted_data / calibration_factor
  float calibration_factor = offsetted_data / weight / 9.81; //We want to return Newtons.
  scale.set_scale(calibration_factor);
  EEPROM.put(calibrationAddress, calibration_factor);
  Serial.print("Calibrating OK:");
  Serial.println(calibration_factor);
}

void tare()
{
  scale.tare(50); //Reset the scale to 0 using the mean of 255 raw values
  EEPROM.put(tareAddress, scale.get_offset());
  Serial.print("Taring OK:");
  Serial.println(scale.get_offset());
}

void get_tare()
{
  Serial.println(scale.get_offset());
}

void set_tare(String inputString)
{
  String tare = get_command_argument(inputString);
  long value = tare.toInt();
  scale.set_offset(value);
  long stored_tare = 0;
  EEPROM.get(tareAddress, stored_tare);
  if (stored_tare != value) {
    EEPROM.put(tareAddress, value);
    Serial.println("updated");
  }
  Serial.println("Tare set");
}

String get_command_argument(String inputString)
{
  return (inputString.substring(inputString.lastIndexOf(":") + 1, inputString.lastIndexOf(";")));
}

void get_transmission_format()
{
  if (binaryFormat)
  {
    Serial.println("binary");
  } else
  {
    Serial.println("text");
  }
}
