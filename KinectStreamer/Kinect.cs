using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;

namespace KinectStreamer
{
    class Kinect
    {

        private KinectSensor kinectSensor;
        private MultiSourceFrameReader multiSourceFrameReader;

        /// <summary>
        /// This method starts a kinect sensor and opens its streams.
        /// </summary>
        public Boolean OpenKinectSensor(FrameProcessor frameProcessor)
        {
            kinectSensor = KinectSensor.GetDefault();
            if(kinectSensor != null)
            {
                kinectSensor.Open();
                if(true) // TODO : kinectSensor.IsAvailable returns a false value when we use a video in KinectStudio ??
                {
                    multiSourceFrameReader = kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Depth | FrameSourceTypes.Color | FrameSourceTypes.Body | FrameSourceTypes.BodyIndex);
                    multiSourceFrameReader.MultiSourceFrameArrived += frameProcessor.FrameArrivedEventHandler;
                    Console.WriteLine("Association with Kinect Sensor is working.");
                    return true;
                }
            }
            return false;
        }

        public void CloseKinectSensor()
        {
            this.kinectSensor.Close();
            this.multiSourceFrameReader.Dispose();
        }

    }
}
