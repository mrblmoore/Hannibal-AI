using System;

namespace TaleWorlds.Engine
{
    /// <summary>
    /// Stub implementation of Vec3 to allow compilation without actual Bannerlord dependencies
    /// </summary>
    public struct Vec3
    {
        public float x;
        public float y;
        public float z;
        
        public static Vec3 Zero => new Vec3(0, 0, 0);
        
        public Vec3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        
        public void Normalize()
        {
            float length = (float)Math.Sqrt(x * x + y * y + z * z);
            if (length > 0)
            {
                x /= length;
                y /= length;
                z /= length;
            }
        }
        
        public static Vec3 operator +(Vec3 left, Vec3 right)
        {
            return new Vec3(left.x + right.x, left.y + right.y, left.z + right.z);
        }
        
        public static Vec3 operator -(Vec3 left, Vec3 right)
        {
            return new Vec3(left.x - right.x, left.y - right.y, left.z - right.z);
        }
        
        public static Vec3 operator *(Vec3 vec, float scalar)
        {
            return new Vec3(vec.x * scalar, vec.y * scalar, vec.z * scalar);
        }
        
        public static Vec3 operator /(Vec3 vec, float scalar)
        {
            return new Vec3(vec.x / scalar, vec.y / scalar, vec.z / scalar);
        }
        
        public static Vec3 operator -(Vec3 vec)
        {
            return new Vec3(-vec.x, -vec.y, -vec.z);
        }
        
        public static bool operator ==(Vec3 left, Vec3 right)
        {
            return left.x == right.x && left.y == right.y && left.z == right.z;
        }
        
        public static bool operator !=(Vec3 left, Vec3 right)
        {
            return !(left == right);
        }
        
        public override bool Equals(object obj)
        {
            if (!(obj is Vec3))
                return false;
                
            Vec3 other = (Vec3)obj;
            return this == other;
        }
        
        public override int GetHashCode()
        {
            return x.GetHashCode() ^ y.GetHashCode() ^ z.GetHashCode();
        }
    }
}
