/**
   Copyright(C) 2016 Singapore ETH Centre, Future Cities Laboratory
   All rights reserved.

   This software may be modified and distributed under the terms
   of the MIT license.See the LICENSE file for details.

   Author:  Filip Schramka (schramka@arch.ethz.ch)
   Summary: This software runs on a adafruit feather. A MPU 6050
            Gyroscope & Accelerometer is attached. The meassured
            quaternion will get delivered over bluetooth with a
            bluefruit EZ-Link device (Serial1) or over USB
            (Serial) to the computer.
            MPU6050 needs SDA/SCL & EZLink needs RX/TX.
            In that case all regular hardware interrupt ports
            are occupied.Interrupts from the MPU 6050 can
            be attached on PCINT ports with the
            PCattachInterrupt(...) method.

   Credit:
            This piece of software contains parts written by:

            I2Cdev device library code is placed under the MIT license
            Copyright (c) 2012 Jeff Rowberg
            https://github.com/jrowberg/i2cdevlib

            Permission is hereby granted, free of charge, to any person obtaining a copy
            of this software and associated documentation files (the "Software"), to deal
            in the Software without restriction, including without limitation the rights
            to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
            copies of the Software, and to permit persons to whom the Software is
            furnished to do so, subject to the following conditions:

            The above copyright notice and this permission notice shall be included in
            all copies or substantial portions of the Software.

            THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
            IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,$
            FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
            AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
            LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
            OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
            THE SOFTWARE.

            -----------------------------------------------------------------------------

            PCINT Tutorial
            Author unknown
            http://playground.arduino.cc/Main/PcInt

*/


/* Pin to interrupt map:
   PIN 9-11 = PortB (PB) 5-7 = PCINT 5-7 = pcmsk0 bits 5-7 = PCICR Bit 1
*/

#include "I2Cdev.h"
#include "MPU6050_6Axis_MotionApps20.h"

#if I2CDEV_IMPLEMENTATION == I2CDEV_ARDUINO_WIRE
#include "Wire.h"
#endif

// choose output, Serial is USB, Serial1 is Bluetooth (just if EZLink module attached)
#define COM             Serial1
// choose PCINT pin --> where you attached the interrupt signal from MPU6050
#define PCINT_PIN       15

// MPU control / status variables
MPU6050 mpu;
uint8_t devStatus;
uint8_t mpuIntStatus;
uint16_t packetSize;
uint16_t fifoCount;
bool dmpReady = false;
uint8_t fifoBuffer[64];
String out;

// controll characters pre & suffix
const char EVENT_PRE = '#';
const char EVENT_SUF = '$';

// data characters pre & suffix
const char DATA_PRE = '{';
const char DATA_SUF = '}';

char msgArr[4];

// MPU output variables
Quaternion q;
VectorFloat gravity;
float ypr[3];

// PCINT variables
static int PCintMode[8];
typedef void (*voidFuncPtr)(void);
volatile static voidFuncPtr PCintFunc[8] = {
  NULL
};
volatile static uint8_t PCintLast;

// input communication variables
char inBuff[32];
uint8_t inBuffPtr = 0;
volatile bool mpuInterrupt = false;

volatile bool ledON = false;
volatile bool sendData = false;
volatile bool resetMPU = false;

// get physical Pin on the CPU
uint8_t getRealPin(uint8_t pin) {

  uint8_t ret;

  switch (pin) {
    case 9:
      ret = 5;
      break;
    case 10:
      ret = 6;
      break;
    case 11:
      ret = 7;
      break;
    case 14:
      ret = 3;
      break;
    case 15:
      ret = 1;
      break;
    case 16:
      ret = 2;
      break;
    default:
      ret = 0;
      break;
  }

  return ret;
}

char* getMessage(char c){
  msgArr[1] = c;
  return msgArr;
}

// attach an interrupt to a specific pin using pin change interrupts.
void PCattachInterrupt(uint8_t pin, void (*userFunc)(void), int mode) {

  uint8_t rPin = getRealPin(pin);

  // Wrong pin chosen, can not attach interrupt
  if (rPin == 0) {
    COM.print(getMessage('a'));
    return;
  }

  PCintMode[rPin] = mode;
  PCintFunc[rPin] = userFunc;
  // set the mask
  PCMSK0 |= 0x01 << rPin;
  // enable the interrupt
  PCICR |= 0x01;
}

