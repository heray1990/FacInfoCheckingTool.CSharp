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

        private void SplashScreen_Load(object sender, EventArgs e)
        {
            string xmlFileName = Path.GetDirectoryName(Application.ExecutablePath) + @"\config.xml";
            XDocument config = XDocument.Load(xmlFileName);

            string queryStringResult = config.Descendants("currentproduct").Descendants("brand").First().Value;
            comboBoxBrand.Text = queryStringResult;

            queryStringResult = config.Descendants("currentproduct").Descendants("model").First().Value;
            comboBoxModel.Text = queryStringResult;

            IEnumerable<string> queryBrands = from item in config.Descendants("products").Descendants("product").Attributes()
                              select item.Value;

            foreach (string itemBrand in queryBrands)
            {
                comboBoxBrand.Items.Add(itemBrand);
            }

            IEnumerable<string> queryModels = from item in config.Descendants("product").Descendants("model")
                              where (string)item.Parent.Parent.Attribute("brand").Value == comboBoxBrand.Text
                              select item.Value;
            foreach (string itemModel in queryModels)
            {
                comboBoxModel.Items.Add(itemModel);
            }

        }

        private void label1_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Dispose();
        }
    }
}
