using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using Utility;


public enum PlanetElementType { grass, grass2, dirt, rock, gold, water, magmaRock, lava, none, background }

[System.Serializable]
public struct PlanetElementData
{
    public PlanetElementType type;
    public Color color;
    [Range(0,1)]
    public float noise;
    public float2 depthRange;
}

[System.Serializable]
public struct PlanetShape
{
    public float radius;
    public float deformation;
    public float deformationFreq;
    public NoiseType type;
}


public class PlanetGenerator : MonoBehaviour
{
    public uint seed;
   
    [Range(0,1)]
    public float caveDensity;
    public Color backgroundColor;

    public PlanetShape shape;
    public NoiseSettings[] noiseList;
    public PlanetElementData[] elementsList;

    public Renderer textureRenderer;


    private Dictionary<PlanetElementType, Color> elementToColor;
    private int mapSize;
    private float2 center;

    // MINMAX Range of randomized parameters
    private readonly Range radiusRange = new Range(100,250);
    private readonly Range deformRange = new Range(0,100);
    private readonly Range deformFreqRange = new Range(0, 8);
    private readonly Range caveDensRange = new Range(0.25f, 0.6f);
    

    private void Start()
    {
        elementToColor = new Dictionary<PlanetElementType, Color>();
        foreach (PlanetElementData e in elementsList)
            elementToColor[e.type] = e.color;
        elementToColor[PlanetElementType.none] = new Color(0, 0, 0, 0);
        elementToColor[PlanetElementType.background] = backgroundColor;

        seed = max(1, seed);

        UpdatePlanetDisplay();
    }


    private void OnValidate()
    {
        Start();
    }


    public float GetRadiusRatio() => (shape.radius - radiusRange.min) / radiusRange.Magnitude();

    public float GetDeformationRatio() => (shape.deformation - deformRange.min) / deformRange.Magnitude();

    public float GetDeformationFreqRatio() => (shape.deformationFreq - deformFreqRange.min) / deformFreqRange.Magnitude();

    public float GetCaveDensityRatio() => (caveDensity - caveDensRange.min) / caveDensRange.Magnitude();


    public void SetSeed(uint seed)
    {
        this.seed = seed;

        Unity.Mathematics.Random generator = new Unity.Mathematics.Random(this.seed);

        shape.radius = generator.NextFloat(radiusRange.min, radiusRange.max);
        shape.deformation = generator.NextFloat(deformRange.min, deformRange.max);
        shape.deformationFreq = generator.NextFloat(deformFreqRange.min, deformFreqRange.max);
        caveDensity = generator.NextFloat(caveDensRange.min, caveDensRange.max);
    }


    public void SetRandomSeed()
    {
        uint randomSeed = (uint) UnityEngine.Random.Range(1, 999999);
        SetSeed(randomSeed);
    }


    public void SetRadiusRatio(float ratio) {
        shape.radius = radiusRange.Magnitude() * ratio + radiusRange.min;
    }


    public void SetDeformationRatio(float ratio)
    {
        shape.deformation = deformRange.Magnitude() * ratio + deformRange.min;
    }


    public void SetDeformationFreqRatio(float ratio)
    {
        shape.deformationFreq = deformFreqRange.Magnitude() * ratio + deformFreqRange.min;
    }


    public void SetCaveDensityRatio(float ratio)
    {
        caveDensity = caveDensRange.Magnitude() * ratio + caveDensRange.min;
    }


    public void UpdatePlanetDisplay()
    {
        NativeArray<PlanetElementType> planetMap = CreatePlanetMap();
        DrawPlanetMap(planetMap, textureRenderer);
        planetMap.Dispose();
    }


    public NativeArray<PlanetElementType> CreatePlanetMap()
    {
        mapSize = (int)(2 * shape.radius + 2 * shape.deformation);
        center = float2(mapSize / 2f);

        NativeArray<float> noiseMap = Noise.CreateNoiseMap(seed, mapSize, noiseList);
        NativeArray<PlanetElementType> planetMap = CreatePlanetMap(noiseMap);

        noiseMap.Dispose();

        return planetMap;
    }


