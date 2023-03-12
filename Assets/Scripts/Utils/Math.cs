using UnityEngine;

namespace Utils
{
    public static class Math
    {
        public static Quaternion GetShortestRotation(Quaternion from, Quaternion to)
        {
            Quaternion q = to * Quaternion.Inverse(from);
            
            // Q can be the-long-rotation-around-the-sphere eg. 350 degrees
            // We want the equivalent short rotation eg. -10 degrees
            // Check if rotation is greater than 190 degrees == q.w is negative
            if (q.w < 0)
            {
                // Convert the quaternion to equivalent "short way around" quaternion
                q.x = -q.x;
                q.y = -q.y;
                q.z = -q.z;
                q.w = -q.w;
            }
            
            return q;
        }
    }
}