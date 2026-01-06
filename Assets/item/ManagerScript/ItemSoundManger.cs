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
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void PlayVineSound()
    {
        audioS.PlayOneShot(itemSoundData.vinegrowing);
    }

    void PlayTileSound()
    {
        audioS.PlayOneShot(itemSoundData.tileplacing);
    }
    void PlayCloudSound()
    {
        audioS.PlayOneShot(itemSoundData.cloudfading);
    }
}
