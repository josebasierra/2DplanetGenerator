using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace Utility
{
    public struct Range
    {
        public float min;
        public float max;

        public Range(float min, float max)
        {
            this.min = min;
            this.max = max;
        }

        public float Magnitude()
        {
            return max - min;
        }
    }

    public static class Functions
    {
        public static Vector3 GetMouseWorldPosition()
        {
            return Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }


        public static void LookAtTarget(Transform actor, Vector2 target)
        {
            Vector2 faceDirection = (target - new Vector2(actor.position.x, actor.position.y)).normalized;
            actor.up = faceDirection;
        }


        public static void PrintList<T>(List<T> list)
        {
            foreach (T x in list)
                Debug.Log(x);
        }

        public static void PrintArray<T>(T[,] array)
        {
            foreach (T x in array)
                Debug.Log(x);
        }


        public static float GetAngle(float2 origin, float2 point)
        {
            float2 v = point - origin;
            float angle = atan(v.y/v.x);

            if (v.x < 0 && v.y >= 0)       //2nd quadrant
                angle = PI + angle;
            else if (v.x < 0 && v.y < 0)   //3rd quadrant
                angle = PI + angle;
            else if (v.x >= 0 && v.y < 0)
                angle = 2*PI + angle;

            return angle;
        }

    }

}