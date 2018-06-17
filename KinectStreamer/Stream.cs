using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace KinectStreamer
{
    /// <summary>
    /// Main worker thread (start senders and frame processor, then open kinect and wait).
    /// </summary>
    class Stream
    {

        private Kinect kinect;
        private FrameProcessor frameProcessor;
        private PointsSender pointsSender;
        private SkullSender skullSender;

        public event EventHandler streamEvent;
        public delegate void EventHandler(Stream stream, StreamEventArgs dsea);

        private volatile bool _shouldStop;

        public Stream(Boolean shouldSendCloud, Boolean shouldSendSkeleton, IPAddress ipAdress, int cloudPort, int maxNumberOfPoints, int skullPort, int numberOfVerticesToSkip)
        {
            kinect = new Kinect();
            /* TODO : avoid senders initialization if should not send */
            pointsSender = new PointsSender(ipAdress, cloudPort, maxNumberOfPoints, numberOfVerticesToSkip);
            skullSender = new SkullSender(ipAdress, skullPort);
            frameProcessor = new FrameProcessor(shouldSendCloud, shouldSendSkeleton, pointsSender, skullSender);
        }

        /// <summary>
        /// Worker thread main loop.
        /// </summary>
        public void DoWork()
        {
            if (kinect.OpenKinectSensor(frameProcessor) == false)
            {
                SendMessage("Impossible d'accéder à la Kinect.");
                return;
            }else
            {
                frameProcessor.kinectEvent += (k, eventArgs) => { SendMessage(eventArgs.ToString()); };
            }
            Console.WriteLine("The streamer is streaming incoming data now...");
            while (!_shouldStop)
            {
                /* worker thread will stop when RequestStop() method will be called */
            }
            this.kinect.CloseKinectSensor();
            Console.WriteLine("The worker thread has terminated and does not stream data anymore.");
        }

        /// <summary>
        /// This method is called to stop the worker thread.
        /// </summary>
        public void RequestStop()
        {
            _shouldStop = true;
        }

        /// <summary>
        /// This method is used by the streamer to display messages in the GUI console.
        /// </summary>
        public void SendMessage(string message)
        {
            StreamEventArgs dsea = new StreamEventArgs(message);
            streamEvent?.Invoke(this, dsea);
        }
    }

    /* Classe pour les events envoyés */
    public class StreamEventArgs : EventArgs
    {
        private string message;

        public StreamEventArgs(String message) { this.message = message; }

        public override string ToString() { return message; }
    }
}
