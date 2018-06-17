using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace KinectStreamer
{
    /// <summary>
    /// Interaction logic for KinectController.xaml,  which is the app's main window.
    /// </summary>
    public partial class KinectController : Window
    {
        private Boolean isCurrentlyStreaming = false;
        private Stream stream = null;

        public KinectController()
        {
            InitializeComponent();
        }


        private void connectionButton_Click(object sender, RoutedEventArgs e)
        {
            Button clickedButton = (Button)sender;

            if (isCurrentlyStreaming == false)
            {
                /* TODO : Controlling parameters in front end */
                stream = new Stream(shouldSendCloudCheckBox.IsChecked.Value, shouldSendSkeletonCheckBox.IsChecked.Value, IPAddress.Parse(ipAddress.Text), Int32.Parse(cloudPort.Text), Int32.Parse(maxNumberOfPoints.Text), Int32.Parse(skeletonPort.Text), Int32.Parse(numberOfVerticesToSkip.Content.ToString()));
                
                /* Eventhandler configuration for the console */
                stream.streamEvent += (senderStream, eventArgs) =>
                {
                    Dispatcher.Invoke((Action)delegate ()
                    {
                        PrintMessageInConsole(eventArgs.ToString());
                    });
                };

                Thread streamingThread = new Thread(stream.DoWork);
                streamingThread.Start();
                isCurrentlyStreaming = true;
                clickedButton.Content = "Arrêter";
                DesactivateComponents();
            }
            else
            {
                if (stream != null) { stream.RequestStop(); }
                isCurrentlyStreaming = false;
                clickedButton.Content = "Envoyer";
                ActivateComponents();
            }
        }

        private void PrintMessageInConsole(string message)
        {
            console.AppendText(message + "\n");
            console.ScrollToEnd();
        }

        private void textBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void DesactivateComponents()
        {
            ipAddress.IsEnabled = false;
            maxNumberOfPoints.IsEnabled = false;
            cloudPort.IsEnabled = false;
            kinectID.IsEnabled = false;
            skeletonPort.IsEnabled = false;
            shouldSendCloudCheckBox.IsEnabled = false;
            shouldSendSkeletonCheckBox.IsEnabled = false;
            sliderDockPanel.IsEnabled = false;
            /* TODO : griser les autres options */
        }

        private void ActivateComponents()
        {
            ipAddress.IsEnabled = true;
            maxNumberOfPoints.IsEnabled = true;
            cloudPort.IsEnabled = true;
            kinectID.IsEnabled = true;
            skeletonPort.IsEnabled = true;
            shouldSendCloudCheckBox.IsEnabled = true;
            shouldSendSkeletonCheckBox.IsEnabled = true;
            sliderDockPanel.IsEnabled = true;
        }
    }
}
