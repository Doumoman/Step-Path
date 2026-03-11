using UnityEngine;

public class ItemSoundManger : MonoBehaviour
{
    [SerializeField] itemSound itemSoundData;
    AudioSource audioS;

    private void Awake()
    {
        audioS = GetComponent<AudioSource>();
        itemSoundData.audio = audioS;
    }
    void Start()
    {
        
    }
    private void OnEnable()
    {
        itemSoundData.vineG += PlayVineSound;
        itemSoundData.cloudF += PlayCloudSound;
        itemSoundData.tileP += PlayTileSound;
        itemSoundData.waterP += PlayWaterPourSound;
        itemSoundData.mushroomG += PlayMushroomGrowSound;
        itemSoundData.stairC += PlayStairCompleteSound;
        itemSoundData.tileP_fail += PlayTilePlacing_fail;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void PlayVineSound()
    {
        if(itemSoundData.vinegrowing != null && audioS != null) audioS.PlayOneShot(itemSoundData.vinegrowing);
    }

    void PlayTileSound()
    {
        if (itemSoundData.tileplacing != null && audioS != null) audioS.PlayOneShot(itemSoundData.tileplacing);
    }
    void PlayCloudSound()
    {
        if (itemSoundData.cloudfading != null && audioS != null) audioS.PlayOneShot(itemSoundData.cloudfading);
    }

    void PlayWaterPourSound()
    {
        if (itemSoundData.tileplacing != null && audioS != null) audioS.PlayOneShot(itemSoundData.waterpour);
    }

    void PlayMushroomGrowSound()
    {
        if (itemSoundData.tileplacing != null && audioS != null) audioS.PlayOneShot(itemSoundData.mushroomgrow);
    }

    void PlayStairCompleteSound()
    {
        if (itemSoundData.tileplacing != null && audioS != null) audioS.PlayOneShot(itemSoundData.staircomplete);
    }

    void PlayTilePlacing_fail()
    {
        if (itemSoundData.tileplacing != null && audioS != null) audioS.PlayOneShot(itemSoundData.tileplacing_fail);
    }
}
