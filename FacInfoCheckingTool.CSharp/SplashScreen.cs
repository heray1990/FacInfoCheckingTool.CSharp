﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using System.IO;

namespace FacInfoCheckingTool.CSharp
{
    public partial class SplashScreen : Form
    {

        public SplashScreen()
        {
            InitializeComponent();
        }

        private string brandName, modelName, swVersion;
        private uint barcodeLength, macAddrLength, comBaudRate, comId;

        public string BrandName { get { return brandName; } }
        public string ModelName { get { return modelName; } }
        public string SwVersion { get { return swVersion; } }
        public uint BarcodeLength { get { return barcodeLength; } }
        public uint MacAddrLength { get { return macAddrLength; } }
        public uint ComBaudRate { get { return comBaudRate; } }
        public uint ComId { get { return comId; } }

        private void SplashScreen_Load(object sender, EventArgs e)
        {
            try
            {
                /* Load Brand and Model from config.xml. And initialize the 
                 * items of comboBoxBrand and comboBoxModel. */
                string xmlFileName = Path.GetDirectoryName(Application.ExecutablePath) + @"\config.xml";
                XDocument config = XDocument.Load(xmlFileName);

                string queryStringResult = config.Descendants("currentproduct").Descendants("brand").First().Value;
                comboBoxBrand.Text = queryStringResult;

                queryStringResult = config.Descendants("currentproduct").Descendants("model").First().Value;
                comboBoxModel.Text = queryStringResult;

                comBaudRate = uint.Parse(config.Descendants("serialport").Attributes("baud").First().Value);
                comId = uint.Parse(config.Descendants("serialport").Attributes("id").First().Value);

                IEnumerable<string> queryBrands = from item in config.Descendants("products").Descendants("product").Attributes()
                                                  select item.Value;

                foreach (string itemBrand in queryBrands)
                {
                    comboBoxBrand.Items.Add(itemBrand);
                }

                IEnumerable<string> queryModels = from item in config.Descendants("product").Descendants("model")
                                                  where (string)item.Parent.Attribute("brand").Value == comboBoxBrand.Text
                                                  select item.Attribute("name").Value;
                foreach (string itemModel in queryModels)
                {
                    comboBoxModel.Items.Add(itemModel);
                }
            }
            catch (System.IO.FileNotFoundException ex)
            {
                string caption = "config.xml 文件不存在";
                var result = MessageBox.Show(ex.Message, caption,
                    MessageBoxButtons.OK, MessageBoxIcon.Stop);

                OutputLog.SaveLogInFile(caption + ", 退出程序！");

                if (result == DialogResult.OK)
                {
                    System.Environment.Exit(0);
                }
            }

            labelVersion.Text = OutputLog.Version();
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            brandName = comboBoxBrand.Text;
            modelName = comboBoxModel.Text;

            string xmlFileName = Path.GetDirectoryName(Application.ExecutablePath) + @"\config.xml";
            XDocument config = XDocument.Load(xmlFileName);

            barcodeLength = uint.Parse((from c in config.Descendants("barcodelength")
                                        where c.Parent.Attribute("brand").Value == brandName
                                        select c.Value).First());
            macAddrLength = uint.Parse((from c in config.Descendants("macaddrlength")
                                        where c.Parent.Attribute("brand").Value == brandName
                                        select c.Value).First());
            swVersion = (from c in config.Descendants("swversion")
                         where (c.Parent.Parent.Attribute("brand").Value == brandName)
                         && (c.Parent.Attribute("name").Value == modelName)
                         select c.Value).First();

            config.Descendants("currentproduct").First().SetElementValue("brand", brandName);
            config.Descendants("currentproduct").First().SetElementValue("model", modelName);
            config.Save(xmlFileName);

            this.DialogResult = DialogResult.OK;
            this.Hide();
        }

        private void comboBoxBrand_SelectedIndexChanged(object sender, EventArgs e)
        {
            string xmlFileName = Path.GetDirectoryName(Application.ExecutablePath) + @"\config.xml";
            XDocument config = XDocument.Load(xmlFileName);

            IEnumerable<string> queryModels = from item in config.Descendants("product").Descendants("model")
                                              where (string)item.Parent.Attribute("brand").Value == comboBoxBrand.Text
                                              select item.Attribute("name").Value;
            comboBoxModel.Text = queryModels.First();
            comboBoxModel.Items.Clear();
            foreach (string itemModel in queryModels)
            {
                comboBoxModel.Items.Add(itemModel);
            }
        }
    }
}
