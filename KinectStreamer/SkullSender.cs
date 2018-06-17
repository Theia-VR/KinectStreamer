using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Runtime.Serialization.Json;
using TheiaData;

namespace KinectStreamer
{
    class SkullSender
    {

        private UdpClient udpClient;

        public SkullSender(IPAddress ipAdress, int port)
        {
            udpClient = new UdpClient();
            try
            {
                udpClient.Connect(ipAdress, port);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        /// <summary>
        /// This method is used when the stream has to send a skeleton over network.
        /// </summary>
        public void SendSkeleton(Skeleton skeleton)
        {
            byte[] bytesToSend = new byte[Skeleton.ByteSize];

            byte[] timeStamp = BitConverter.GetBytes(skeleton.timestamp);
            Array.Copy(timeStamp, 0, bytesToSend, 0, timeStamp.Length);
            bytesToSend[timeStamp.Length] = skeleton.tag;

            int currentIndex = timeStamp.Length + 1;
            for(int i = 0; i < skeleton.joints.Length; i++)
            {
                byte[] xInBytes = BitConverter.GetBytes(skeleton.joints[i].x);
                byte[] yInBytes = BitConverter.GetBytes(skeleton.joints[i].y);
                byte[] zInBytes = BitConverter.GetBytes(skeleton.joints[i].z);

                Array.Copy(xInBytes, 0, bytesToSend, currentIndex, xInBytes.Length);
                currentIndex += xInBytes.Length;
                Array.Copy(yInBytes, 0, bytesToSend, currentIndex, yInBytes.Length);
                currentIndex += yInBytes.Length;
                Array.Copy(zInBytes, 0, bytesToSend, currentIndex, zInBytes.Length);
                currentIndex += zInBytes.Length;

                bytesToSend[currentIndex] = skeleton.joints[i].r;
                bytesToSend[currentIndex + 1] = skeleton.joints[i].g;
                bytesToSend[currentIndex + 2] = skeleton.joints[i].b;
                bytesToSend[currentIndex + 3] = skeleton.joints[i].tag;
                currentIndex += 4;
            }

            udpClient.Send(bytesToSend, bytesToSend.Length);
        }

        public void SendSkeletonJson(Skeleton skull)
        {
            //Console.WriteLine("Sending skeleton (tag : " + skull.tag + ")");

            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(Skeleton));
            MemoryStream stream = new MemoryStream();

            serializer.WriteObject(stream, skull);
            stream.Position = 0;
            StreamReader streamReader = new StreamReader(stream);
            string dataJson = streamReader.ReadToEnd();

            byte[] sendBytes = Encoding.ASCII.GetBytes(dataJson);
            try
            {
                udpClient.Send(sendBytes, sendBytes.Length);
                //Console.WriteLine(dataJson);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            streamReader.Close();
            stream.Close();
        }

        public void CloseSender()
        {
            try
            {
                udpClient.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
