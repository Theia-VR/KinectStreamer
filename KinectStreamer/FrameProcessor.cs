using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Text;
using TheiaData;

namespace KinectStreamer
{
    class FrameProcessor
    {
        public event EventHandler kinectEvent;
        public delegate void EventHandler(FrameProcessor fp, KinectEventArgs kea);

        private Boolean shouldSendCloud;
        private Boolean shouldSendSkeleton;

        private PointsSender pointsSender;
        private SkullSender skullSender;

        private Boolean initializationDone;
        private CoordinateMapper coordinateMapper;

        private int colorFrameWidth;
        private int colorFrameHeight;
        private int bodyIndexFrameWidth;
        private int bodyIndexFrameHeight;
        private int depthFrameWidth;
        private int depthFrameHeight;

        private int nbFrame = 0;

        public FrameProcessor(Boolean shouldSendCloud, Boolean shouldSendSkeleton, PointsSender pointsSender, SkullSender skullSender)
        {
            this.shouldSendCloud = shouldSendCloud;
            this.shouldSendSkeleton = shouldSendSkeleton;

            this.pointsSender = pointsSender;
            this.skullSender = skullSender;

            initializationDone = false;
            coordinateMapper = KinectSensor.GetDefault().CoordinateMapper;
        }

        /* Is called whenever a frame arrived from the Kinect */
        public void FrameArrivedEventHandler(object sender, MultiSourceFrameArrivedEventArgs frameArrivedEvent)
        {
            nbFrame++;
            Console.WriteLine("Frame received ! (" + nbFrame + ")");

            MultiSourceFrame frameReceived = frameArrivedEvent.FrameReference.AcquireFrame();
            if (frameReceived == null) { return; }

            if(shouldSendCloud)
            {
                using (var colorFrame = frameReceived.ColorFrameReference.AcquireFrame())
                using (var depthFrame = frameReceived.DepthFrameReference.AcquireFrame())
                using (var bodyIndexFrame = frameReceived.BodyIndexFrameReference.AcquireFrame())
                {
                    if (depthFrame == null)
                    {
                        Console.WriteLine("DEPTH FRAME NULL");
                        return;
                    }

                    if (!initializationDone)
                    {
                        initialization(colorFrame, depthFrame, bodyIndexFrame);
                    }

                    ushort[] depthData = new ushort[depthFrameWidth * depthFrameHeight];
                    byte[] bodyData = new byte[depthFrameWidth * depthFrameHeight];
                    byte[] colorData = new byte[colorFrameWidth * colorFrameHeight * 4];
                    
                    depthFrame.CopyFrameDataToArray(depthData);
                    colorFrame.CopyConvertedFrameDataToArray(colorData, ColorImageFormat.Rgba);
                    bodyIndexFrame.CopyFrameDataToArray(bodyData);

                    /*int countDepth = 0;
                    for(int i = 0; i < depthData.Length; i++)
                    {
                        if(depthData[i] != 0)
                        {
                            countDepth++;
                        }
                    }

                    

                    int countColor = 0;
                    for (int i = 0; i < colorData.Length; i++)
                    {
                        if (colorData[i] != 0)
                        {
                            countColor++;
                        }
                    }
                    Console.WriteLine("depthData : " + countDepth + "/" + depthData.Length);
                    Console.WriteLine("depthData : " + countColor + "/" + colorData.Length);*/

                    PointCloud pointCloud = new PointCloud();
                    pointCloud.timestamp = colorFrame.RelativeTime.Ticks;
                    pointCloud.vertices = GetVertices(depthData, bodyData, colorData).ToArray();

                    /*int counter = 0;
                    StringBuilder builder = new StringBuilder();
                    foreach(Vertex vertex in pointCloud.vertices)
                    {
                        builder.AppendLine("Vertex " + counter + " : " + vertex.x + " " + vertex.y + " " + vertex.z + " " + vertex.r + " " + vertex.g + " " + vertex.b + " " + vertex.tag);
                        counter++;
                    }
                    Console.WriteLine(builder.ToString());*/

                    pointsSender.SendPointCloud(pointCloud);
                }
            }

            if(shouldSendSkeleton)
            {
                using (var bodyFrame = frameReceived.BodyFrameReference.AcquireFrame())
                {

                    Body[] bodies = new Body[bodyFrame.BodyCount];
                    bodyFrame.GetAndRefreshBodyData(bodies);

                    for(int i = 0; i < bodies.Length; i++)
                    {
                        if (bodies[i].IsTracked)
                        {
                            Skeleton skeleton = new Skeleton();
                            skeleton.tag = (byte) i;
                            foreach (KeyValuePair<JointType, Joint> joint in bodies[i].Joints)
                            {
                                Vertex point = new Vertex();
                                point.x = joint.Value.Position.X;
                                point.y = joint.Value.Position.Y;
                                point.z = joint.Value.Position.Z;

                                skeleton.joints[(int)joint.Key] = point;
                            }
                            skeleton.timestamp = bodyFrame.RelativeTime.Ticks;
                            skullSender.SendSkeleton(skeleton);
                        }
                    }

                }
            }
        }

