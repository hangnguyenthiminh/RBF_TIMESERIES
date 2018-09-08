using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.IO;
using System.Threading;
using Encog.ML.Data.Basic;
using Encog.Neural.Rbf.Training;
using Encog.Neural.RBF;
using Encog.Neural.Pattern;
using Encog.MathUtil.RBF;

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

        // Delegates to enable async calls for setting controls properties
        private delegate void SetTextCallback(System.Windows.Forms.Control control, string text);
        private delegate void AddSubItemCallback(System.Windows.Forms.ListView control, int item, string subitemText);

        // Thread safe updating of control's text property
        private void SetText(System.Windows.Forms.Control control, string text)
        {
            if (control.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetText);
                Invoke(d, new object[] { control, text });
            }
            else
            {
                control.Text = text;
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
                btnStart.Enabled = true;
            }
        }

        private Boolean StringIsNull(string text)
        {
            if(text.Equals(null) || text.Equals(""))
            {
                return true;
            }
            return false;
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            //Get input from Form
            //Get testing rate
            if (!StringIsNull(txtTestingRate.Text))
            {
                testingRate = double.Parse(txtTestingRate.Text);
            }

            //Get window size
            if (!StringIsNull(txtWindowSize.Text))
            {
                windowSize = int.Parse(txtWindowSize.Text);
            }

            //Step1: Get inputTrain, idealTrain, inputTest, idealTest
            GetTrainTest(data, out train, out test, testingRate);
            GetInputIdeal(train, out inputTrain, out idealTrain, windowSize);
            GetInputIdeal(train, out inputTest, out idealTest, windowSize);
            Console.WriteLine("Get data success!!");



            //Step2: Caculate train Error
            //Specify the number of dimensions and the number of neurons per dimension
            const int dimensions = 2;
            const int numNeuronsPerDimension = 7;

            //Set the standard RBF neuron width. 
            //Literature seems to suggest this is a good default value.
            const double volumeNeuronWidth = 2.0 / numNeuronsPerDimension;

            //RBF can struggle when it comes to flats at the edge of the sample space.
            //We have added the ability to include wider neurons on the sample space boundary which greatly
            //improves fitting to flats
            const bool includeEdgeRBFs = true;

            #region Setup

            //General setup is the same as before
            var pattern = new RadialBasisPattern();
            pattern.InputNeurons = dimensions;
            pattern.OutputNeurons = 1;

            //Total number of neurons required.
            //Total number of Edges is calculated possibly for future use but not used any further here
            int numNeurons = (int)Math.Pow(numNeuronsPerDimension, dimensions);
            // int numEdges = (int) (dimensions*Math.Pow(2, dimensions - 1));

            pattern.AddHiddenLayer(numNeurons);

            var network = (RBFNetwork)pattern.Generate();
            //RadialBasisFunctionLayer rbfLayer = (RadialBasisFunctionLayer)network.GetLayer(RadialBasisPattern.RBF_LAYER);


            //Position the multidimensional RBF neurons, with equal spacing, within the provided sample space from 0 to 1.
            //rbfLayer.SetRBFCentersAndWidthsEqualSpacing(0, 1, RBFEnum.Gaussian, dimensions, volumeNeuronWidth, includeEdgeRBFs);
            network.SetRBFCentersAndWidthsEqualSpacing(0, 1, RBFEnum.Gaussian, volumeNeuronWidth, includeEdgeRBFs);

            #endregion

            //Create some training data that can not easily be represented by gaussians
            //There are other training examples for both 1D and 2D
            //Degenerate training data only provides outputs as 1 or 0 (averaging over all outputs for a given set of inputs would produce something approaching the smooth training data).
            //Smooth training data provides true values for the provided input dimensions.
            //Create2DSmoothTainingDataGit();

            //Create the training set and train.
            var trainingSet = new BasicMLDataSet(inputTrain, idealTrain);
            var trainNW = new SVDTraining(network, trainingSet);

            //SVD is a single step solve
            int epoch = 1;
            do
            {
                trainNW.Iteration();
                Console.WriteLine(@"Epoch #" + epoch + @" Error:" + trainNW.Error);
                epoch++;
            } while ((epoch < 1) && (trainNW.Error > 0.001));
            //Set Textbox
            SetText(txtTrainErr, trainNW.Error.ToString("F5"));


            //Step3: Caculate Test Error


        }

    }
}
