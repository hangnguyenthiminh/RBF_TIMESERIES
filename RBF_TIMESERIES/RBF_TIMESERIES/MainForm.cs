using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.IO;
using System.Threading;

namespace RBF_TIMESERIES
{
    public partial class MainForm : Form
    {
        private double[] data = null;
        private double[][] allData = null;
        private int windowSize = 5;
        private double testingRate = 30;

        //Train
        private double[] train = null;
        private double[][] inputTrain = null;
        private double[][] idealTrain = null;

        //Test
        private double[] test = null;
        private double[][] inputTest = null;
        private double[][] idealTest = null;


        public MainForm()
        {
            //
            InitializeComponent();
        }

        // Update data in list view
        private void UpdateDataListView()
        {
            // remove all current records
            dataList.Items.Clear();
            // add new records
            for (int i = 0, n = data.GetLength(0); i < n; i++)
            {
                //add STT column
                dataList.Items.Add((i + 1).ToString());
                //add Real Data column (thêm lần lượt value vào các cột sau...thêm trên một hàng)
                //ListView1.Items[index của tên cột đầu tiên].SubItems.Add("thêm từng cột")
                dataList.Items[i].SubItems.Add(data[i].ToString());
            }
        }

        private double[] GetWindowData(int index, int windowSize, double[] data)
        {
            double[] subData = new double[windowSize];
            for (int i = 0; i < windowSize; i++)
            {
                subData[i] = data[index + i];
            }
            return subData;
        }


        /// <summary>
        /// Get data to input[][] and ideal[][]
        /// </summary>
        /// <param name="data">data train or test</param>
        /// <param name="input"></param>
        /// <param name="ideal"></param>
        /// <param name="windowSize"></param>
        private void GetInputIdeal(double[] data, out double[][] input, out double[][] ideal, int windowSize)
        {
            int sizeData = data.Length;
            //Get input, ideal cho train
            input = new double[sizeData - windowSize][];
            ideal = new double[sizeData - windowSize][];

            for (int i = 0; i < sizeData - windowSize; i++)
            {
                input[i] = GetWindowData(i, windowSize, data);
                ideal[i] = new double[] { data[i + windowSize] };
            }
        }

        /// <summary>
        /// Get data to train[] and test[]
        /// </summary>
        /// <param name="data">all data get from File</param>
        /// <param name="train"></param>
        /// <param name="test"></param>
        /// <param name="testingRate"></param>
        private void GetTrainTest(double[] data, out double[] train, out double[] test, double testingRate)
        {
            //khoi tao
            int sizeData = data.Length;
            int numTest = (int)((testingRate / 100) * sizeData);
            int numTrain = sizeData - numTest;
            train = new double[numTrain];
            test = new double[numTest];
            int j = 0;
            int k = 0;

            //Get data train and test from data[]
            for (int i = 0; i < sizeData; i++)
            {
                if (i < numTrain)
                {
                    train[j] = data[i];
                    j++;
                }
                else
                {
                    test[k] = data[i];
                    k++;
                }
            }
        }

        private void btnLoadData_Click(object sender, EventArgs e)
        {
            // show file selection dialog
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                StreamReader reader = null;
                // read maximum 1000 points
                double[] tempData = new double[1000];

                try
                {
                    // open selected file
                    reader = File.OpenText(openFileDialog.FileName);
                    int i = 0;

                    // read the data

                    string[] lines = System.IO.File.ReadAllLines(openFileDialog.FileName);

                    // Display the file contents by using a foreach loop.
                    System.Console.WriteLine("Contents of WriteLines2.txt = ");
                    foreach (string line in lines)
                    {
                        // Use a tab to indent each line of the file.
                        int pos = line.IndexOf("\t");
                        string value = line.Substring(pos + 1, line.Length - 1 - pos);
                        //parse the value
                        tempData[i] = double.Parse(value);
                        i++;

                    }

                    // allocate and set data
                    data = new double[i];
                    Array.Copy(tempData, 0, data, 0, i);

                }
                catch (Exception)
                {
                    MessageBox.Show("Failed reading the file", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                finally
                {
                    // close file
                    if (reader != null)
                        reader.Close();
                }

                // update list and chart
                UpdateDataListView();
                // enable "Start" button
                //startButton.Enabled = true;
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            GetTrainTest(data, out train, out test, testingRate);
            GetInputIdeal(train, out inputTrain, out idealTrain, windowSize);
            GetInputIdeal(train, out inputTest, out idealTest, windowSize);
        }
    }
}