        /// <summary>
        /// This method is called to initialize values for the frame processor (executed only once, when the first frame arrives).
        /// </summary>
        private void initialization(ColorFrame colorFrame, DepthFrame depthFrame, BodyIndexFrame bodyIndexFrame)
        {
            colorFrameWidth = colorFrame.FrameDescription.Width;
            colorFrameHeight = colorFrame.FrameDescription.Height;

            depthFrameWidth = depthFrame.FrameDescription.Width;
            depthFrameHeight = depthFrame.FrameDescription.Height;

            bodyIndexFrameWidth = bodyIndexFrame.FrameDescription.Width;
            bodyIndexFrameHeight = bodyIndexFrame.FrameDescription.Height;

            initializationDone = true;
            Console.WriteLine("Frame Processor initialization is done.");
        }

        /* Return vertices from frames data. */
        private List<Vertex> GetVertices(ushort[] depthData, byte[] bodyData, byte[] colorData)
        {
            List<Vertex> vertices = new List<Vertex>();

            ColorSpacePoint[] colorPoints = new ColorSpacePoint[depthData.Length];
            CameraSpacePoint[] cameraSpace = new CameraSpacePoint[depthData.Length];


            coordinateMapper.MapDepthFrameToCameraSpace(depthData, cameraSpace);
            coordinateMapper.MapDepthFrameToColorSpace(depthData, colorPoints);

            for (int y = 0; y < depthFrameHeight; ++y)
            {
                for (int x = 0; x < depthFrameWidth; ++x)
                {
                    int depthIndex = (y * depthFrameWidth) + x;

                    byte person = bodyData[depthIndex];
                    if (person != 0xff)
                    {
                        Vertex vertex = new Vertex();
                        vertex.tag = person;

                        vertex.x = cameraSpace[depthIndex].X;
                        vertex.y = cameraSpace[depthIndex].Y;
                        vertex.z = cameraSpace[depthIndex].Z;

                        ColorSpacePoint colorPoint = colorPoints[depthIndex];
                        
                        int colorX = (int)Math.Floor(colorPoint.X + 0.5);
                        int colorY = (int)Math.Floor(colorPoint.Y + 0.5);

                        if ((colorX >= 0) && (colorX < colorFrameWidth) && (colorY >= 0) && (colorY < colorFrameHeight))
                        {
                            int colorIndex = ((colorY * colorFrameWidth) + colorX) * 4;
                            
                            vertex.r = colorData[colorIndex];
                            vertex.g = colorData[colorIndex + 1];
                            vertex.b = colorData[colorIndex + 2];
                        }else
                        {
                            /*Console.WriteLine("ERROR ON COLOR POINT");
                            Console.WriteLine("Depth index : " + depthIndex);
                            Console.WriteLine("Vertex : " + vertex.x + " " + vertex.y + " " + vertex.z);
                            Console.WriteLine("ColorPoint : " + colorPoint.X + " " + colorPoint.Y);
                            Console.WriteLine("Color : " + colorX + " " + colorY);*/
                            vertex.r = 10;
                            vertex.g = 10;
                            vertex.b = 10;
                        }
                        vertices.Add(vertex);
                    }
                }
            }
            return vertices;
        }


        public void SendMessage(string message)
        {
            KinectEventArgs dsea = new KinectEventArgs(message);
            kinectEvent?.Invoke(this, dsea);
        }

        public class KinectEventArgs : EventArgs
        {
            private string message;

            public KinectEventArgs(String message) { this.message = message; }

            public override string ToString() { return message; }
        }
    }
}
