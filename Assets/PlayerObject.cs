using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;


// Represents the physical object for this character
public class PlayerObject : MonoBehaviour
{
    public GameObject characterObj;
    public Coords currPosition = new Coords(5,3);
    public ActiveCharacter character;
    public int order;

    private string direction;

    //Spawn the character in a new room. Reset some of their stats
    public void quoteOnQuoteInitialize(Coords startPos, int order)
    {
        currPosition = startPos;
        this.order = order;
    }

    public void changeSprite(string direction, int frame)
    {
        characterObj.GetComponent<SpriteRenderer>().sprite = getFromAssetBundle(direction, character.ToString());
        this.direction = direction;
        changeAnimationFrame(frame);
    }

    public void changeAnimationFrame(int frame)
    {
        switch (frame)
        {
            case 0:
            case 2:
                characterObj.GetComponent<SpriteRenderer>().sprite = getFromAssetBundle(direction, character.ToString());
                break;
            case 1:
                characterObj.GetComponent<SpriteRenderer>().sprite = getFromAssetBundle(direction, character.ToString() + "_1");
                break;
            case 3:
                characterObj.GetComponent<SpriteRenderer>().sprite = getFromAssetBundle(direction, character.ToString() + "_2");
                break;
        }
    }

    // Asset bundles needed for sprite movement
    private Sprite getFromAssetBundle(string bundleName, string assetName)
    {
        AssetBundle localAssetBundle = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, bundleName));

        if (localAssetBundle == null)
        {
            Debug.LogError("Failed to load AssetBundle!");
            return null;
        }

        Sprite asset = localAssetBundle.LoadAsset<Sprite>(assetName);
        localAssetBundle.Unload(false);
        return asset;
    }
}