using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour
{
    public const int maxViewDst = 300;
    public Transform viewer;
    public static Vector2 viewerPosition;

    int chunkSize;
    int chunksVisibleInViewDst;

    private void Start() {
        chunkSize = MapGenerator.mapSize;
        chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / chunkSize);
    }
}
