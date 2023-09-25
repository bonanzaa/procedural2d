using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using System;
using Unity.Mathematics;

public class MapGenerator : MonoBehaviour
{
    public static MapGenerator Instance;
    public GenerationAlgorithm generationAlgorithm;
    public bool displayMapPreview = true;
    public enum DrawMode {NoiseMap, ColourMap,FalloffMap};
	public DrawMode drawMode;
    public static int mapSize = 100;
    public float noiseScale;
    public int seed;
    public int octaves;
    public float persistance;
    public float lacunarity;
    public Vector2 offset;
    public bool autoUpdate = true;
    public TerrainType[] regions;
    [Header("Falloff Map")]
    public bool useFalloff = false;
    public float curveA = 3f;
    public float curveB = 2.2f;
    [Header("Diamond Square")]
    public int heightMapSize = 128;
    public float roughness = 2f;
    public int heightMapResolution = 8;
    private int falloffChuckSize = 0;
    private MapDisplay display;

    float[] heightArr;
    float[,] falloffMap;

    private void Awake() {
        Instance = this;
        if(generationAlgorithm == GenerationAlgorithm.DiamondSquare) return;
		falloffMap = FalloffGenerator.GenerateFalloffMap (falloffChuckSize,curveA,curveB);
        AdjustFalloffChunkSize();
	}

    private void Start() {
        if(generationAlgorithm == GenerationAlgorithm.Perlin){
            GeneratePerlinMap();
        }else{
            GenerateDiamondSquareMap();
        }
    }

    private void GenerateHeightArray(){
        float step = 1f / heightMapResolution;

        heightArr = new float[heightMapResolution];
        for (int i = 0; i < heightMapResolution; i++)
        {
            heightArr[i] = i * step;
        }

        heightArr[^1] = 1;
    }

	public void GeneratePerlinMap() {
		float[,] noiseMap = Noise.GenerateNoiseMap (mapSize, mapSize, seed, noiseScale, octaves, persistance, lacunarity, offset);

		Color[] colourMap = new Color[mapSize * mapSize];

		for (int y = 0; y < mapSize; y++) {
			for (int x = 0; x < mapSize; x++) {
                if(useFalloff){
                    noiseMap[x,y] = Mathf.Clamp01(noiseMap[x,y] - falloffMap[x,y]);
                }

				float currentHeight = noiseMap [x, y];
				for (int i = 0; i < regions.Length; i++) {
					if (currentHeight <= regions [i].height) {
                        // colorMap
						colourMap [y * mapSize + x] = regions[i].colour;                       
						break;
					}
				}
			}
		}

		if(display == null) display = GetComponent<MapDisplay>();

        if(!displayMapPreview){
            display.textureRender.gameObject.SetActive(false);
            return;
        }

        display.textureRender.gameObject.SetActive(true);
		if (drawMode == DrawMode.NoiseMap) {
			display.DrawTexture (TextureGenerator.TextureFromHeightMap(noiseMap));
		} else if (drawMode == DrawMode.ColourMap) {
			display.DrawTexture (TextureGenerator.TextureFromColourMap(colourMap, mapSize, mapSize));
		}else if(drawMode == DrawMode.FalloffMap){
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(falloffChuckSize,curveA,curveB)));
        }
	}

    public void GenerateDiamondSquareMap(){
        CheckForCorrectHeightMapSize();

        System.Random r = new System.Random(seed);
        UnityEngine.Random.InitState(seed);

        GenerateHeightArray();

        int dataSize = heightMapSize + 1;
        double[,] data = new double[dataSize,dataSize];

        data[0, 0] = data[0, dataSize - 1] = data[dataSize - 1, 0] =
              data[dataSize - 1, dataSize - 1] = UnityEngine.Random.Range(0f,1f);
        
        double h = roughness;

        for (int sideLength = dataSize-1; sideLength >= 2; sideLength /= 2, h /= 2.0)
        {
            int halfSide = sideLength / 2;
            for (int x = 0; x < dataSize-1; x+=sideLength)
            {
                for (int y = 0; y < dataSize-1; y+=sideLength)
                {
                    double avg = data[x, y] + //top left
                        data[x + sideLength, y] +//top right
                        data[x, y + sideLength] + //lower left
                        data[x + sideLength, y + sideLength];//lower right
                        avg /= 4.0f;
                    data[x + halfSide, y + halfSide] =Round(avg + (r.NextDouble() * 2 * h) - h);
                }
            }

            for (int x = 0; x < dataSize-1; x+= halfSide)
            {
                for (int y = (x + halfSide) % sideLength; y < dataSize-1; y+=sideLength)
                {
                    double avg =
                        data[(x - halfSide + dataSize) % dataSize, y] + //left of center
                        data[(x + halfSide) % dataSize, y] + //right of center
                        data[x, (y + halfSide) % dataSize] + //below center
                        data[x, (y - halfSide + dataSize) % dataSize]; //above center
                        avg /= 4.0f;
                    avg = Round(avg + (r.NextDouble() * 2 * h) - h);
                    data[x, y] = avg;

                    if (x == 0) data[dataSize - 1, y] = avg;
                    if (y == 0) data[x, dataSize - 1] = avg;

                }
            }

        }
        if(display == null) display = GetComponent<MapDisplay>();

        // return data
        if (drawMode == DrawMode.NoiseMap) {
			display.DrawTexture (TextureGenerator.TextureFromHeightMapNoLerpDouble(data));
		} else if (drawMode == DrawMode.ColourMap) {
			display.DrawTexture (TextureGenerator.ColorMapFromHeightMapNoLerpDouble(data,regions));
		}
    }

    private void CheckForCorrectHeightMapSize(){
        if(IsPowerOfTwo()) return;

        int lowerPower = 1;
        int upperPower = 2;

        while (upperPower < heightMapSize)
        {
            lowerPower = upperPower;
            upperPower *= 2;
        }

        // Check if the difference between a and lowerPower is smaller than the difference between a and upperPower.
        if (heightMapSize - lowerPower <= upperPower - heightMapSize)
        {
            heightMapSize = lowerPower;
            return;
        }

        heightMapSize = upperPower;
    }

    private bool IsPowerOfTwo(){
        return heightMapSize > 0 && (heightMapSize & (heightMapSize - 1)) == 0;
    }

    private float Round(double value){


        float closestValue = heightArr[0];
        float minDifference = Mathf.Abs((float)value - closestValue);

        for (int i = 1; i < heightArr.Length; i++)
        {
            float difference = Mathf.Abs((float)value - heightArr[i]);
            if (difference < minDifference)
            {
                closestValue = heightArr[i];
                minDifference = difference;
            }
        }

        return closestValue;
    }
	void OnValidate() {
		if (mapSize < 1) {
			mapSize = 1;
		}
		if (mapSize < 1) {
			mapSize = 1;
		}
		if (lacunarity < 1) {
			lacunarity = 1;
		}
		if (octaves < 0) {
			octaves = 0;
		}
        if(heightMapResolution < 1){
            heightMapResolution = 1;
        }


        AdjustFalloffChunkSize();

        falloffMap = FalloffGenerator.GenerateFalloffMap (falloffChuckSize,curveA,curveB);
	}
    private void AdjustFalloffChunkSize(){
        falloffChuckSize = mapSize;
    }
}

[System.Serializable]
public struct TerrainType {
	public string name;
	public float height;
	public Color colour;
    public TerrainCellState state;
}

public enum Cell2dOrientation{
    XY,
    YX
}

public enum GenerationAlgorithm{
    Perlin,
    DiamondSquare
}