// detach an interrupt from a pin
void PCdetachInterrupt(uint8_t pin) {

  uint8_t rPin = getRealPin(pin);

  // Wrong pin chosen, can not detach interrupt
  if (rPin == 0) {
    COM.print(getMessage('b'));
    return;
  }

  uint8_t mask = 0x01;

  for (int i = 0; i < rPin - 1; ++i) {
    mask = mask << 1;
    mask++;
  }

  // reset the mask
  PCMSK0 &= mask;
  // disable the interrupt --> just one interrupt can work at a time
  PCICR &= 0x00;
}

// interrupt handling
SIGNAL(PCINT0_vect) {
  uint8_t rPin;
  uint8_t curr;
  uint8_t mask;
  uint8_t pin;

  // get the pin states
  curr = *portInputRegister(2);
  mask = curr ^ PCintLast;
  PCintLast = curr;

  // mask is pins that have changed. screen out non pcint pins.
  if ((mask &= PCMSK0) == 0) {
    return;
  }

  // mask is pcint pins that have changed.
  for (uint8_t i = 0; i < 8; i++) {
    rPin = 0x01 << i;
    if (rPin & mask) {
      pin = i;
      // Trigger interrupt if mode is CHANGE, or if mode is RISING and
      // the bit is currently high, or if mode is FALLING and bit is low.
      if ((PCintMode[pin] == CHANGE
           || ((PCintMode[pin] == RISING) && (curr & rPin))
           || ((PCintMode[pin] == FALLING) && !(curr & rPin)))
          && (PCintFunc[pin] != NULL)) {
        PCintFunc[pin]();
      }
    }
  }
}

// interrupt routine
void dmpDataReady() {
  mpuInterrupt = true;
}

void initMPU6050() {

  // load and configure the DMP
  COM.print(getMessage('0'));
  devStatus = mpu.dmpInitialize();

  // supply your own gyro/accel offsets here, find them with the MPU_calibration sketch -- works better without
  /*mpu.setXAccelOffset(-1854);
  mpu.setYAccelOffset(-631);
  mpu.setZAccelOffset(1788);
  mpu.setXGyroOffset(220);
  mpu.setYGyroOffset(76);
  mpu.setZGyroOffset(-85);
  */

  // make sure it worked (returns 0 if so)
  if (devStatus == 0) {
    // turn on the DMP, now that it's ready
    COM.print(getMessage('2'));
    mpu.setDMPEnabled(true);

    // enable Arduino interrupt detection
    COM.print(getMessage('3'));
    PCattachInterrupt(PCINT_PIN, dmpDataReady, CHANGE);
    mpuIntStatus = mpu.getIntStatus();

    // set our DMP Ready flag so the main loop() function knows it's okay to use it
    dmpReady = true;

    // get expected DMP packet size for later comparison
    packetSize = mpu.dmpGetFIFOPacketSize();

    // reset FIFO so we can start clear
    mpu.resetFIFO();

  } else {
    // ERROR!
    // 1 = initial memory load failed
    // 2 = DMP configuration updates failed
    // (if it's going to break, usually the code will be 1)
    if (devStatus == 1)
      COM.print(getMessage('c'));
    else if (devStatus == 2)
      COM.print(getMessage('d'));
    else
      COM.print(getMessage('e'));
  }
}

void initMsgArray(){
  msgArr[0] = EVENT_PRE;
  msgArr[2] = EVENT_SUF;
  msgArr[3] = '\0';
}

void setup()
{
  COM.begin(19200);
 
  initMsgArray();

#if I2CDEV_IMPLEMENTATION == I2CDEV_ARDUINO_WIRE
  Wire.begin();
  TWBR = 24; // 400kHz I2C clock (200kHz if CPU is 8MHz)
#elif I2CDEV_IMPLEMENTATION == I2CDEV_BUILTIN_FASTWIRE
  Fastwire::setup(400, true);
#endif

  // for PCINT
  pinMode(PCINT_PIN, INPUT);
  pinMode(13, OUTPUT);

  COM.print(getMessage('1'));
  mpu.initialize();

  // verify connection, send result
  COM.print(mpu.testConnection() ? getMessage('f') : getMessage('g'));

  initMPU6050();

  //give MPU6050 time to stop oscilating and send rdy signal
  delay(500);
  COM.print(getMessage('j'));
}

