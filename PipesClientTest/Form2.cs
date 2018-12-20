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

            textBox1.Text = GlobeVal.myconfigfile.machinecount.ToString();
            comboBox1.Items.Clear();
            comboBox1.Items.Add("仿真方式");
            comboBox1.Items.Add("新Doli控制器");
            comboBox1.Items.Add("旧Doli控制器");
            comboBox1.SelectedIndex = 0;
            comboBox1_SelectionChangeCommitted(null, null);

            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int l;
            int.TryParse(textBox1.Text, out l);
            GlobeVal.myconfigfile.machinecount = l;
            GlobeVal.myconfigfile.mode = comboBox1.SelectedIndex;
            modMain.initValue(GlobeVal.myconfigfile.machinecount);

            for (int i=0;i<GlobeVal.myconfigfile.machinecount;i++)
            {
                GlobeVal.myconfigfile.SimulationMode[i] = (dataGridView1.Columns[1] as DataGridViewComboBoxColumn).Items.IndexOf(dataGridView1.Rows[i].Cells[1].Value);
            }

            GlobeVal.myconfigfile.SerializeNow(Application.StartupPath + @"\sys\系统设置.ini");

            Demo.Init();

            Demo.readdemo(Application.StartupPath + @"\demo\计算演示1.txt");

            Demo.makesin();

            Close();
        }

        private void comboBox1_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex ==0)
            {
                dataGridView1.Visible = true;
            }
            else
            {
                dataGridView1.Visible = false; 
            }

            dataGridView1.Rows.Clear();

            for (int i = 0; i < GlobeVal.myconfigfile.machinecount; i++)
            {
                dataGridView1.Rows.Add();
                dataGridView1.Rows[i].Cells[0].Value = (i + 1).ToString();
              
                dataGridView1.Rows[i].Cells[1].Value   = (dataGridView1.Columns[1] as DataGridViewComboBoxColumn).Items[GlobeVal.myconfigfile.SimulationMode[i]];
                  
                
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            GlobeVal.myconfigfile.machinecount = Convert.ToInt32(textBox1.Text);
            comboBox1_SelectionChangeCommitted(null, null);
        }
    }
}
