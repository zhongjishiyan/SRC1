using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Doli.DoSANet;


namespace PipesClientTest
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            comboBox1.Items.Clear();
            comboBox1.Items.Add("仿真方式");
            comboBox1.Items.Add("新Doli控制器");
            comboBox1.Items.Add("旧Doli控制器");
            comboBox1.SelectedIndex = 0;
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int l;
            int.TryParse(textBox1.Text, out l);
            GlobeVal.myconfigfile.machinecount = l;
            GlobeVal.myconfigfile.mode = comboBox1.SelectedIndex;
            modMain.initValue(GlobeVal.myconfigfile.machinecount);
            Close();
        }
    }
}
