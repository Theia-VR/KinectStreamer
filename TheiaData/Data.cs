using System;
using System.Runtime.Serialization;

namespace TheiaData
{
    /**
    * A vertex is a pair of coordinates and color information.
    * Vertices are linked to skeletons through tags. <- we need to check that
    **/
    [DataContract]
    public class Vertex
    {
        [DataMember]
        public float x { get; set; }
        [DataMember]
        public float y { get; set; }
        [DataMember]
        public float z { get; set; }
        [DataMember]
        public byte r { get; set; }
        [DataMember]
        public byte g { get; set; }
        [DataMember]
        public byte b { get; set; }
        [DataMember]
        public byte tag { get; set; }

        public static int ByteSize = (3 * sizeof(float)) + (4 * sizeof(byte));

        public Vertex()
        {
            this.x = 0;
            this.y = 0;
            this.z = 0;
            this.r = 1;
            this.g = 1;
            this.b = 1;
            this.tag = 0;
        }

        public Vertex(float x, float y, float z, byte r, byte g, byte b, byte tag)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.r = r;
            this.g = g;
            this.b = b;
            this.tag = tag;
        }

        public Vertex(byte[] bytes)
        {
            this.x = BitConverter.ToUInt16(bytes, 0);
            this.y = BitConverter.ToUInt16(bytes, 2);
            this.z = BitConverter.ToUInt16(bytes, 4);
            this.r = bytes[6];
            this.g = bytes[7];
            this.b = bytes[8];
            this.tag = bytes[9];
        }

        public Vertex(byte[] bytes, int offset)
        {
            this.x = BitConverter.ToUInt16(bytes, 0 + offset);
            this.y = BitConverter.ToUInt16(bytes, 2 + offset);
            this.z = BitConverter.ToUInt16(bytes, 4 + offset);
            this.r = bytes[6 + offset];
            this.g = bytes[7 + offset];
            this.b = bytes[8 + offset];
            this.tag = bytes[9 + offset];
        }

        public static byte[] ArrayToBytes(Vertex[] vertices)
        {
            byte[] buffer = new byte[vertices.Length * Vertex.ByteSize];
            for (int i = 0; i < vertices.Length; i++)
            {
                Array.Copy(BitConverter.GetBytes(vertices[i].x), 0, buffer, i * Vertex.ByteSize, 2);
                Array.Copy(BitConverter.GetBytes(vertices[i].y), 0, buffer, i * Vertex.ByteSize + 2, 2);
                Array.Copy(BitConverter.GetBytes(vertices[i].z), 0, buffer, i * Vertex.ByteSize + 4, 2);
                Array.Copy(BitConverter.GetBytes(vertices[i].r), 0, buffer, i * Vertex.ByteSize + 6, 1);
                Array.Copy(BitConverter.GetBytes(vertices[i].g), 0, buffer, i * Vertex.ByteSize + 7, 1);
                Array.Copy(BitConverter.GetBytes(vertices[i].b), 0, buffer, i * Vertex.ByteSize + 8, 1);
                Array.Copy(BitConverter.GetBytes(vertices[i].tag), 0, buffer, i * Vertex.ByteSize + 9, 1);
            }
            return buffer;
        }

        public static Vertex[] BytesToArray(byte[] bytes)
        {
            Vertex[] array = new Vertex[bytes.Length / Vertex.ByteSize];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = new Vertex(bytes, i * Vertex.ByteSize);
            }
            return array;
        }
    }
    /**
     * Tag associated to each joint type in a skeleton.
     * This tag is used to assemble the skeleton.
     **/
    [DataContract]
    public enum SkeletonJointType
    {
        SpineBase = 0,
        SpineMid = 1,
        Neck = 2,
        Head = 3,

        ShoulderLeft = 4,
        ElbowLeft = 5,
        WristLeft = 6,
        HandLeft = 7,

        ShoulderRight = 8,
        ElbowRight = 9,
        WristRight = 10,
        HandRight = 11,

        HipLeft = 12,
        KneeLeft = 13,
        AnkleLeft = 14,
        FootLeft = 15,

        HipRight = 16,
        KneeRight = 17,
        AnkleRight = 18,
        FootRight = 19,

        SpineShoulder = 20,

        HandTipLeft = 21,
        ThumbLeft = 22,

        HandTipRight = 23,
        ThumbRight = 24
    }
    /**
     * Point cloud data as transmitted by the Kinect Streamer
     **/
    public class PointCloud
    {
        public long timestamp { get; set; }
        public Vertex[] vertices { get; set; }

        public PointCloud()
        {
        }

        public PointCloud(byte[] data)
        {
            this.timestamp = BitConverter.ToUInt16(data, 0);
            int nbVertices = (data.Length - sizeof(long)) / Vertex.ByteSize;
            this.vertices = new Vertex[nbVertices];

            for (int i = 0; i < nbVertices; i++)
            {
                this.vertices[i] = new Vertex(data, sizeof(long) + (i * Vertex.ByteSize));
            }
        }
    }
    /**
     * Skeleton data as transmitted by the Kinect Streamer
     **/
    [DataContract]
    public class Skeleton
    {
        [DataMember(Name = "timestamp")]
        public long timestamp { get; set; }
        [DataMember(Name = "tag")]
        public byte tag { get; set; }
        [DataMember(Name = "joints")]
        public Vertex[] joints { get; set; }

        public static int ByteSize = sizeof(long) + sizeof(byte) + 25 * Vertex.ByteSize;

        public Skeleton()
        {
            joints = new Vertex[25];
        }
    }
}