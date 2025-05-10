using System;

namespace HannibalAI.Adapters
{
    /// <summary>
    /// Compatibility class for Vec3, used for cross-version support
    /// This will be used when we need to maintain our own implementation
    /// of Vec3 for older game versions
    /// </summary>
    public class HannibalVec3
    {
        public float x;
        public float y;
        public float z;
        
        public HannibalVec3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        
        public static HannibalVec3 Zero => new HannibalVec3(0, 0, 0);
        
        public float Distance(HannibalVec3 other)
        {
            float dx = x - other.x;
            float dy = y - other.y;
            float dz = z - other.z;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
        
        /// <summary>
        /// Convert from HannibalVec3 to TaleWorlds.Library.Vec3
        /// </summary>
        public TaleWorlds.Library.Vec3 ToLibraryVec3()
        {
            return new TaleWorlds.Library.Vec3(x, y, z);
        }
        
        /// <summary>
        /// Convert from TaleWorlds.Library.Vec3 to HannibalVec3
        /// </summary>
        public static HannibalVec3 FromLibraryVec3(TaleWorlds.Library.Vec3 vec)
        {
            return new HannibalVec3(vec.x, vec.y, vec.z);
        }
    }
}