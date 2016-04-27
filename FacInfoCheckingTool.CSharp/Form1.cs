﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO.Ports;

namespace FacInfoCheckingTool.CSharp
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            comPort.DataReceived += new SerialDataReceivedEventHandler(comPort_DataReceived);
        }

        private SerialPort comPort = new SerialPort();
        private ICommands command;
        private bool isAllPass = true;
        private bool isCmdDataRecv = false;        

        private void Form1_Load(object sender, EventArgs e)
        {
            SplashScreen splashScreen = new SplashScreen();
            splashScreen.TopMost = true;
            this.Hide();
            this.ShowInTaskbar = false;

            if (splashScreen.ShowDialog() == DialogResult.OK)
            {
                if (ConfigXmlHandler.currentBrand == "CAN")
                {
                    command = new CANTVCommands();
                    pictureBoxLogo.Image = global::FacInfoCheckingTool.CSharp.Properties.Resources.CANTV;
                }
                else if(ConfigXmlHandler.currentBrand == "Haier")
                {
                    command = new CANTVCommands();
                    pictureBoxLogo.Image = global::FacInfoCheckingTool.CSharp.Properties.Resources.Haier;
                }
                else
                {
                    command = new CANTVCommands();
                    pictureBoxLogo.Image = global::FacInfoCheckingTool.CSharp.Properties.Resources.CANTV;
                }

                labelModelName.Text = ConfigXmlHandler.currentModel;
                
                this.Show();
            }
        }

        private void watermarkTextBoxBarcode_KeyPress(object sender, KeyPressEventArgs e)
        {            
            if (e.KeyChar == 13)
            {
                if (watermarkTextBoxBarcode.Text.Length == 1)
                {
                    watermarkTextBoxBarcode.ReadOnly = true;
                    watermarkTextBoxMacAddr.Focus();
                }
                else
                {
                    string caption = "整机条码长度不正确";
                    string message = "请输入正确的整机条码。";
                    var result = MessageBox.Show(message, caption,
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    OutputLog.SaveLogInFile(caption + "！");

                    if (result == DialogResult.OK)
                    {
                        watermarkTextBoxBarcode.Text = "";
                        watermarkTextBoxBarcode.Focus();
                    }
                }
            }
            else if ((e.KeyChar >= 48 && e.KeyChar <= 57)  // 0-9
                || (e.KeyChar >= 65 && e.KeyChar <= 90)    // A-Z
                || (e.KeyChar >= 97 && e.KeyChar <= 122)   // a-z
                || (e.KeyChar == 46) || (e.KeyChar == 8))  // Del and Backspace
            {
                e.Handled = false;
            }
            else
            {
                e.Handled = true;
            }
        }

        private void watermarkTextBoxMacAddr_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                if (watermarkTextBoxMacAddr.Text.Length == 12)
                {
                    watermarkTextBoxMacAddr.ReadOnly = true;
                    textBoxLog.Focus();

                    try
                    {
                        if (comPort.IsOpen) comPort.Close();
                        else
                        {
                            // Serial Port setting
                            comPort.BaudRate = int.Parse(ConfigXmlHandler.comBaudRate);
                            comPort.PortName = ConfigXmlHandler.comId;
                            comPort.DataBits = 8;
                            comPort.ReadTimeout = 100;
                            comPort.Open();

                            MainTask();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                        watermarkTextBoxBarcode.ReadOnly = false;
                        watermarkTextBoxBarcode.Text = "";
                        watermarkTextBoxBarcode.Focus();
                        watermarkTextBoxMacAddr.ReadOnly = false;
                        watermarkTextBoxMacAddr.Text = "";
                    }
                }
                else
                {
                    string caption = "MAC 地址条码长度不正确";
                    string message = "请输入正确的 MAC 地址条码。";
                    var result = MessageBox.Show(message, caption,
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    OutputLog.SaveLogInFile(caption + "！");

                    if (result == DialogResult.OK)
                    {
                        watermarkTextBoxMacAddr.Text = "";
                        watermarkTextBoxMacAddr.Focus();
                    }
                }
            }
            else if ((e.KeyChar >= 48 && e.KeyChar <= 57)  // 0-9
                || (e.KeyChar >= 65 && e.KeyChar <= 90)    // A-Z
                || (e.KeyChar >= 97 && e.KeyChar <= 122)   // a-z
                || (e.KeyChar == 46) || (e.KeyChar == 8))  // Del and Backspace
            {
                e.Handled = false;
            }
            else
            {
                e.Handled = true;
            }
        }

        private void TVSerialPortSettingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormSerialPortSetting frmSerialPortSetting = new FormSerialPortSetting();
            frmSerialPortSetting.Show();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (comPort.IsOpen)
                {
                    comPort.ReadExisting();
                    comPort.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void comPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string data = "";
                int n = comPort.BytesToRead;
                Byte[] buf = new Byte[n];
                comPort.Read(buf, 0, n);

                isCmdDataRecv = true;

                if (buf[0] == 0x55 && ((buf[1] == 0xB0) || (buf[1] == 0xB2) || (buf[1] == 0xB4)))
                {
                    command.CommandIdx++;
                    foreach (Byte dataByte in buf)
                    {
                        data = data + dataByte.ToString("X2").ToUpper() + " ";
                    }

                    if (command.CommandIdx == 1)
                    {
                        this.Invoke((EventHandler)(delegate
                        {
                            OutputLog.ShowLog(textBoxLog, "[TV]MAC Address: " + data);
                            data = "";
                            for (int i = 0; i <= n; i++)
                            {
                                if ((i >= 3) && (i <= 8))
                                {
                                    data = data + buf[i].ToString("X2").ToUpper();
                                }
                            }
                            labelMacAddr.Text = data;
                            if (data == watermarkTextBoxMacAddr.Text.ToUpper())
                            {
                                labelMacAddr.BackColor = Color.Green;
                                isAllPass = true && isAllPass;
                            }
                            else
                            {
                                labelMacAddr.BackColor = Color.Red;
                                isAllPass = false;
                            }
                        }));
                    }
                    else if (command.CommandIdx == 2)
                    {
                        this.Invoke((EventHandler)(delegate
                        {
                            OutputLog.ShowLog(textBoxLog, "[TV]Software Version: " + data);
                            data = "";
                            Byte[] bufTmp = new Byte[n];
                            for (int i = 0; i <= n; i++)
                            {
                                if ((i >= 3) && (i <= 8))
                                {                                    
                                    bufTmp[i - 3] = buf[i];
                                }
                            }
                            data = data + Encoding.ASCII.GetString(bufTmp);
                            labelSwVer.Text = data;
                            labelSwVer.BackColor = Color.Green;
                            isAllPass = true && isAllPass;

                            if (isAllPass)
                            {
                                labelResult.Text = "PASS";
                                labelResult.BackColor = Color.Green;
                            }
                            else
                            {
                                labelResult.Text = "NG";
                                labelResult.BackColor = Color.Red;
                            }
                        }));
                    }
                }
                else
                {
                    OutputLog.ShowLog(textBoxLog, "Unknown data!");
                }                                
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void MainTask()
        {            
            isAllPass = true;
            isCmdDataRecv = false;
            labelMacAddr.BackColor = Color.White;
            labelSwVer.BackColor = Color.White;
            labelResult.BackColor = Color.White;
            labelResult.Text = "Checking";
            
            CmdSendingEngine(command.ReadMacAddr());
            isCmdDataRecv = false;
            CmdSendingEngine(command.ReadSwVer());
            isCmdDataRecv = false;            
        }

        private void CmdSendingEngine(byte[] CmdByte)
        {
            int i = 0;

            while (!(isCmdDataRecv) && (i <= 3))
            {
                i++;
                comPort.Write(CmdByte, 0, command.CmdLength);
                DelayMillisecondForCmd(500);
            } 
        }

        private void DelayMillisecondForCmd(int millisecond)
        {
            int start = Environment.TickCount;

            while (Math.Abs(Environment.TickCount - start) < millisecond)
            {
                if (isCmdDataRecv) { break; }
                Application.DoEvents();
            }
        }
    }
}