    private NativeArray<PlanetElementType> CreatePlanetMap(NativeArray<float> noiseMap)
    {
        NativeArray<PlanetElementType> planetMap = new NativeArray<PlanetElementType>(mapSize * mapSize, Allocator.TempJob);

        NativeArray<PlanetElementData> elementsListAux = new NativeArray<PlanetElementData>(elementsList.Length, Allocator.TempJob);
        elementsListAux.CopyFrom(elementsList);


        PlanetMapJob planetMapJob = new PlanetMapJob()
        {
            planetMap = planetMap,
            mapSize = mapSize,
            center = center,
            noiseMap = noiseMap,
            elementsList = elementsListAux,
            shape = shape,
            seed = seed,
            caveDensity = caveDensity
        };

        JobHandle jobHandle = planetMapJob.Schedule(noiseMap.Length, 64);
        jobHandle.Complete();

        elementsListAux.Dispose();

        return planetMap;
    }


    [BurstCompile]
    private struct PlanetMapJob : IJobParallelFor
    {
        public NativeArray<PlanetElementType> planetMap;
        public int mapSize;
        public float2 center;

        [ReadOnly]
        public NativeArray<float> noiseMap;

        [ReadOnly]
        public NativeArray<PlanetElementData> elementsList;

        public PlanetShape shape;
        public uint seed;
        public float caveDensity;



        public void Execute(int i)
        {
            float2 point = float2(i % mapSize, i / mapSize);
            float angle = Utility.Functions.GetAngle(center, point);

            float surfaceDistToCenter = SurfaceAltitude(seed, angle, shape);
            float pointDistToCenter = distance(center, point);

            float depth = (surfaceDistToCenter - pointDistToCenter) / surfaceDistToCenter;
            float noise = noiseMap[i];
            float scaledNoise = (noise - caveDensity) / (1 - caveDensity);

            if (depth < 0)
                planetMap[i] = PlanetElementType.none;
            else if (noise < caveDensity && depth < 0.8f)
                planetMap[i] = PlanetElementType.background;
            else
                planetMap[i] = SelectPlanetElement(elementsList, depth, scaledNoise);

        }
    }


    private static float SurfaceAltitude(uint seed, float angle, PlanetShape shape)
    {
        float r = shape.radius * shape.deformationFreq/100;

        Unity.Mathematics.Random generator = new Unity.Mathematics.Random(seed);
        float2 viewPoint = float2(generator.NextFloat(0, 999999), generator.NextFloat(0, 999999));

        float2 st = viewPoint + float2(cos(angle) * r, sin(angle) * r);

        // different surface generation noise to be done *
        float noise1 = Noise.SampleNoise(shape.type, st);
        float noise2 = Noise.SampleNoise(shape.type, st + float2(100,100));

        float noise = (noise1 * noise2);
        noise *= noise;

        return shape.radius + shape.deformation*noise;
    }


    private static PlanetElementType SelectPlanetElement(NativeArray<PlanetElementData> candidatesData, float depth, float noise)
    {
        float bestScore = float.MaxValue;   //less is better
        int bestIndex = 0;
        for (int i = 0; i < candidatesData.Length; i++)
        {
            PlanetElementData eData = candidatesData[i];

            float depthPenalization = 0;
            if ((depth < eData.depthRange.x) || (depth > eData.depthRange.y))
                depthPenalization = min(distance(depth, eData.depthRange.x), distance(depth, eData.depthRange.y));

            float noisePenalization = distance(eData.noise, noise);

            float score = 1f*depthPenalization + 0.5f*noisePenalization;

            if (score < bestScore)
            {
                bestScore = score;
                bestIndex = i;
            }
        }

        return candidatesData[bestIndex].type;
    }


    private void DrawPlanetMap(NativeArray<PlanetElementType> planetMap, Renderer textureRender)
    {
        Texture2D texture = new Texture2D(mapSize, mapSize);

        Color[] colorMap = new Color[mapSize * mapSize];
        for (int y = 0; y < mapSize; y++)
        {
            for (int x = 0; x < mapSize; x++)
            {
                colorMap[y * mapSize + x] = elementToColor[planetMap[y * mapSize + x]];
            }
        }
        texture.SetPixels(colorMap);
        texture.Apply();

        textureRender.sharedMaterial.mainTexture = texture;
        textureRender.transform.localScale = new Vector3(mapSize / 10, 1, mapSize / 10);
    }

}