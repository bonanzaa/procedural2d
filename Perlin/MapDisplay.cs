using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

public class MapDisplay : MonoBehaviour
{
    public Renderer textureRender;

	private void Awake() {
		textureRender.gameObject.SetActive(false);
	}

	public void DrawTexture(Texture2D texture) {
		textureRender.sharedMaterial.mainTexture = texture;
		textureRender.transform.localScale = new Vector3 (texture.width, 1, texture.height);
	}
}
