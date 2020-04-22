using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

using Unity.Mathematics;
using static Unity.Mathematics.math;


public enum NoiseType { perlin, cellular, simplex }
public enum NoiseOperation { intersect, union, mult, mix, none }


[System.Serializable]
public class NoiseSettings
{
    public NoiseType type;
    public float scale = 10;
    public NoiseOperation operation;
}


public static class Noise
{

    public static NativeArray<float> CreateNoiseMap(uint seed, int mapSize, NoiseSettings[] noiseList)
    {
        NativeArray<float> noiseMap = new NativeArray<float>(mapSize * mapSize, Allocator.TempJob);

        Unity.Mathematics.Random generator = new Unity.Mathematics.Random(seed);

        float2 viewPoint = float2(generator.NextFloat(0, 999999), generator.NextFloat(0, 999999));
        float2 offset = float2(-mapSize / 2, -mapSize / 2);
        
        int n = noiseList.Length;
        JobHandle[] jobHandles = new JobHandle[n];

        for (int i = 0; i < n; i++)
        {


            NoiseSettings noise = noiseList[i];
            NoiseJob noiseJob = new NoiseJob()
            {
                noiseMap = noiseMap,
                mapSize = mapSize,
                noiseType = noise.type,
                noiseOperation = noise.operation,
                scale = noise.scale,
                viewPoint = viewPoint,
                offset = offset
            };

            if (i == 0)
                jobHandles[i] = noiseJob.Schedule(noiseMap.Length, 64);
            else
                jobHandles[i] = noiseJob.Schedule(noiseMap.Length, 64, jobHandles[i - 1]);
        }

        jobHandles[n - 1].Complete();

        return noiseMap;
    }


    public static void DrawNoiseMap(NativeArray<float> noiseMap, int mapSize, Renderer textureRender)
    {
        Texture2D texture = new Texture2D(mapSize, mapSize);

        Color[] colorMap = new Color[mapSize * mapSize];
        for (int y = 0; y < mapSize; y++)
        {
            for (int x = 0; x < mapSize; x++)
            {
                colorMap[y * mapSize + x] = Color.Lerp(Color.black, Color.white, noiseMap[y * mapSize + x]);
            }
        }
        texture.SetPixels(colorMap);
        texture.Apply();

        textureRender.sharedMaterial.mainTexture = texture;
        textureRender.transform.localScale = new Vector3(mapSize/10, 1, mapSize/10);
    }


    public static float SampleNoise(NoiseType type, float2 st)
    {
        switch (type)
        {
            case NoiseType.perlin:
                return (noise.cnoise(st) + 1) / 2;   //[-1,1] -> [0,1]
            case NoiseType.cellular:
                return noise.cellular(st).x;
            case NoiseType.simplex:
                return (noise.snoise(st) + 1) / 2;    //[-1,1] -> [0,1]
            default: return 0f;
        }
    }


    public static float MergeNoise(NoiseOperation operation, float value1, float value2)
    {
        switch (operation)
        {
            case NoiseOperation.intersect:
                return min(value1, value2);
            case NoiseOperation.union:
                return max(value1, value2);
            case NoiseOperation.mult:
                return value1 * value2;
            case NoiseOperation.mix:
                return (value1 + value2) / 2;
            default: return value2;
        }
    }


    [BurstCompile]
    private struct NoiseJob : IJobParallelFor
    {
        public NativeArray<float> noiseMap;
        public int mapSize;

        public NoiseType noiseType;
        public NoiseOperation noiseOperation;

        public float scale;
        public float2 viewPoint, offset;


        public void Execute(int i)
        {
            float originalNoiseValue = noiseMap[i];
            if (originalNoiseValue == -1) return;

            int x = i % mapSize;
            int y = i / mapSize;
            float2 st = viewPoint + offset / scale + float2(x, y) / scale;
     
            float noiseValue = SampleNoise(noiseType, st);
            noiseMap[i] = MergeNoise(noiseOperation, originalNoiseValue, noiseValue);
        }

    }

}