void loop() {
  if (ledON)
    digitalWrite(13, HIGH);   // turn the LED on (HIGH is the voltage level)
  else
    digitalWrite(13, LOW);   // turn the LED off (LOW is the voltage level)

  if (resetMPU) {
    resetMPU6050();
    resetMPU = false;
    return;
  }

  // if DMP init failed stop here
  if (!dmpReady)
    return;

  while (!mpuInterrupt && fifoCount < packetSize)
    ;

  // reset interrupt flag and get INT_STATUS byte
  mpuInterrupt = false;
  mpuIntStatus = mpu.getIntStatus();

  // get current FIFO count
  fifoCount = mpu.getFIFOCount();

  // check for overflow (this should never happen unless our code is too inefficient)
  if ((mpuIntStatus & 0x10) || fifoCount == 1024) {
    // reset so we can continue cleanly
    mpu.resetFIFO();
    COM.print(getMessage('h'));
    // otherwise, check for DMP data ready interrupt (this should happen frequently)
  } else if (mpuIntStatus & 0x02) {
    // wait for correct available data length, should be a VERY short wait
    while (fifoCount < packetSize) fifoCount = mpu.getFIFOCount();

    // read a packet from FIFO
    mpu.getFIFOBytes(fifoBuffer, packetSize);

    // track FIFO count here in case there is > 1 packet available
    // (this lets us immediately read more without waiting for an interrupt)
    fifoCount -= packetSize;

    mpu.dmpGetQuaternion(&q, fifoBuffer);

    if (sendData) {
      // change Y and Z for unity coordinate system
      COM.print(DATA_PRE);
      printFloat(q.x);
      printFloat(q.z);
      printFloat(q.y);
      printFloat(-q.w);
      COM.print(DATA_SUF);
    }
  }
}

void printFloat(float f) {
  byte * b = (byte *) &f;
  COM.write(b[0]);
  COM.write(b[1]);
  COM.write(b[2]);
  COM.write(b[3]);
}

void resetMPU6050() {
  mpu.resetDMP();
  dmpReady = false;
  delay(500);
  initMPU6050();
}

// serial input functions
// for USB
void serialEvent() {
  serialEventHandler();
}

// for Bluetooth
void serialEvent1() {
  serialEventHandler();
}

// same handler for both inputs
void serialEventHandler() {

  if (inBuffPtr + COM.available() >= 32) {
    COM.print(getMessage('i'));
    return;
  }

  byte tmp;

  // get the data
  while (COM.available()) {
    tmp = COM.read();
    // kill nonReadable ascii characters
    if (tmp > 0x20 && tmp < 0x7F) {
      inBuff[inBuffPtr++] = (char)tmp;
    }
  }

  // max 11 commands in 32byte wide buffer
  char input[11];
  int inputPtr = 0;

  // search for valid input
  for (int i = 0; i < inBuffPtr - 2; ++i) {
    //COM.print(inBuff[i]);
    if (inBuff[i] == EVENT_PRE && inBuff[i + 2] == EVENT_SUF)
      input[inputPtr++] = inBuff[i + 1];
  }

  // safe incomplete commands for next interrupt routines
  char safe;
  bool one = false;
  bool two = false;

  if (inBuff[inBuffPtr - 1] == EVENT_PRE) {
    one = true;
  }
  else if (inBuff[inBuffPtr - 2] == EVENT_PRE) {
    safe = inBuff[inBuffPtr - 1];
    two = true;
  }

  //clear buffer
  memset(inBuff, 0, sizeof(inBuff));

  if (one) {
    inBuff[0] = EVENT_PRE;
    inBuffPtr = 1;
  }
  else if (two) {
    inBuff[0] = EVENT_PRE;
    inBuff[1] = safe;
    inBuffPtr = 2;
  }
  else
    inBuffPtr = 0;

  // set the booleans
  for (int i = 0; i < inputPtr ; i++) {
    switch (input[i]) {
      case 'b':
        delay(3000);
        COM.print(getMessage('k'));
        sendData = true;
        ledON = !ledON;
        break;
      case 'r':
        resetMPU = true;
        break;
      case 's':
        sendData = false;
        COM.print(getMessage('l'));
        break;
      case 'c':
        sendData = true;
        COM.print(getMessage('k'));
        break;
      default:
        break;
    }
  }
}

