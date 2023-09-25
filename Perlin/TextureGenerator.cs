using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.IO;

public static class TextureGenerator 
{
   public static Texture2D TextureFromColourMap(Color[] colourMap, int width, int height) {
		Texture2D texture = new Texture2D (width, height);
		texture.filterMode = FilterMode.Point;
		texture.wrapMode = TextureWrapMode.Clamp;
		texture.SetPixels (colourMap);
		texture.Apply ();
		return texture;
	}

	public static Texture2D TextureFromHeightMap(float[,] heightMap) {
		int width = heightMap.GetLength (0);
		int height = heightMap.GetLength (1);

		Color[] colourMap = new Color[width * height];
		for (int y = 0; y < height; y++) {
			for (int x = 0; x < width; x++) {
				colourMap [y * width + x] = Color.Lerp (Color.black, Color.white, heightMap [x, y]);
			}
		}

		return TextureFromColourMap (colourMap, width, height);
	}

	public static Texture2D TextureFromHeightMapNoLerpDouble(double[,] heightMap) {
		int width = heightMap.GetLength (0);
		int height = heightMap.GetLength (1);

		Color[] colourMap = new Color[width * height];
		for (int y = 0; y < height; y++) {
			for (int x = 0; x < width; x++) {
				colourMap [y * width + x] = new Color((float)heightMap[x,y],(float)heightMap[x,y],(float)heightMap[x,y],1);
			}
		}

		return TextureFromColourMap (colourMap, width, height);
	}

	public static Texture2D ColorMapFromHeightMapNoLerpDouble(double[,] heightMap,TerrainType[] regions){
		int width = heightMap.GetLength (0);
		int height = heightMap.GetLength (1);

		Color[] colourMap = new Color[width * height];
		for (int y = 0; y < height; y++) {
			for (int x = 0; x < width; x++) {
				for (int i = 0; i < regions.Length; i++)
                    {
                        if (heightMap[x,y] <= regions[i].height) {
                            // colorMap
                            colourMap [y * width + x] = regions[i].colour;
							break;
                        } 
                    }
			}
		}

		return TextureFromColourMap(colourMap, width, height);
	}
}
