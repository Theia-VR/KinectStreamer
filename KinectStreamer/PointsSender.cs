using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using TheiaData;

namespace KinectStreamer
{
    class PointsSender
    {

        private UdpClient udpClient;
        private int maxChunkSize;
        private int numberOfVerticesToSkip;

        public PointsSender(IPAddress ipAdress, int port, int maxChunkSize, int numberOfVerticesToSkip)
        {
            udpClient = new UdpClient();
            this.maxChunkSize = maxChunkSize;
            this.numberOfVerticesToSkip = numberOfVerticesToSkip;
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
        /// This method is used when the stream has to send a point cloud over network (usually one for each frame).
        /// </summary>
        public void SendPointCloud(PointCloud pointCloud)
        {
            if (pointCloud.vertices.Length == 0)
            {
                Console.WriteLine("No point to send.");
                return;
            }
            List<byte[]> sendBytesList = ConvertVerticesToByteArrays(pointCloud);
            //Console.WriteLine("PointsSender received points to Send (number of vertices : " + pointCloud.vertices.Length + ", number of packets : " + sendBytesList.Count + ", timestamp : " + pointCloud.timestamp + ")");
            try
            {
                foreach (byte[] sendBytes in sendBytesList)
                {
                    udpClient.Send(sendBytes, sendBytes.Length);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        /// <summary>
        /// This method is used to convert a point cloud to byte arrays (in order to send them over network), each array corresponding to a chunk of the complete cloud (need to be rebuild on the client-side).
        /// </summary>
        private List<byte[]> ConvertVerticesToByteArrays(PointCloud pointCloud)
        {
            List<byte[]> result = new List<byte[]>();

            byte[] timeStamp = BitConverter.GetBytes(pointCloud.timestamp);
            int spaceForVerticesInInchunk = maxChunkSize - timeStamp.Length;

            int remainingSpaceInCurrentChunk = 0;
            int indexInCurrentChunk = 0;
            byte[] currentChunk = null;
            for (int numberOfVerticesConverted = 0; numberOfVerticesConverted < pointCloud.vertices.Length; numberOfVerticesConverted++)
            {
                if (numberOfVerticesConverted % this.numberOfVerticesToSkip != 0)
                {
                    continue;
                }
                /* If we don't have enough space left for another vertex */
                if (remainingSpaceInCurrentChunk < Vertex.ByteSize)
                {
                    /* If we were currently building a chunk, we add the actual chunk to the list. */
                    if (currentChunk != null)
                    {
                        result.Add(currentChunk);
                    }

                    /* Determining the required size of the next chunk (a vertex = 16 bytes) and instantiating it */
                    int numberOfVertexLeft = pointCloud.vertices.Length - numberOfVerticesConverted;

                    /* If we have enough vertex left to entirely fill a chunk */
                    if((numberOfVertexLeft * Vertex.ByteSize) > spaceForVerticesInInchunk)
                    {
                        currentChunk = new byte[maxChunkSize];
                    }
                    else
                    {
                        currentChunk = new byte[sizeof(long) + numberOfVertexLeft * Vertex.ByteSize];
                    }

                    /* Adding the timestamp at the beginning of the array */
                    Array.Copy(timeStamp, 0, currentChunk, 0, timeStamp.Length);
                    //Array.Copy(chunkIndex, );
                    indexInCurrentChunk = timeStamp.Length;

                    remainingSpaceInCurrentChunk = currentChunk.Length - timeStamp.Length;

                }

                /* Adding one vertex to the current chunk */
                Vertex vertex = pointCloud.vertices[numberOfVerticesConverted];

                byte[] xInBytes = BitConverter.GetBytes(vertex.x);
                byte[] yInBytes = BitConverter.GetBytes(vertex.y);
                byte[] zInBytes = BitConverter.GetBytes(vertex.z);

                Array.Copy(xInBytes, 0, currentChunk, indexInCurrentChunk, xInBytes.Length);
                indexInCurrentChunk += xInBytes.Length;
                Array.Copy(yInBytes, 0, currentChunk, indexInCurrentChunk, yInBytes.Length);
                indexInCurrentChunk += yInBytes.Length;
                Array.Copy(zInBytes, 0, currentChunk, indexInCurrentChunk, zInBytes.Length);
                indexInCurrentChunk += zInBytes.Length;

                currentChunk[indexInCurrentChunk] = vertex.r;
                currentChunk[indexInCurrentChunk + 1] = vertex.g;
                currentChunk[indexInCurrentChunk + 2] = vertex.b;
                currentChunk[indexInCurrentChunk + 3] = vertex.tag;
                indexInCurrentChunk += 4;

                remainingSpaceInCurrentChunk -= Vertex.ByteSize;
                
            }
            result.Add(currentChunk);
            return result;
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
